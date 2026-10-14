using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeAssist.Semantics;

namespace CodeAssist.Extension
{
    internal sealed class CompilerSemanticService : IDisposable
    {
        internal static readonly CompilerSemanticService Instance = new CompilerSemanticService();
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private ClangdClient client;
        private string currentRoot;
        internal string LastError { get; private set; }
        internal string EditorExecutable { get; set; }
        internal async Task<NavigationEntry[]> FindAsync(string method, string root, string executable, string file, string text, int line, int column, CancellationToken cancellation)
        {
            var result = await QueryAsync(method, root, executable, file, text, line, column, cancellation).ConfigureAwait(false);
            if (result == null) return null;
            var locations = result is JArray array ? array : result is JObject ? new JArray(result) : new JArray();
            var entries = new List<NavigationEntry>();
            foreach (var location in locations)
            {
                var uriText = (string)(location["uri"] ?? location["targetUri"]);
                var range = location["range"] ?? location["targetSelectionRange"];
                if (range == null || !Uri.TryCreate(uriText, UriKind.Absolute, out var uri) || !uri.IsFile) continue;
                entries.Add(new NavigationEntry("Compiler result", uri.LocalPath, (int)range["start"]["line"] + 1, (int)range["start"]["character"] + 1));
            }
            return entries.ToArray();
        }
        internal Task<JToken> QueryAsync(string method, string root, string executable, string file, string text, int line, int column,
            CancellationToken cancellation, JObject parameters = null, IDictionary<string, string> openDocuments = null) =>
            Task.Run(() => QueryCoreAsync(method, root, executable, file, text, line, column, cancellation, parameters, openDocuments), cancellation);
        private async Task<JToken> QueryCoreAsync(string method, string root, string executable, string file, string text, int line, int column,
            CancellationToken cancellation, JObject parameters, IDictionary<string, string> openDocuments)
        {
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(file) || text == null) return null;
            if (!Directory.Exists(root)) root = Path.GetDirectoryName(root);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
            if (Environment.GetEnvironmentVariable("CODEASSIST_USE_CLANGD") != "1" &&
                !File.Exists(Path.Combine(root, "compile_commands.json")) && !File.Exists(Path.Combine(root, "compile_flags.txt"))) return null;
            var clangd = Environment.GetEnvironmentVariable("CODEASSIST_CLANGD");
            if (string.IsNullOrWhiteSpace(clangd) && !string.IsNullOrEmpty(executable))
                clangd = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(executable), "../../VC/Tools/Llvm/x64/bin/clangd.exe"));
            if (string.IsNullOrEmpty(clangd) || !File.Exists(clangd)) { LastError = "clangd executable not found"; return null; }
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation, shutdown.Token))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    await gate.WaitAsync(timeout.Token).ConfigureAwait(false);
                    try
                    {
                        if (client == null || !string.Equals(root, currentRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            client?.Dispose(); client = null; currentRoot = root;
                            client = await ClangdClient.StartAsync(clangd, root, timeout.Token,
                                File.Exists(Path.Combine(root, "compile_commands.json")) && Environment.GetEnvironmentVariable("CODEASSIST_CLANGD_BACKGROUND_INDEX") != "0").ConfigureAwait(false);
                        }
                        await client.SyncAsync(file, text, timeout.Token).ConfigureAwait(false);
                        if (openDocuments != null)
                            foreach (var document in openDocuments) await client.SyncAsync(document.Key, document.Value, timeout.Token).ConfigureAwait(false);
                        if (method == "textDocument/rename")
                            foreach (var include in CodeAssist.Core.CppDocumentNavigation.Includes(text))
                            {
                                var header = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), include.Name.Replace('/', Path.DirectorySeparatorChar)));
                                if (File.Exists(header) && (openDocuments == null || !openDocuments.ContainsKey(header)))
                                    await client.SyncAsync(header, File.ReadAllText(header), timeout.Token).ConfigureAwait(false);
                            }
                        var extra = parameters ?? (method == "textDocument/references" ? new JObject { ["context"] = new JObject { ["includeDeclaration"] = true } } : null);
                        var result = await client.AtAsync(method, file, line - 1, column - 1, timeout.Token, extra).ConfigureAwait(false);
                        LastError = client.LastError; return result;
                    }
                    catch (Exception error) when (!(error is OperationCanceledException))
                    { LastError = error.Message; client?.Dispose(); client = null; return null; }
                    finally { gate.Release(); }
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested && !shutdown.IsCancellationRequested) { LastError = "clangd request timed out"; return null; }
                catch (Exception error) when (!(error is OperationCanceledException))
                { LastError = error.Message; return null; }
            }
        }
        public void Dispose() { shutdown.Cancel(); client?.Dispose(); }
    }
}
