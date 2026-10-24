using CodeAssist.Core;
using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public class IndexExtensionTests
{
 [Theory][InlineData("ipp;.TPP, ipp", ".ipp;.tpp")][InlineData("", "")]
 public void NormalizesExtensions(string input,string expected){Assert.Equal(expected,string.Join(";",FileExtensionSettings.Parse(input)));}
 [Theory][InlineData("../cpp")][InlineData("*.cpp")][InlineData("cpp|exe")]
 public void RejectsPathsAndWildcards(string input){Assert.Throws<ArgumentException>(()=>FileExtensionSettings.Parse(input));}
 [Fact] public void CustomExtensionsActuallyEnterIndexAndFingerprintInputs()
 {
  var root=Path.Combine(Path.GetTempPath(),"CodeAssistExtensionTest",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try {File.WriteAllText(Path.Combine(root,"Extra.tpp"),"struct ExtraType {};\n");File.WriteAllText(Path.Combine(root,"Native.c"),"struct NativeType {};\n");File.WriteAllText(Path.Combine(root,"Ignored.txt"),"struct IgnoredType {};\n");var extensions=FileExtensionSettings.Parse("tpp");var index=new PersistentSymbolIndex {AdditionalSourceExtensions=extensions,ParseConcurrency=1};index.BuildProject(root);Assert.Single(index.FindDefinitions("ExtraType"));Assert.Single(index.FindDefinitions("NativeType"));Assert.Empty(index.FindDefinitions("IgnoredType"));Assert.Equal(2,PersistentSymbolIndex.InputFiles(root,null,extensions).Length);Assert.Single(PersistentSymbolIndex.InputFiles(root));}
  finally {Directory.Delete(root,true);}
 }
}
