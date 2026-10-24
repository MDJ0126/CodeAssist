using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using CodeAssist.Core;

namespace CodeAssist.Indexing
{
    public sealed class PersistentSymbolIndex
    {
        public string[] AdditionalSourceExtensions { get; set; } = Array.Empty<string>();
        private int parseConcurrency=2;
        public int ParseConcurrency { get=>parseConcurrency;set=>parseConcurrency=Math.Max(1,Math.Min(16,value)); }
        private static readonly HashSet<string> SourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".h", ".hpp", ".hh", ".inl", ".cpp", ".cc", ".cxx", ".c" };

        private static readonly HashSet<string> IgnoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".git", ".vs", "Binaries", "DerivedDataCache", "Intermediate", "Saved" };

        private static readonly Dictionary<string, string> BuiltInReturnTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "GetWorld", "UWorld*" }
            };

        private static readonly Regex TypePattern = new Regex(
            @"\b(?:class|struct)\s+(?:\w+_API\s+)?(?<name>[A-Za-z_]\w*)(?:\s*:\s*(?:(?:public|protected|private)\s+)?(?<base>[A-Za-z_]\w*))?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex FunctionPattern = new Regex(
            @"\b(?<type>[A-Za-z_]\w*(?:\s*<[^>]+>)?[*&]?)\s+(?:(?<owner>[A-Za-z_]\w*)::)?(?<name>operator\s*(?:->\*?|\(\)|\[\]|[+*/%<>=!&|^~-]+)|~?[A-Za-z_]\w*)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ConstructorPattern = new Regex(
            @"\b(?<owner>[A-Za-z_]\w*)::(?<name>~?[A-Za-z_]\w*)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex VariablePattern = new Regex(
            @"\b(?<type>(?:const\s+)?[A-Za-z_]\w*(?:\s*<[^;{}()]+>)?\s*[*&]?)\s+(?<name>[A-Za-z_]\w*)\s*(?=[=;,\[])" ,
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ConstructedVariablePattern = new Regex(
            @"\b(?<type>[A-Za-z_]\w*(?:\s*<[^;{}()]+>)?\s*[*&]?)\s+(?<name>[A-Za-z_]\w*)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ModuleNamePattern = new Regex(
            "\\\"(?<name>[A-Za-z_]\\w*)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly object gate = new object();
        private List<IndexedSymbol> symbols = new List<IndexedSymbol>();
        private string[] sourceFiles = Array.Empty<string>();
        private Dictionary<string, List<IndexedSymbol>> completionsByPrefix = new Dictionary<string, List<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<IndexedSymbol>> membersByOwner = new Dictionary<string, List<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> typeNamesByInsensitiveName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> variableTypesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> returnTypesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> variableTypesByOwnerAndName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> returnTypesByOwnerAndName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> baseTypesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<IndexedSymbol>> definitionsByName = new Dictionary<string, List<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase);
        private List<IndexedSymbol> specifierSymbols = new List<IndexedSymbol>();
        private List<IndexedSymbol> headerSymbols = new List<IndexedSymbol>();

        public string ProjectRoot { get; private set; }
        public string EngineRoot { get; private set; }
        public DateTime LastUpdatedUtc { get; private set; }

        public int Count
        {
            get { lock (gate) return symbols.Count; }
        }

        public void Build(string projectRoot, string engineRoot = null)
        {
            var files = new List<string>(EnumerateProjectFiles(projectRoot, AdditionalSourceExtensions));

            if (!string.IsNullOrWhiteSpace(engineRoot) && Directory.Exists(engineRoot))
            {
                files.AddRange(EnumerateEngineApiFiles(engineRoot, CollectEngineModules(projectRoot),AdditionalSourceExtensions));
            }

            BuildFiles(files, projectRoot, engineRoot);
        }

        public void BuildProject(string projectRoot, CancellationToken cancellation = default(CancellationToken))
        {
            BuildFiles(EnumerateProjectFiles(projectRoot, AdditionalSourceExtensions), projectRoot, null, cancellation);
        }

        public void BuildEngine(string engineRoot, string projectRoot, CancellationToken cancellation = default(CancellationToken))
        {
            var modules = CollectEngineModules(projectRoot);
            BuildFiles(EnumerateEngineApiFiles(engineRoot, modules,AdditionalSourceExtensions), null, engineRoot, cancellation);
        }

        public void LoadBuiltInApi()
        {
            var builtIns = new[]
            {
                Type("UObject"), Type("AActor", "UObject"), Type("UActorComponent", "UObject"),
                Type("USceneComponent", "UActorComponent"), Type("UWorld", "UObject"),
                Type("UGameplayStatics", "UObject"), Type("UInputMappingContext", "UObject"),
                Type("UInputAction", "UObject"), Type("FVector"), Type("FRotator"),
                Macro("UCLASS"), Macro("USTRUCT"), Macro("UENUM"), Macro("UINTERFACE"),
                Macro("UFUNCTION"), Macro("UDELEGATE"), Macro("UPROPERTY"), Macro("UMETA"), Macro("UPARAM"),
                Macro("GENERATED_UINTERFACE_BODY"), Macro("GENERATED_IINTERFACE_BODY"), Macro("RIGVM_METHOD"),
                Macro("GENERATED_BODY"), Macro("GENERATED_UCLASS_BODY"), Macro("GENERATED_USTRUCT_BODY"),
                Specifier("VisibleAnywhere"), Specifier("VisibleDefaultsOnly"), Specifier("EditAnywhere"),
                Specifier("EditDefaultsOnly"), Specifier("EditInstanceOnly"), Specifier("BlueprintReadOnly"),
                Specifier("BlueprintReadWrite"), Specifier("BlueprintAssignable"), Specifier("BlueprintCallable"),
                Specifier("BlueprintPure"), Specifier("Category"), Specifier("meta"), Specifier("Transient"),
                Specifier("Replicated"), Specifier("ReplicatedUsing"), Specifier("SaveGame"), Specifier("Config"),
                Specifier("Instanced"), Specifier("AdvancedDisplay"), Specifier("DisplayName"), Specifier("ClampMin"),
                Specifier("ClampMax"), Specifier("AllowPrivateAccess"),
                Header("CoreMinimal.h"), Header("GameFramework/Actor.h"), Header("GameFramework/Pawn.h"),
                Header("Camera/CameraComponent.h"), Header("GameFramework/SpringArmComponent.h"),
                Header("Components/CapsuleComponent.h"), Header("Components/StaticMeshComponent.h"),
                Header("InputMappingContext.h"), Header("InputAction.h"),
                Function("AActor", "Destroy", "bool"), Function("AActor", "SetLifeSpan", "void"),
                Function("AActor", "GetWorld", "UWorld*"), Function("AActor", "GetActorLocation", "FVector"),
                Function("AActor", "SetActorLocation", "bool"), Function("AActor", "GetActorRotation", "FRotator"),
                Function("AActor", "Tick", "void"), Function("AActor", "BeginPlay", "void"),
                Function("UWorld", "SpawnActor", "AActor*"), Function("UWorld", "DestroyActor", "bool"),
                Function("UObject", "GetWorld", "UWorld*"),
                Function("UGameplayStatics", "GetActorOfClass", "AActor*"),
                Function("UGameplayStatics", "GetAllActorsOfClass", "void")
            };
            ApplySymbols(builtIns.ToList(), null, null, DateTime.UtcNow);
        }

        public void ReplaceWith(params PersistentSymbolIndex[] indexes)
        {
            var merged = indexes
                .Where(index => index != null)
                .SelectMany(index => index.Snapshot())
                .GroupBy(item => item.Identity, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            ApplySymbols(
                merged,
                indexes.FirstOrDefault(index => !string.IsNullOrWhiteSpace(index?.ProjectRoot))?.ProjectRoot,
                indexes.FirstOrDefault(index => !string.IsNullOrWhiteSpace(index?.EngineRoot))?.EngineRoot,
                indexes.Where(index => index != null).Select(index => index.LastUpdatedUtc).DefaultIfEmpty().Max(),
                indexes.Where(index => index != null).SelectMany(index => index.SourceFiles()));
        }

        private void BuildFiles(IEnumerable<string> files, string projectRoot, string engineRoot, CancellationToken cancellation = default(CancellationToken))
        {
            var fileList = files.Select(file => { cancellation.ThrowIfCancellationRequested(); return file; }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var discovered = new ConcurrentBag<IndexedSymbol>();
            Parallel.ForEach(
                fileList,
                CreateParallelOptions(cancellation),
                file => ParseFile(file, discovered.Add));

            var built = discovered
                .GroupBy(item => item.Identity, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            cancellation.ThrowIfCancellationRequested();
            ApplySymbols(built, projectRoot, engineRoot, DateTime.UtcNow, fileList);
        }

        private IndexedSymbol[] Snapshot()
        {
            lock (gate) return symbols.ToArray();
        }

        public IReadOnlyList<IndexedSymbol> SearchSymbols(string query, string filePath = null, bool functionsOnly = false, int limit = 200)
        {
            return Snapshot().Where(item => !string.IsNullOrEmpty(item.FilePath) && !item.IsLocal &&
                (!functionsOnly || item.Kind == SymbolKind.Function) &&
                (filePath == null || string.Equals(filePath, item.FilePath, StringComparison.OrdinalIgnoreCase)))
                .Where(item => MemberMatchRank(item.Name, query ?? string.Empty) != int.MaxValue)
                .OrderBy(item => MemberMatchRank(item.Name, query ?? string.Empty))
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.FilePath)
                .Take(limit).ToArray();
        }

        public IReadOnlyList<string> SourceFiles() { lock (gate) return sourceFiles.ToArray(); }

        public IReadOnlyList<IndexedSymbol> RelatedSymbols(string name, string owner = null)
        {
            var all = Snapshot();
            var related = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(owner)) foreach (var type in GetTypeHierarchy(owner)) related.Add(type);
            foreach (var item in all.Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                if (item.Kind == SymbolKind.Type) foreach (var type in GetTypeHierarchy(item.Name)) related.Add(type);
                if (!string.IsNullOrEmpty(item.OwnerType)) related.Add(item.OwnerType);
                if (!string.IsNullOrEmpty(item.ValueType)) related.Add(NormalizeTypeName(item.ValueType));
            }
            return all.Where(item => !item.IsLocal && !string.IsNullOrEmpty(item.FilePath) &&
                (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) || related.Contains(item.OwnerType) || related.Contains(item.Name)))
                .OrderBy(item => item.Name).ThenBy(item => item.FilePath).Take(2000).ToArray();
        }

        private static IndexedSymbol Type(string name, string baseType = null)
        {
            return new IndexedSymbol(name, SymbolKind.Type, string.Empty, 0, 0, baseType: baseType);
        }

        private static IndexedSymbol Function(string owner, string name, string returnType)
        {
            return new IndexedSymbol(name, SymbolKind.Function, string.Empty, 0, 0, owner, returnType);
        }

        private static IndexedSymbol Macro(string name)
        {
            return new IndexedSymbol(name, SymbolKind.Macro, string.Empty, 0, 0);
        }

        private static IndexedSymbol Specifier(string name)
        {
            return new IndexedSymbol(name, SymbolKind.Specifier, string.Empty, 0, 0);
        }

        private static IndexedSymbol Header(string includePath)
        {
            return new IndexedSymbol(includePath, SymbolKind.Header, string.Empty, 0, 0);
        }

        public IReadOnlyList<IndexedSymbol> CompleteSpecifiers(string prefix, int limit = 100)
        {
            prefix = prefix ?? string.Empty;
            lock (gate)
            {
                return specifierSymbols
                    .Where(item => item.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => CompletionMatchRank(item.Name, prefix))
                    .ThenBy(item => item.Name, StringComparer.Ordinal)
                    .Take(limit)
                    .ToArray();
            }
        }

        public IReadOnlyList<IndexedSymbol> CompleteHeaders(string prefix, int limit = 100)
        {
            prefix = prefix ?? string.Empty;
            lock (gate)
            {
                return headerSymbols
                    .Select(item => new { Item = item, Rank = HeaderMatchRank(item.Name, prefix) })
                    .Where(value => value.Rank < int.MaxValue)
                    .OrderBy(value => value.Rank)
                    .ThenBy(value => value.Item.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(value => value.Item)
                    .Take(limit)
                    .ToArray();
            }
        }

        public IReadOnlyList<IndexedSymbol> Complete(string prefix, int limit = 200, bool acronyms = false, bool shorthand = false)
        {
            prefix = prefix ?? string.Empty;
            lock (gate)
            {
                IEnumerable<IndexedSymbol> pool = symbols.Where(item => !item.IsLocal);
                if (prefix.Length > 0 && completionsByPrefix.TryGetValue((acronyms||shorthand?prefix.Substring(0,1):GetPrefixKey(prefix)), out var indexedPool))
                {
                    pool = indexedPool;
                }

                var names = new HashSet<string>(StringComparer.Ordinal);
                var results = new List<KeyValuePair<int, IndexedSymbol>>();
                foreach (var item in pool)
                {
                    var rank = IdentifierMatching.Rank(item.Name, prefix, acronyms, shorthand);
                    if (rank == int.MaxValue || !names.Add(item.Name)) continue;
                    results.Add(new KeyValuePair<int, IndexedSymbol>(rank, item));
                }
                return results
                    .OrderBy(item => item.Key)
                    .ThenBy(item => item.Value.Name, StringComparer.Ordinal)
                    .Select(item => item.Value)
                    .Take(limit)
                    .ToArray();
            }
        }

        public IReadOnlyList<IndexedSymbol> CompleteMembers(string typeName, string prefix, int limit = 200, bool acronyms = true, bool shorthand = true)
        {
            return CompleteMembers(typeName,prefix,limit,acronyms,shorthand,null);
        }

        public IReadOnlyList<IndexedSymbol> CompleteMembers(string typeName,string prefix,int limit,bool acronyms,bool shorthand,IReadOnlyDictionary<string,IReadOnlyList<IndexedSymbol>> liveDocuments)
        {
            typeName=NormalizeTypeName(typeName);
            if(string.IsNullOrWhiteSpace(typeName)||limit<=0)return Array.Empty<IndexedSymbol>();
            var livePaths=new HashSet<string>(liveDocuments?.Keys??Enumerable.Empty<string>(),StringComparer.OrdinalIgnoreCase);
            var live=(liveDocuments?.Values.SelectMany(items=>items)??Enumerable.Empty<IndexedSymbol>()).Where(item=>!item.IsLocal).ToArray();
            var liveTypes=live.Where(item=>item.Kind==SymbolKind.Type).GroupBy(item=>NormalizeTypeName(item.Name),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First(),StringComparer.OrdinalIgnoreCase);
            var liveMembers=live.Where(item=>!string.IsNullOrEmpty(item.OwnerType)).ToLookup(item=>NormalizeTypeName(item.OwnerType),StringComparer.OrdinalIgnoreCase);
            lock(gate)
            {
                var owners=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var current=typeName;
                while(!string.IsNullOrWhiteSpace(current)&&owners.Add(current))
                {
                    current=liveTypes.TryGetValue(current,out var liveType)?NormalizeTypeName(liveType.BaseType):ResolveBaseType(current);
                }
                var results=new List<KeyValuePair<int,IndexedSymbol>>();
                var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var owner in owners)
                {
                    var persisted=membersByOwner.TryGetValue(owner,out var members)?members:Enumerable.Empty<IndexedSymbol>();
                    foreach(var item in liveMembers[owner].Concat(persisted.Where(item=>!livePaths.Contains(item.FilePath))))
                    {
                        var rank=CompletionMemberRank(item.Name,prefix??string.Empty,acronyms,shorthand);
                        if(rank==int.MaxValue||!names.Add(item.Name+"\0"+item.Signature))continue;
                        results.Add(new KeyValuePair<int,IndexedSymbol>(rank,item));
                    }
                }
                return results.OrderBy(item=>item.Key).ThenBy(item=>item.Value.Name,StringComparer.OrdinalIgnoreCase).Select(item=>item.Value).Take(limit).ToArray();
            }
        }

        public static string NormalizeTypeName(string typeName)
        {
            var normalized = Regex.Replace(typeName ?? string.Empty, @"\b(?:const|class|struct)\b|[*&\s]", string.Empty);
            var wrappers = new HashSet<string>(StringComparer.Ordinal)
            {
                "TObjectPtr", "TWeakObjectPtr", "TSoftObjectPtr", "TSharedPtr", "TSharedRef", "TUniquePtr"
            };
            while (true)
            {
                var match = Regex.Match(normalized, @"^(?<wrapper>[A-Za-z_]\w*)<(?<inner>[^,>]+)(?:,[^>]*)?>$");
                if (!match.Success || !wrappers.Contains(match.Groups["wrapper"].Value)) return normalized;
                normalized = Regex.Replace(match.Groups["inner"].Value, @"\b(?:const|class|struct)\b|[*&\s]", string.Empty);
            }
        }

        public string ResolveVariableType(string variableName)
        {
            lock (gate)
            {
                return variableTypesByName.TryGetValue(variableName, out var typeName) ? typeName : null;
            }
        }

        public string ResolveVariableType(string ownerType, string variableName)
        {
            foreach (var owner in GetTypeHierarchy(NormalizeTypeName(ownerType)))
            {
                lock (gate)
                {
                    if (variableTypesByOwnerAndName.TryGetValue(MemberKey(owner, variableName), out var typeName))
                        return typeName;
                }
            }
            return string.IsNullOrEmpty(ownerType) ? ResolveVariableType(variableName) : null;
        }

        public string ResolveReturnType(string functionName)
        {
            lock (gate)
            {
                // Core Unreal functions must not be displaced by incomplete macro-heavy
                // declarations discovered while scanning engine headers.
                if (BuiltInReturnTypes.TryGetValue(functionName, out var builtInType))
                {
                    return builtInType;
                }

                if (returnTypesByName.TryGetValue(functionName, out var typeName) &&
                    !string.IsNullOrWhiteSpace(typeName))
                {
                    return typeName;
                }
                return null;
            }
        }

        public string ResolveReturnType(string ownerType, string functionName)
        {
            foreach (var owner in GetTypeHierarchy(NormalizeTypeName(ownerType)))
            {
                lock (gate)
                {
                    if (returnTypesByOwnerAndName.TryGetValue(MemberKey(owner, functionName), out var typeName))
                        return typeName;
                }
            }
            return string.IsNullOrEmpty(ownerType) ? ResolveReturnType(functionName) : null;
        }

        /// <summary>Resolve overloaded operator-> chains without following raw pointers or cycles.</summary>
        public string ResolveArrowTargetType(string valueType)
        {
            var current = valueType;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (var depth = 0; depth < 16 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                var normalized = NormalizeTypeName(current);
                if (Regex.IsMatch(current, @"\*\s*(?:const\s*)?$")) return normalized;
                if (!visited.Add(normalized)) return null;
                var target = ResolveReturnType(normalized, "operator->");
                if (string.IsNullOrWhiteSpace(target)) return normalized;
                current = target;
            }
            return null;
        }

        public string FindCorrectTypeCasing(string name)
        {
            lock (gate)
            {
                return typeNamesByInsensitiveName.TryGetValue(name, out var correct) && !correct.Equals(name, StringComparison.Ordinal)
                    ? correct
                    : null;
            }
        }

        public string ResolveBaseType(string typeName)
        {
            lock (gate)
            {
                return baseTypesByName.TryGetValue(NormalizeTypeName(typeName), out var baseType) ? baseType : null;
            }
        }

        public IReadOnlyList<IndexedSymbol> FindSymbolDetails(string name)
        {
            if (string.IsNullOrEmpty(name)) return Array.Empty<IndexedSymbol>();
            lock (gate)
                return definitionsByName.TryGetValue(name, out var found)
                    ? found.Where(item => item.Name == name && !item.IsLocal && !string.IsNullOrWhiteSpace(item.FilePath)).ToArray()
                    : Array.Empty<IndexedSymbol>();
        }

        public IReadOnlyList<SourceSymbol> FindDefinitions(string name)
        {
            lock (gate)
            {
                return definitionsByName.TryGetValue(name, out var definitions)
                    ? definitions.Where(item => !string.IsNullOrWhiteSpace(item.FilePath)).Select(ToSourceSymbol).ToArray()
                    : Array.Empty<SourceSymbol>();
            }
        }

        public bool ContainsSymbol(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (gate) return definitionsByName.ContainsKey(name);
        }

        public bool ContainsMember(string ownerType, string memberName)
        {
            if (string.IsNullOrWhiteSpace(ownerType) || string.IsNullOrWhiteSpace(memberName)) return false;
            foreach (var owner in GetTypeHierarchy(NormalizeTypeName(ownerType)))
            {
                lock (gate)
                {
                    if (membersByOwner.TryGetValue(owner, out var members) &&
                        members.Any(item => string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            return false;
        }

        public IReadOnlyList<SourceSymbol> FindReferences(string name, string activeFile = null, int line = 0, int column = 0, string activeText = null, CancellationToken cancellation = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Array.Empty<SourceSymbol>();
            }

            string[] files;
            lock (gate)
            {
                files = sourceFiles.ToArray();
            }
            if (!string.IsNullOrEmpty(activeFile)) files = files.Concat(new[] { activeFile }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            var results = new ConcurrentBag<SourceSymbol>();
            int? localDeclaration = null;
            if (!string.IsNullOrEmpty(activeFile) && line > 0)
            {
                try
                {
                    var context = new CppReferenceContext(activeText ?? File.ReadAllText(activeFile));
                    var offset = OffsetAt(context.Code, line, column);
                    localDeclaration = context.DeclarationAt(name, offset);
                    if (localDeclaration.HasValue) files = new[] { activeFile };
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            Parallel.ForEach(files, CreateParallelOptions(cancellation), file =>
            {
                string text;
                try { text = activeText != null && string.Equals(file, activeFile, StringComparison.OrdinalIgnoreCase) ? activeText : File.ReadAllText(file); }
                catch (IOException) { return; }
                catch (UnauthorizedAccessException) { return; }

                var context = new CppReferenceContext(text);
                var offsets = context.References(name, localDeclaration);
                var currentLine = 1; var lineStart = 0; var position = 0;
                foreach (var offset in offsets)
                {
                    cancellation.ThrowIfCancellationRequested();
                    while (position < offset) { if (text[position++] == '\n') { currentLine++; lineStart = position; } }
                    results.Add(new SourceSymbol(name, file, currentLine, offset - lineStart + 1, SymbolKind.Variable));
                }
            });

            return results.OrderBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Line).ThenBy(item => item.Column).ToArray();
        }

        private static int OffsetAt(string text, int line, int column)
        {
            var position = 0;
            for (var current = 1; current < line && position < text.Length; current++)
            { var end = text.IndexOf('\n', position); position = end < 0 ? text.Length : end + 1; }
            return Math.Min(text.Length, position + Math.Max(0, column - 1));
        }

        public void Save(string cachePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            List<string> lines;
            lock (gate)
            {
                lines = new List<string> { "CODEASSIST7", Encode(ProjectRoot), Encode(EngineRoot), LastUpdatedUtc.Ticks.ToString() };
                lines.AddRange(sourceFiles.Select(file => "@FILE\t" + Encode(file)));
                lines.AddRange(symbols.Select(item => string.Join("\t", Encode(item.Name), (int)item.Kind, Encode(item.FilePath), item.Line, item.Column, Encode(item.OwnerType), Encode(item.ValueType), Encode(item.BaseType), Encode(item.ScopeId), Encode(item.Signature), item.IsLocal ? "1" : "0")));
            }
            var temporaryPath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllLines(temporaryPath, lines, Encoding.UTF8);
                if (File.Exists(cachePath)) File.Replace(temporaryPath, cachePath, null);
                else File.Move(temporaryPath, cachePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        public bool Load(string cachePath)
        {
            if (!File.Exists(cachePath)) return false;
            try
            {
                var lines = File.ReadAllLines(cachePath, Encoding.UTF8);
                if (lines.Length < 4 || lines[0] != "CODEASSIST7") return false;
                var loaded = new List<IndexedSymbol>();
                var files = new List<string>();
                foreach (var line in lines.Skip(4))
                {
                    var fields = line.Split('\t');
                    if (fields.Length == 2 && fields[0] == "@FILE") { files.Add(Decode(fields[1])); continue; }
                    if (fields.Length != 11) return false;
                    loaded.Add(new IndexedSymbol(Decode(fields[0]), (SymbolKind)int.Parse(fields[1]), Decode(fields[2]), int.Parse(fields[3]), int.Parse(fields[4]), Decode(fields[5]), Decode(fields[6]), Decode(fields[7]), Decode(fields[8]), Decode(fields[9]), fields[10] == "1"));
                }
                ApplySymbols(
                    loaded,
                    Decode(lines[1]),
                    Decode(lines[2]),
                    new DateTime(long.Parse(lines[3]), DateTimeKind.Utc), files);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is FormatException)
            {
                return false;
            }
        }

        private HashSet<string> GetTypeHierarchy(string typeName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { typeName };
            lock (gate)
            {
                var current = typeName;
                while (true)
                {
                    var baseType = baseTypesByName.TryGetValue(current, out var indexedBaseType) ? indexedBaseType : null;
                    if (string.IsNullOrWhiteSpace(baseType) || !result.Add(baseType)) break;
                    current = baseType;
                }
            }
            return result;
        }

        private static void ParseFile(string filePath, Action<IndexedSymbol> addSymbol)
        {
            string[] lines;
            try { lines = File.ReadAllLines(filePath); }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }

            ParseLines(filePath, lines, addSymbol);
        }

        public static IReadOnlyList<IndexedSymbol> ReadDocumentSymbols(string filePath, string text)
        {
            var found = new List<IndexedSymbol>();
            ParseLines(filePath, (text ?? string.Empty).Replace("\r\n", "\n").Split('\n'), found.Add);
            return found;
        }

        private static void ParseLines(string filePath, string[] lines, Action<IndexedSymbol> addSymbol)
        {

            var context = new CppReferenceContext(string.Join("\n", lines));
            lines = context.Code.Split('\n');
            var offset = 0;
            var typeScopes = new Stack<TypeScope>();
            string pendingType = null;
            var braceDepth = 0;
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                Action<IndexedSymbol> emit = item => addSymbol(new IndexedSymbol(item.Name, item.Kind, item.FilePath, item.Line, item.Column,
                    item.OwnerType, item.ValueType, item.BaseType, context.ScopeAt(offset + Math.Max(0, item.Column - 1)),
                    item.Kind == SymbolKind.Function ? ReadFunctionSignature(line,Math.Max(0,item.Column-1)) : "",
                    item.Kind == SymbolKind.Variable && context.IsExecutableAt(offset + Math.Max(0, item.Column - 1))));
                var macroMatch=Regex.Match(line,@"^\s*#\s*define\s+(?<name>[A-Za-z_]\w*)");
                if(macroMatch.Success)
                {
                    var group=macroMatch.Groups["name"];
                    emit(new IndexedSymbol(group.Value,SymbolKind.Macro,filePath,index+1,group.Index+1));
                    offset+=line.Length+1;
                    continue;
                }
                var typeMatch = TypePattern.Match(line);
                if (typeMatch.Success)
                {
                    var typeName = typeMatch.Groups["name"].Value;
                    emit(new IndexedSymbol(typeName, SymbolKind.Type, filePath, index + 1, typeMatch.Groups["name"].Index + 1, baseType: typeMatch.Groups["base"].Value));
                    var declarationTail = line.Substring(typeMatch.Index + typeMatch.Length);
                    if (declarationTail.Contains("{"))
                    {
                        typeScopes.Push(new TypeScope(typeName, braceDepth + 1));
                        pendingType = null;
                    }
                    else if (!declarationTail.Contains(";"))
                    {
                        pendingType = typeName;
                    }
                }
                else if (pendingType != null && line.Contains("{"))
                {
                    typeScopes.Push(new TypeScope(pendingType, braceDepth + 1));
                    pendingType = null;
                }

                var constructedVariableMatch = ConstructedVariablePattern.Match(line);
                var insideExecutableScope = context.IsExecutableAt(offset + constructedVariableMatch.Groups["name"].Index);
                if (insideExecutableScope && constructedVariableMatch.Success && line.TrimEnd().EndsWith(";", StringComparison.Ordinal))
                {
                    emit(new IndexedSymbol(
                        constructedVariableMatch.Groups["name"].Value,
                        SymbolKind.Variable,
                        filePath,
                        index + 1,
                        constructedVariableMatch.Groups["name"].Index + 1,
                        typeScopes.Count == 0 ? null : typeScopes.Peek().Name,
                        constructedVariableMatch.Groups["type"].Value.Trim()));
                }
                else
                {
                    var constructorMatch = ConstructorPattern.Match(line);
                    var functionMatch = FunctionPattern.Match(line);
                    var selectedFunction = functionMatch.Success ? functionMatch : constructorMatch;
                    if (selectedFunction.Success)
                    {
                        var owner = selectedFunction.Groups["owner"].Success ? selectedFunction.Groups["owner"].Value : typeScopes.Count == 0 ? null : typeScopes.Peek().Name;
                        var name = selectedFunction.Groups["name"].Value;
                        if (!IsControlKeyword(name) && UnrealMacroParser.Find(name).Count == 0)
                        {
                            var valueType = selectedFunction.Groups["type"].Success ? selectedFunction.Groups["type"].Value : owner;
                            emit(new IndexedSymbol(name, SymbolKind.Function, filePath, index + 1, selectedFunction.Groups["name"].Index + 1, owner, valueType));
                        }
                    }
                    else if (!line.Contains("("))
                    {
                        var variableMatch = VariablePattern.Match(line);
                        if (variableMatch.Success)
                        {
                            emit(new IndexedSymbol(variableMatch.Groups["name"].Value, SymbolKind.Variable, filePath, index + 1, variableMatch.Groups["name"].Index + 1, typeScopes.Count == 0 ? null : typeScopes.Peek().Name, variableMatch.Groups["type"].Value.Trim()));
                        }
                    }
                }

                // Inline members can share a line with a function; do not drop the field.
                if(line.Contains("("))
                    foreach(Match member in VariablePattern.Matches(line))
                    {
                        var group=member.Groups["name"];
                        if(group.Index < line.IndexOf('(') && !context.IsExecutableAt(offset+group.Index) && !IsControlKeyword(member.Groups["type"].Value.Trim()))
                            emit(new IndexedSymbol(group.Value,SymbolKind.Variable,filePath,index+1,group.Index+1,typeScopes.Count==0?null:typeScopes.Peek().Name,member.Groups["type"].Value.Trim()));
                    }
                braceDepth += line.Count(character => character == '{');
                braceDepth -= line.Count(character => character == '}');
                while (typeScopes.Count > 0 && braceDepth < typeScopes.Peek().Depth)
                {
                    typeScopes.Pop();
                }
                offset += line.Length + 1;
            }
        }

        private static string ReadFunctionSignature(string line,int start)
        {
            var depth=0;var end=start;
            for(;end<line.Length;end++) {
                var ch=line[end];
                if(ch=='(')depth++;
                else if(ch==')')depth=Math.Max(0,depth-1);
                else if(depth==0&&(ch==';'||ch=='{'||ch=='}'))break;
            }
            return line.Substring(start,end-start).Trim();
        }

        private sealed class TypeScope
        {
            public TypeScope(string name, int depth)
            {
                Name = name;
                Depth = depth;
            }

            public string Name { get; }
            public int Depth { get; }
        }

        public static string[] InputFiles(string root, string projectRoot = null,string[] extensions=null)
        {
            return (projectRoot == null ? EnumerateProjectFiles(root,extensions) : EnumerateEngineApiFiles(root, CollectEngineModules(projectRoot),extensions))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static IEnumerable<string> EnumerateProjectFiles(string root,string[] extensions=null)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) yield break;
            foreach (var file in EnumerateFiles(root, directory => !IgnoredDirectories.Contains(Path.GetFileName(directory)),extensions)) yield return file;
        }

        private static IEnumerable<string> EnumerateEngineApiFiles(string engineRoot, ISet<string> modules,string[] extensions=null)
        {
            foreach (var moduleRoot in EnumerateEngineModuleRoots(engineRoot, modules))
            {
                foreach (var apiDirectory in new[] { Path.Combine(moduleRoot, "Public"), Path.Combine(moduleRoot, "Classes") })
                {
                    if (!Directory.Exists(apiDirectory)) continue;
                    foreach (var file in EnumerateFiles(apiDirectory, directory => !IgnoredDirectories.Contains(Path.GetFileName(directory)),extensions))
                    {
                        if (!file.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase)) yield return file;
                    }
                }
            }
        }

        private static IEnumerable<string> EnumerateEngineModuleRoots(string engineRoot, ISet<string> modules)
        {
            var runtimeRoot = Path.Combine(engineRoot, "Engine", "Source", "Runtime");
            foreach (var module in modules)
            {
                var runtimeModule = Path.Combine(runtimeRoot, module);
                if (Directory.Exists(runtimeModule)) yield return runtimeModule;
            }

            // Plugin APIs (for example Enhanced Input) live below
            // Engine/Plugins/.../Source/<Module>, not Engine/Source/Runtime.
            // Walk directories only, stop below a matched module, and parse just
            // its Public/Classes headers so the background scan remains bounded.
            var pluginsRoot = Path.Combine(engineRoot, "Engine", "Plugins");
            if (!Directory.Exists(pluginsRoot)) yield break;
            var pending = new Stack<string>();
            pending.Push(pluginsRoot);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                var parent = Path.GetDirectoryName(directory);
                if (modules.Contains(Path.GetFileName(directory)) &&
                    string.Equals(Path.GetFileName(parent), "Source", StringComparison.OrdinalIgnoreCase))
                {
                    yield return directory;
                    continue;
                }

                string[] children;
                try { children = Directory.GetDirectories(directory); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (var child in children) pending.Push(child);
            }
        }

        private static ISet<string> CollectEngineModules(string projectRoot)
        {
            var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Core", "CoreUObject", "Engine" };
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot)) return modules;
            foreach (var buildFile in Directory.EnumerateFiles(projectRoot, "*.Build.cs", SearchOption.AllDirectories))
            {
                string text;
                try { text = File.ReadAllText(buildFile); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (Match match in ModuleNamePattern.Matches(text)) modules.Add(match.Groups["name"].Value);
            }
            return modules;
        }

        private static IEnumerable<string> EnumerateFiles(string root, Func<string, bool> includeDirectory,string[] extensions=null)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                string[] files;
                string[] directories;
                try { files = Directory.GetFiles(directory); directories = Directory.GetDirectories(directory); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                foreach (var file in files) if ((SourceExtensions.Contains(Path.GetExtension(file))||(extensions??Array.Empty<string>()).Contains(Path.GetExtension(file),StringComparer.OrdinalIgnoreCase))) yield return file;
                foreach (var child in directories) if (includeDirectory(child)) pending.Push(child);
            }
        }

        private static string StripLineComment(string line)
        {
            var comment = line.IndexOf("//", StringComparison.Ordinal);
            return comment < 0 ? line : line.Substring(0, comment);
        }

        private static string StripComments(string line, ref bool insideBlockComment)
        {
            var result = new StringBuilder(line?.Length ?? 0);
            var index = 0;
            while (index < (line?.Length ?? 0))
            {
                if (insideBlockComment)
                {
                    var end = line.IndexOf("*/", index, StringComparison.Ordinal);
                    if (end < 0) return result.ToString();
                    insideBlockComment = false;
                    index = end + 2;
                    continue;
                }
                if (index + 1 < line.Length && line[index] == '/' && line[index + 1] == '/') break;
                if (index + 1 < line.Length && line[index] == '/' && line[index + 1] == '*')
                {
                    insideBlockComment = true;
                    index += 2;
                    continue;
                }
                result.Append(line[index]);
                index++;
            }
            return result.ToString();
        }

        private static bool IsControlKeyword(string name)
        {
            return name == "if" || name == "for" || name == "while" || name == "switch" || name == "catch";
        }

        private static SourceSymbol ToSourceSymbol(IndexedSymbol item)
        {
            return new SourceSymbol(item.Name, item.FilePath, item.Line, item.Column, item.Kind);
        }

        private static bool Matches(string name, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var queryIndex = 0;
            for (var index = 0; index < name.Length && queryIndex < query.Length; index++)
            {
                if (char.ToUpperInvariant(name[index]) == char.ToUpperInvariant(query[queryIndex])) queryIndex++;
            }
            return queryIndex == query.Length;
        }

        private void ApplySymbols(List<IndexedSymbol> newSymbols, string projectRoot, string engineRoot, DateTime updatedUtc, IEnumerable<string> files = null)
        {
            var lookups = BuildLookups(newSymbols);
            var newFiles = (files ?? newSymbols.Select(item => item.FilePath)).Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToArray();
            lock (gate)
            {
                symbols = newSymbols;
                sourceFiles = newFiles;
                completionsByPrefix = lookups.CompletionsByPrefix;
                membersByOwner = lookups.MembersByOwner;
                typeNamesByInsensitiveName = lookups.TypeNamesByInsensitiveName;
                variableTypesByName = lookups.VariableTypesByName;
                returnTypesByName = lookups.ReturnTypesByName;
                variableTypesByOwnerAndName = lookups.VariableTypesByOwnerAndName;
                returnTypesByOwnerAndName = lookups.ReturnTypesByOwnerAndName;
                baseTypesByName = lookups.BaseTypesByName;
                definitionsByName = lookups.DefinitionsByName;
                specifierSymbols = lookups.Specifiers;
                headerSymbols = lookups.Headers;
                ProjectRoot = projectRoot;
                EngineRoot = engineRoot;
                LastUpdatedUtc = updatedUtc;
            }
        }

        private static LookupTables BuildLookups(List<IndexedSymbol> sourceSymbols)
        {
            var distinctCompletions = sourceSymbols
                .Where(item => !item.IsLocal && !string.IsNullOrEmpty(item.Name) && item.Kind != SymbolKind.Specifier && item.Kind != SymbolKind.Header)
                .GroupBy(item => item.Name, StringComparer.Ordinal)
                .Select(group => group.OrderBy(item => item.Kind).First())
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var completions = new Dictionary<string, List<IndexedSymbol>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in distinctCompletions)
            {
                for (var length = 1; length <= Math.Min(2, item.Name.Length); length++)
                {
                    var key = item.Name.Substring(0, length);
                    if (!completions.TryGetValue(key, out var values))
                    {
                        values = new List<IndexedSymbol>();
                        completions[key] = values;
                    }
                    values.Add(item);
                }
            }
            var members = sourceSymbols
                .Where(item => !item.IsLocal && !string.IsNullOrEmpty(item.OwnerType))
                .GroupBy(item => item.OwnerType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var typeNames = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Type && !string.IsNullOrEmpty(item.Name))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
            var variableTypes = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Variable && !item.IsLocal && !string.IsNullOrEmpty(item.ValueType))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => UniqueValueType(group), StringComparer.OrdinalIgnoreCase);
            var ownerVariableTypes = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Variable && !item.IsLocal && !string.IsNullOrEmpty(item.OwnerType) && !string.IsNullOrEmpty(item.ValueType))
                .GroupBy(item => MemberKey(item.OwnerType, item.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => UniqueValueType(group), StringComparer.OrdinalIgnoreCase);
            var returnTypes = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Function && !string.IsNullOrEmpty(item.ValueType))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => SelectReturnType(group),
                    StringComparer.OrdinalIgnoreCase);
            var ownerReturnTypes = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Function && !string.IsNullOrEmpty(item.OwnerType) && !string.IsNullOrEmpty(item.ValueType))
                .GroupBy(item => MemberKey(item.OwnerType, item.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => SelectReturnType(group), StringComparer.OrdinalIgnoreCase);
            var baseTypes = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Type && !string.IsNullOrEmpty(item.BaseType))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(item => item.BaseType)
                    .First(value => !string.IsNullOrWhiteSpace(value)), StringComparer.OrdinalIgnoreCase);
            var definitions = sourceSymbols
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var specifiers = sourceSymbols
                .Where(item => item.Kind == SymbolKind.Specifier)
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .ToList();
            var headers = sourceSymbols
                .Where(item => IsHeader(item.FilePath))
                .Select(item => GetIncludePath(item.FilePath))
                .Concat(sourceSymbols.Where(item => item.Kind == SymbolKind.Header).Select(item => item.Name))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(Header)
                .ToList();
            return new LookupTables(completions, members, typeNames, variableTypes, returnTypes,
                ownerVariableTypes, ownerReturnTypes, baseTypes, definitions, specifiers, headers);
        }

        private static string SelectReturnType(IEnumerable<IndexedSymbol> candidates)
        {
            var values = candidates.Where(item => !IsTemplatePlaceholderType(item.ValueType)).ToArray();
            return UniqueValueType(values.Length > 0 ? values : candidates);
        }

        private static string UniqueValueType(IEnumerable<IndexedSymbol> candidates)
        {
            var types = candidates.Select(item => item.ValueType).Distinct(StringComparer.Ordinal).Take(2).ToArray();
            return types.Length == 1 ? types[0] : null;
        }

        private static bool IsTemplatePlaceholderType(string typeName)
        {
            var normalized = NormalizeTypeName(typeName);
            return normalized == "T" || Regex.IsMatch(normalized, @"^T[A-Z][A-Za-z0-9_]*$");
        }

        private static string MemberKey(string ownerType, string memberName)
        {
            return (ownerType ?? string.Empty) + "\0" + (memberName ?? string.Empty);
        }

        private sealed class LookupTables
        {
            public LookupTables(
                Dictionary<string, List<IndexedSymbol>> completions,
                Dictionary<string, List<IndexedSymbol>> members,
                Dictionary<string, string> typeNames,
                Dictionary<string, string> variableTypes,
                Dictionary<string, string> returnTypes,
                Dictionary<string, string> variableTypesByOwnerAndName,
                Dictionary<string, string> returnTypesByOwnerAndName,
                Dictionary<string, string> baseTypes,
                Dictionary<string, List<IndexedSymbol>> definitions,
                List<IndexedSymbol> specifiers,
                List<IndexedSymbol> headers)
            {
                CompletionsByPrefix = completions;
                MembersByOwner = members;
                TypeNamesByInsensitiveName = typeNames;
                VariableTypesByName = variableTypes;
                ReturnTypesByName = returnTypes;
                VariableTypesByOwnerAndName = variableTypesByOwnerAndName;
                ReturnTypesByOwnerAndName = returnTypesByOwnerAndName;
                BaseTypesByName = baseTypes;
                DefinitionsByName = definitions;
                Specifiers = specifiers;
                Headers = headers;
            }

            public Dictionary<string, List<IndexedSymbol>> CompletionsByPrefix { get; }
            public Dictionary<string, List<IndexedSymbol>> MembersByOwner { get; }
            public Dictionary<string, string> TypeNamesByInsensitiveName { get; }
            public Dictionary<string, string> VariableTypesByName { get; }
            public Dictionary<string, string> ReturnTypesByName { get; }
            public Dictionary<string, string> VariableTypesByOwnerAndName { get; }
            public Dictionary<string, string> ReturnTypesByOwnerAndName { get; }
            public Dictionary<string, string> BaseTypesByName { get; }
            public Dictionary<string, List<IndexedSymbol>> DefinitionsByName { get; }
            public List<IndexedSymbol> Specifiers { get; }
            public List<IndexedSymbol> Headers { get; }
        }

        private static string GetPrefixKey(string prefix)
        {
            return prefix.Substring(0, Math.Min(2, prefix.Length));
        }

        private static bool IsHeader(string filePath)
        {
            var extension = Path.GetExtension(filePath ?? string.Empty);
            return extension.Equals(".h", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".hpp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".hh", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".inl", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetIncludePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;
            var normalized = filePath.Replace('\\', '/');
            foreach (var marker in new[] { "/Public/", "/Classes/" })
            {
                var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex >= 0) return normalized.Substring(markerIndex + marker.Length);
            }

            var sourceIndex = normalized.IndexOf("/Source/", StringComparison.OrdinalIgnoreCase);
            if (sourceIndex >= 0)
            {
                var moduleStart = sourceIndex + "/Source/".Length;
                var moduleEnd = normalized.IndexOf('/', moduleStart);
                if (moduleEnd >= 0 && moduleEnd + 1 < normalized.Length) return normalized.Substring(moduleEnd + 1);
            }
            return Path.GetFileName(filePath);
        }

        private static int HeaderMatchRank(string path, string query)
        {
            if (string.IsNullOrEmpty(query)) return 3;
            if (path.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (Path.GetFileName(path).StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            if (path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return int.MaxValue;
        }

        private ParallelOptions CreateParallelOptions(CancellationToken cancellation = default(CancellationToken))
        {
            return new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(ParseConcurrency, Math.Max(1, Environment.ProcessorCount))
                , CancellationToken = cancellation
            };
        }

        private static int MatchRank(string name, string query)
        {
            if (string.IsNullOrEmpty(query)) return 2;
            if (name.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return 3;
        }

        private static int CompletionMatchRank(string name, string query)
        {
            if (name.Equals(query, StringComparison.Ordinal)) return 0;
            if (name.StartsWith(query, StringComparison.Ordinal)) return 1;
            if (name.Equals(query, StringComparison.OrdinalIgnoreCase)) return 2;
            return 3;
        }

        private static int CompletionMemberRank(string name, string query, bool acronyms, bool shorthand)
        {
            // Preserve the expanded member-list matching when shorthand is enabled.
            // Symbol search keeps its independent fuzzy matcher below.
            if (shorthand) return MemberMatchRank(name, query);
            if (string.IsNullOrEmpty(query)) return 4;
            if (name.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            return acronyms && IdentifierMatching.Acronym(name).StartsWith(query, StringComparison.OrdinalIgnoreCase)
                ? 2 : int.MaxValue;
        }

        private static int MemberMatchRank(string name, string query)
        {
            if (string.IsNullOrEmpty(query)) return 4;
            if (name.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 2;

            var queryIndex = 0;
            for (var index = 0; index < name.Length && queryIndex < query.Length; index++)
            {
                if (char.ToUpperInvariant(name[index]) == char.ToUpperInvariant(query[queryIndex])) queryIndex++;
            }
            return queryIndex == query.Length ? 3 : int.MaxValue;
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}
