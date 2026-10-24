using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeAssist.Semantics
{
    public static class LspFraming
    {
        public static byte[] Encode(JToken message)
        {
            var body = Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
            var header = Encoding.ASCII.GetBytes("Content-Length: " + body.Length + "\r\n\r\n");
            var bytes = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, bytes, 0, header.Length); Buffer.BlockCopy(body, 0, bytes, header.Length, body.Length); return bytes;
        }
        public static async Task<JToken> ReadAsync(Stream stream, CancellationToken cancellation)
        {
            var header = new List<byte>(); var one = new byte[1];
            while (true)
            {
                if (await stream.ReadAsync(one, 0, 1, cancellation).ConfigureAwait(false) == 0)
                { if (header.Count > 0) throw new EndOfStreamException("Truncated LSP header"); return null; }
                header.Add(one[0]); var n = header.Count;
                if (n > 16384) throw new InvalidDataException("Oversize LSP header");
                if (n >= 4 && header[n - 4] == 13 && header[n - 3] == 10 && header[n - 2] == 13 && header[n - 1] == 10) break;
            }
            var length = -1;
            foreach (var line in Encoding.ASCII.GetString(header.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase) && !int.TryParse(line.Substring(15).Trim(), out length))
                    throw new InvalidDataException("Invalid LSP content length");
            if (length < 0 || length > 32 * 1024 * 1024) throw new InvalidDataException("Invalid LSP content length");
            var body = new byte[length]; var offset = 0;
            while (offset < body.Length)
            {
                var read = await stream.ReadAsync(body, offset, body.Length - offset, cancellation).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("Truncated LSP body"); offset += read;
            }
            return JToken.Parse(Encoding.UTF8.GetString(body));
        }
    }
    public sealed class ClangdClient : IDisposable
    {
        private readonly Process process;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly SemaphoreSlim writer = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim documents = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JToken>> pending = new ConcurrentDictionary<long, TaskCompletionSource<JToken>>();
        private readonly Dictionary<string, Tuple<int, string>> opened = new Dictionary<string, Tuple<int, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Task reader;
        private long sequence;
        private readonly SemaphoreSlim previewGate=new SemaphoreSlim(1,1);
        private TaskCompletionSource<JToken> previewEdit;
        private int disposed;
        public string LastError { get; private set; }
        private ClangdClient(string executable, string root, bool backgroundIndex,string databaseDirectory=null)
        {
            process = new Process { StartInfo = new ProcessStartInfo(executable,
                "--background-index=" + (backgroundIndex ? "1" : "0") + " -j=2 --log=error --header-insertion=never --compile-commands-dir=\"" + root + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = root } };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) LastError = e.Data; };
            if(!string.IsNullOrEmpty(databaseDirectory)){var directory=Path.GetFullPath(databaseDirectory);if(directory.IndexOf('"')>=0||!File.Exists(Path.Combine(directory,"compile_commands.json")))throw new ArgumentException("Invalid compilation database directory");process.StartInfo.Arguments+=" --compile-commands-dir=\""+directory.TrimEnd('\\','/')+"\"";}
            process.Start(); process.BeginErrorReadLine(); reader = Task.Run(ReadLoopAsync);
        }
        public static async Task<ClangdClient> StartAsync(string executable, string root, CancellationToken cancellation, bool backgroundIndex = false,string databaseDirectory=null)
        {
            var client = new ClangdClient(executable, root, backgroundIndex,databaseDirectory);
            try
            {
                await client.RequestAsync("initialize", new JObject
                {
                    ["processId"] = Process.GetCurrentProcess().Id, ["rootUri"] = new Uri(Path.GetFullPath(root) + Path.DirectorySeparatorChar).AbsoluteUri,
                    ["capabilities"] = new JObject { ["workspace"] = new JObject {["applyEdit"] = true}, ["general"] = new JObject { ["positionEncodings"] = new JArray("utf-16") },
                        ["textDocument"] = new JObject { ["codeAction"] = new JObject { ["dataSupport"] = true, ["resolveSupport"] = new JObject { ["properties"] = new JArray("edit") }, ["codeActionLiteralSupport"] = new JObject { ["codeActionKind"] = new JObject { ["valueSet"] = new JArray("quickfix", "refactor", "refactor.extract", "refactor.inline", "refactor.rewrite") } } }, ["completion"] = new JObject { ["completionItem"] = new JObject { ["snippetSupport"] = false } } } }
                }, cancellation).ConfigureAwait(false);
                await client.NotifyAsync("initialized", new JObject()).ConfigureAwait(false); return client;
            }
            catch { client.Dispose(); throw; }
        }
        public async Task SyncAsync(string file, string text, CancellationToken cancellation)
        {
            await documents.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var uri = new Uri(Path.GetFullPath(file)).AbsoluteUri;
                if (!opened.TryGetValue(file, out var previous))
                {
                    await NotifyAsync("textDocument/didOpen", new JObject { ["textDocument"] = new JObject { ["uri"] = uri, ["languageId"] = "cpp", ["version"] = 1, ["text"] = text } }).ConfigureAwait(false);
                    opened[file] = Tuple.Create(1, text);
                }
                else if (previous.Item2 != text)
                {
                    var version = previous.Item1 + 1;
                    await NotifyAsync("textDocument/didChange", new JObject { ["textDocument"] = new JObject { ["uri"] = uri, ["version"] = version },
                        ["contentChanges"] = new JArray(new JObject { ["text"] = text }) }).ConfigureAwait(false);
                    opened[file] = Tuple.Create(version, text);
                }
            }
            finally { documents.Release(); }
        }
        public Task<JToken> AtAsync(string method, string file, int line, int character, CancellationToken cancellation, JObject extra = null)
        {
            var args = extra ?? new JObject(); args["textDocument"] = new JObject { ["uri"] = new Uri(Path.GetFullPath(file)).AbsoluteUri };
            args["position"] = new JObject { ["line"] = line, ["character"] = character }; return RequestAsync(method, args, cancellation);
        }
        public async Task<JToken> RequestAsync(string method, JToken args, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (disposed != 0) throw new ObjectDisposedException(nameof(ClangdClient));
            var id = Interlocked.Increment(ref sequence);
            var completion = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[id] = completion;
            using (cancellation.Register(() => { if (pending.TryRemove(id, out var canceled)) canceled.TrySetCanceled(); }))
            {
                try
                {
                    await SendAsync(new JObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = args }).ConfigureAwait(false);
                    return await completion.Task.ConfigureAwait(false);
                }
                finally { pending.TryRemove(id, out _); }
            }
        }
        public static bool SupportsPreviewCommand(JToken command) => command is JObject &&
            ((string)command["command"]=="clangd.applyTweak"||(string)command["command"]=="clangd.applyFix");
        public async Task<JToken> PreviewCommandAsync(JObject command,CancellationToken cancellation)
        {
            if(!SupportsPreviewCommand(command))throw new NotSupportedException("Unsupported compiler command");
            await previewGate.WaitAsync(cancellation).ConfigureAwait(false);
            var captured=new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                Volatile.Write(ref previewEdit,captured);
                try {await RequestAsync("workspace/executeCommand",new JObject {["command"]=command["command"].DeepClone(),["arguments"]=command["arguments"]?.DeepClone()??new JArray()},cancellation).ConfigureAwait(false);}
                catch(InvalidOperationException) when(captured.Task.Status==TaskStatus.RanToCompletion) { }
                cancellation.ThrowIfCancellationRequested();
                if(captured.Task.Status!=TaskStatus.RanToCompletion)throw new InvalidOperationException("Compiler command did not produce a text-edit preview");
                return await captured.Task.ConfigureAwait(false);
            }
            finally {Volatile.Write(ref previewEdit,null);previewGate.Release();}
        }
        private readonly ConcurrentDictionary<string,JArray> diagnostics=new ConcurrentDictionary<string,JArray>(StringComparer.OrdinalIgnoreCase);
        public JArray Diagnostics(string file)=>diagnostics.TryGetValue(new Uri(Path.GetFullPath(file)).AbsoluteUri,out var found)?(JArray)found.DeepClone():new JArray();
        public Task NotifyAsync(string method, JToken args) => SendAsync(new JObject { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = args });
        private async Task SendAsync(JToken value)
        {
            var bytes = LspFraming.Encode(value); await writer.WaitAsync(shutdown.Token).ConfigureAwait(false);
            try { await process.StandardInput.BaseStream.WriteAsync(bytes, 0, bytes.Length, shutdown.Token).ConfigureAwait(false); await process.StandardInput.BaseStream.FlushAsync(shutdown.Token).ConfigureAwait(false); }
            finally { writer.Release(); }
        }
        private async Task ReadLoopAsync()
        {
            try
            {
                while (!shutdown.IsCancellationRequested)
                {
                    var message = await LspFraming.ReadAsync(process.StandardOutput.BaseStream, shutdown.Token).ConfigureAwait(false);
                    if (message == null) throw new EndOfStreamException("clangd exited");
                    if (message["method"] != null)
                    {
                        if((string)message["method"]=="workspace/applyEdit" && message["id"]!=null)
                        {
                            var capture=Volatile.Read(ref previewEdit);var edit=message["params"]?["edit"];
                            if(capture!=null&&edit!=null)capture.TrySetResult(edit.DeepClone());
                            await SendAsync(new JObject {["jsonrpc"]="2.0",["id"]=message["id"].DeepClone(),["result"]=new JObject {["applied"]=false,["failureReason"]="CodeAssist preview only; no files changed"}}).ConfigureAwait(false);
                            continue;
                        }
                        if((string)message["method"]=="textDocument/publishDiagnostics"&&message["params"]?["uri"]!=null&&message["params"]?["diagnostics"] is JArray published)diagnostics[(string)message["params"]["uri"]]=(JArray)published.DeepClone();
                        if (message["id"] != null) await SendAsync(new JObject { ["jsonrpc"] = "2.0", ["id"] = message["id"],
                            ["error"] = new JObject { ["code"] = -32601, ["message"] = "Unsupported client request" } }).ConfigureAwait(false);
                        continue;
                    }
                    if (message["id"] != null && pending.TryRemove((long)message["id"], out var completion))
                    {
                        if (message["error"] != null) completion.TrySetException(new InvalidOperationException(message["error"].ToString(Formatting.None)));
                        else completion.TrySetResult(message["result"]);
                    }
                }
            }
            catch (Exception error)
            {
                LastError = error.Message;
                foreach (var request in pending) if (pending.TryRemove(request.Key, out var completion)) completion.TrySetException(error);
            }
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            shutdown.Cancel();
            try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { }
            foreach (var request in pending) if (pending.TryRemove(request.Key, out var completion)) completion.TrySetCanceled();
            reader.ContinueWith(completed => process.Dispose(), TaskScheduler.Default);
        }
    }
}
