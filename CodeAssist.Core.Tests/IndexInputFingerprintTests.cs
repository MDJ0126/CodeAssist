using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public class IndexInputFingerprintTests
{
    [Fact]
    public void UnchangedFilesReuseFingerprintButEditsAddsAndDeletesInvalidate()
    {
        var root = Path.Combine(Path.GetTempPath(), "CodeAssistInputs-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try {
            var a = Path.Combine(root, "a.h"); File.WriteAllText(a, "class A {};");
            var first = IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root));
            Assert.Equal(first, IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root)));
            File.WriteAllText(a, "class Changed {};");
            var edited = IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root)); Assert.NotEqual(first, edited);
            var b = Path.Combine(root, "b.h"); File.WriteAllText(b, "class B {};");
            var added = IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root)); Assert.NotEqual(edited, added);
            File.Delete(b); Assert.Equal(edited, IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root)));
            var ignored = Path.Combine(root, "Intermediate"); Directory.CreateDirectory(ignored); File.WriteAllText(Path.Combine(ignored, "generated.h"), "class Ignore {};");
            Assert.Equal(edited, IndexInputFingerprint.Create(PersistentSymbolIndex.InputFiles(root)));
        } finally { Directory.Delete(root, true); }
    }
}
