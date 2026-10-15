using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace CodeAssist.Indexing
{
    public sealed class CompilationTarget
    {
        public string ProjectFile { get; internal set; }
        public string Condition { get; internal set; }
        public IReadOnlyList<string> IncludeDirectories { get; internal set; }
        public IReadOnlyList<string> Defines { get; internal set; }
        public IReadOnlyList<string> UnresolvedValues { get; internal set; }
    }

    /// <summary>Reads generated project settings without executing MSBuild or Unreal Build Tool.</summary>
    public sealed class ProjectCompilationModel
    {
        public IReadOnlyList<CompilationTarget> Targets { get; private set; } = Array.Empty<CompilationTarget>();
        public string Fingerprint { get; private set; } = string.Empty;
        public IReadOnlyList<IndexedSymbol> HeaderCandidates(IEnumerable<string> indexedFiles)
        {
            var roots = Targets.SelectMany(target => target.IncludeDirectories.Select(directory =>
                Path.GetFullPath(Path.IsPathRooted(directory) ? directory : Path.Combine(Path.GetDirectoryName(target.ProjectFile), directory))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var candidates = new List<IndexedSymbol>();
            foreach (var file in indexedFiles)
            {
                var extension = Path.GetExtension(file);
                if (!new[] { ".h", ".hpp", ".hh", ".inl" }.Contains(extension, StringComparer.OrdinalIgnoreCase)) continue;
                var fullFile = Path.GetFullPath(file);
                foreach (var includeRoot in roots)
                    if (fullFile.StartsWith(includeRoot, StringComparison.OrdinalIgnoreCase))
                        candidates.Add(new IndexedSymbol(fullFile.Substring(includeRoot.Length).Replace('\\', '/'), CodeAssist.Core.SymbolKind.Header, file, 1, 1));
            }
            return candidates.GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToArray();
        }
        public static ProjectCompilationModel Load(string root)
        {
            var targets = new List<CompilationTarget>();
            var inputs = new List<string>();
            var folders = new[] { root, Path.Combine(root, "Intermediate", "ProjectFiles") };
            foreach (var file in folders.Where(Directory.Exists).SelectMany(folder => Directory.EnumerateFiles(folder, "*.vcxproj")).OrderBy(path => path))
            {
                var content = File.ReadAllText(file); inputs.Add(file + "\n" + content);
                var xml = XDocument.Parse(content);
                var groups = xml.Descendants().Where(e => e.Name.LocalName == "PropertyGroup" || e.Name.LocalName == "ItemDefinitionGroup").ToArray();
                var conditions = groups.Select(e => (string)e.Attribute("Condition") ?? "").Distinct().ToArray();
                foreach (var condition in conditions)
                {
                    var values = groups.Where(g => string.IsNullOrEmpty((string)g.Attribute("Condition")) || (string)g.Attribute("Condition") == condition)
                        .SelectMany(g => g.Descendants()).ToArray();
                    var includes = Read(values, new[] { "NMakeIncludeSearchPath", "AdditionalIncludeDirectories", "IncludePath" }, file, root);
                    var defines = Read(values, new[] { "NMakePreprocessorDefinitions", "PreprocessorDefinitions" }, file, root);
                    targets.Add(new CompilationTarget { ProjectFile = file, Condition = condition,
                        IncludeDirectories = includes.Where(v => !v.Contains("$(") && !v.Contains("%(")).ToArray(),
                        Defines = defines.Where(v => !v.Contains("$(") && !v.Contains("%(")).ToArray(),
                        UnresolvedValues = includes.Concat(defines).Where(v => v.Contains("$(") || v.Contains("%(")).ToArray() });
                }
            }
            var source = Path.Combine(root, "Source");
            if (Directory.Exists(source))
                inputs.AddRange(Directory.EnumerateFiles(source, "*.Build.cs", SearchOption.AllDirectories).OrderBy(path => path).Select(file => file + "\n" + File.ReadAllText(file)));
            using (var sha = SHA256.Create())
                return new ProjectCompilationModel { Targets = targets, Fingerprint = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", inputs)))).Replace("-", "") };
        }
        private static string[] Read(IEnumerable<XElement> values, string[] names, string file, string root)
        {
            return values.Where(e => names.Contains(e.Name.LocalName)).SelectMany(e => e.Value.Split(';'))
                .Select(v => v.Trim().Trim('"').Replace("$(ProjectDir)", Path.GetDirectoryName(file) + Path.DirectorySeparatorChar)
                    .Replace("$(SolutionDir)", root + Path.DirectorySeparatorChar))
                .Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
