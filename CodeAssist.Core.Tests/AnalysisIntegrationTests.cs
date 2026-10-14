using CodeAssist.Core;
using CodeAssist.Indexing;

namespace CodeAssist.Core.Tests;

public class AnalysisIntegrationTests
{
    [Fact]
    public void NativeObservedUnrealMacrosExcludeCommentsAndLiterals()
    {
        var text = "/* UCLASS()\n UDELEGATE() */\n\"RIGVM_METHOD()\";\n" +
            "UDELEGATE() GENERATED_UINTERFACE_BODY() GENERATED_IINTERFACE_BODY() RIGVM_METHOD()\n" +
            "UDELEGATE_SUFFIX()";
        var macros = UnrealMacroParser.Find(text);
        Assert.Equal(new[] { "UDELEGATE", "GENERATED_UINTERFACE_BODY", "GENERATED_IINTERFACE_BODY", "RIGVM_METHOD" },
            macros.Select(macro => macro.Name));
        Assert.All(macros, macro => Assert.Equal(macro.Name, text.Substring(macro.Start, macro.Length)));
    }
    [Fact]
    public void LiteralPrefixesAndContinuedLineCommentsAreMasked()
    {
        var text = "auto a=L\"Thing\"; auto b=u8R\"x(Thing)x\"; auto c=L'x'; // Thing\\\r\nThing\r\nThing();";
        var context = new CppReferenceContext(text);
        Assert.Single(context.References("Thing"));
        Assert.Empty(context.References("L")); Assert.Empty(context.References("u8"));
    }
    [Fact]
    public void OwnerContextDoesNotLeakPastFunctionAndUnderstandsInlineClass()
    {
        var closed = "void A::One() {}\nvoid Free() {\n";
        Assert.Null(CppExpressionContext.FindEnclosingFunctionOwner(closed, closed.Length));
        var inline = "class A : public Base { void Run() { if (true) {\n";
        Assert.Equal("A", CppExpressionContext.FindEnclosingFunctionOwner(inline, inline.Length));
    }

    [Fact]
    public void NamespaceMembersAreNotMisclassifiedAsFunctionLocals()
    {
        var symbols = PersistentSymbolIndex.ReadDocumentSymbols("Test.h", "namespace Game {\n int Global;\n void Run();\n}\n");
        Assert.Contains(symbols, s => s.Name == "Global" && !s.IsLocal);
        Assert.Contains(symbols, s => s.Name == "Run" && s.Kind == SymbolKind.Function);
    }
    [Fact]
    public void MaskPreservesOffsetsAndExcludesEscapedAndRawLiterals()
    {
        var text = "Thing(); /* Thing\n */ const char* s = \"a\\\"Thing\";\n" +
            "auto raw = R\"tag(Thing\n\" /* Thing */)tag\"; // Thing\nThing(); char ch='\\''; int n=1'000;";
        var mask = CppCodeMask.Create(text);
        Assert.Equal(text.Length, mask.Length);
        Assert.Equal(text.Count(ch => ch == '\n'), mask.Count(ch => ch == '\n'));
        var references = new CppReferenceContext(text).References("Thing");
        Assert.Equal(2, references.Count);
        Assert.All(references, offset => Assert.Equal("Thing", text.Substring(offset, 5)));
        Assert.Contains("1'000", mask);
    }

    [Fact]
    public void ReferencesRespectShadowedLocalsAndExcludeQualifiedMembers()
    {
        var text = "void One() { int Value=0; Use(Value); { int Value=1; Use(Value); } Use(Value); obj.Value=2; }\n" +
            "void Two() { int Value=3; Use(Value); }";
        var context = new CppReferenceContext(text);
        var outer = context.DeclarationAt("Value", text.IndexOf("Use(Value)") + 4);
        Assert.NotNull(outer);
        Assert.Equal(3, context.References("Value", outer).Count);
        Assert.Null(context.DeclarationAt("Value", text.IndexOf("obj.Value") + 4));
        var inner = context.DeclarationAt("Value", text.IndexOf("Use(Value)", text.IndexOf("int Value=1")) + 4);
        Assert.NotEqual(outer, inner);
        Assert.Equal(2, context.References("Value", inner).Count);
    }

    [Fact]
    public void DocumentRevisionsRejectOutOfOrderAndClosedDocumentResults()
    {
        var store = new DocumentRevisionStore<string>();
        var old = store.Begin("A.cpp"); var current = store.Begin("A.cpp");
        Assert.True(store.Publish("A.cpp", current, "new"));
        Assert.False(store.Publish("A.cpp", old, "old"));
        store.Remove("A.cpp");
        Assert.False(store.Publish("A.cpp", current, "closed"));
        var reopened = store.Begin("A.cpp");
        Assert.NotEqual(current, reopened);
        Assert.False(store.Publish("A.cpp", current, "previous session"));
        Assert.True(store.Publish("A.cpp", reopened, "reopened"));
        Assert.Equal(new[] { "reopened" }, store.Snapshot());
    }

    [Fact]
    public void CompletionSelectionFindsReorderedItemAndRejectsRemovedItem()
    {
        Assert.Equal(1, CompletionSelection.Validate("GetWorld", 0, new[] { "GetPawn", "GetWorld" }));
        Assert.Equal(-1, CompletionSelection.Validate("GetWorld", 2, new[] { "GetPawn" }));
        Assert.Equal(-1, CompletionSelection.Validate("GetWorld", -1, new[] { "GetWorld" }));
    }

    [Fact]
    public void IndexPreservesScopeAndOverloadIdentityAndRejectsAmbiguousTypes()
    {
        WithProject(root =>
        {
            File.WriteAllText(Path.Combine(root, "Test.h"), "class A {\n int Value;\n int Get(int x);\n float Get(float x);\n};\nclass B {\n float Value;\n};");
            File.WriteAllText(Path.Combine(root, "Test.cpp"), "void A::Run() {\n int OnlyLocal=0;\n}\n");
            File.WriteAllText(Path.Combine(root, "Empty.cpp"), "// No symbols");
            var index = new PersistentSymbolIndex(); index.BuildProject(root);
            Assert.Null(index.ResolveVariableType("Value"));
            Assert.Equal("int", index.ResolveVariableType("A", "Value"));
            Assert.Null(index.ResolveVariableType("Missing", "Value"));
            Assert.Null(index.ResolveReturnType("A", "Get"));
            Assert.Equal(2, index.SearchSymbols("Get").Select(s => s.Identity).Distinct().Count());
            Assert.Contains(index.RelatedSymbols("Get", "A"), s => s.OwnerType == "A" && s.Name == "Value");
            Assert.DoesNotContain(index.CompleteMembers("A", "Only"), s => s.Name == "OnlyLocal");
            var cache = Path.Combine(root, "cache.index"); index.Save(cache);
            var restored = new PersistentSymbolIndex(); Assert.True(restored.Load(cache));
            Assert.Contains(Path.Combine(root, "Empty.cpp"), restored.SourceFiles());
            Assert.Equal(index.SearchSymbols("Get").Select(s => s.Identity), restored.SearchSymbols("Get").Select(s => s.Identity));
        });
    }

    [Fact]
    public void IndexReferencesUseActiveUnsavedTextAndLocalScope()
    {
        WithProject(root =>
        {
            var file = Path.Combine(root, "Test.cpp");
            File.WriteAllText(file, "void One() {\n int Value=0;\n Use(Value);\n}\n");
            var index = new PersistentSymbolIndex(); index.BuildProject(root);
            var unsaved = "void One() {\n int Value=0;\n Use(Value);\n Use(Value);\n}\nvoid Two() { int Value=1; Use(Value); }\n// Value\n";
            Assert.Equal(3, index.FindReferences("Value", file, 3, 6, unsaved).Count);
        });
    }

    [Fact]
    public void CompilationModelSeparatesTargetsAndInvalidatesConfigurationFingerprint()
    {
        WithProject(root =>
        {
            var file = Path.Combine(root, "Game.vcxproj");
            File.WriteAllText(file, "<Project><PropertyGroup><NMakeIncludeSearchPath>$(ProjectDir)Source;$(Unknown)</NMakeIncludeSearchPath></PropertyGroup>" +
                "<PropertyGroup Condition=\"Debug|x64\"><NMakePreprocessorDefinitions>DEBUG=1</NMakePreprocessorDefinitions></PropertyGroup>" +
                "<PropertyGroup Condition=\"Release|x64\"><NMakePreprocessorDefinitions>RELEASE=1</NMakePreprocessorDefinitions></PropertyGroup></Project>");
            var model = ProjectCompilationModel.Load(root);
            var debug = model.Targets.Single(t => t.Condition == "Debug|x64");
            Assert.Contains("DEBUG=1", debug.Defines); Assert.DoesNotContain("RELEASE=1", debug.Defines);
            Assert.Contains("$(Unknown)", debug.UnresolvedValues);
            Assert.Contains(Path.Combine(root, "Source"), debug.IncludeDirectories);
            var headers = model.HeaderCandidates(new[] { Path.Combine(root, "Source", "Game", "Test.h") });
            Assert.Contains(headers, header => header.Name == "Game/Test.h");
            File.AppendAllText(file, "\n");
            Assert.NotEqual(model.Fingerprint, ProjectCompilationModel.Load(root).Fingerprint);
        });
    }

    [Fact]
    public void MissingWorkerFallsBackAndCancelledBuildDoesNotPublish()
    {
        WithProject(root =>
        {
            File.WriteAllText(Path.Combine(root, "Test.h"), "class Test {};");
            var index = new PersistentSymbolIndex();
            var result = IndexBuildRunner.Build(index, "project", root, root, Path.Combine(root, "Missing.exe"), true);
            Assert.False(result.UsedWorker); Assert.NotNull(result.WorkerFailure);
            Assert.True(index.ContainsSymbol("Test"));
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var cancelled = new PersistentSymbolIndex();
            Assert.Throws<OperationCanceledException>(() => cancelled.BuildProject(root, cancellation.Token));
            Assert.Equal(0, cancelled.Count);
        });
    }
    private static void WithProject(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "CodeAssistTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try { action(root); } finally { Directory.Delete(root, true); }
    }
}
