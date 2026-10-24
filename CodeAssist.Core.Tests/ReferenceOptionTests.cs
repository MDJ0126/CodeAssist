using CodeAssist.Core;
using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public sealed class ReferenceOptionTests {
 [Fact] public void AllEditableOptionsSurviveDraftCopyAndDiskRoundTrip(){
  var original=new StartupPreferences();var draft=original.Copy();
  var properties=typeof(StartupPreferences).GetProperties().Where(p=>p.CanRead&&p.CanWrite&&new[]{typeof(bool),typeof(int),typeof(string)}.Contains(p.PropertyType)).ToArray();
  foreach(var property in properties){if(property.PropertyType==typeof(bool))property.SetValue(draft,!(bool)property.GetValue(draft)!);else if(property.PropertyType==typeof(int))property.SetValue(draft,7);else property.SetValue(draft,"option value 한글");}
  var path=Path.Combine(Path.GetTempPath(),"CA-options-"+Guid.NewGuid()+".xml");
  try{draft.Save(path);var saved=StartupPreferences.Load(path);foreach(var property in properties)Assert.Equal(property.GetValue(draft),property.GetValue(saved));Assert.NotEqual(original.UserIncludeDirectories,saved.UserIncludeDirectories);}
  finally{File.Delete(path);File.Delete(path+".bak");}
 }
 [Fact] public void DirectoryExclusionUsesPathBoundaries(){var p=new StartupPreferences {UserExcludedDirectories=Path.Combine(Path.GetTempPath(),"CA-excluded")};Assert.False(p.IncludeProjectPath(Path.Combine(Path.GetTempPath(),"CA-excluded","file.h")));Assert.True(p.IncludeProjectPath(Path.Combine(Path.GetTempPath(),"CA-excluded-other","file.h")));}
 [Fact] public void ParserThreadSwitchAndAlternateCacheHaveEffects(){var p=new StartupPreferences {ParseConcurrency=7,ParsingMultithreaded=false};Assert.Equal(1,p.EffectiveParseConcurrency);p.ParsingMultithreaded=true;Assert.Equal(7,p.EffectiveParseConcurrency);p.UseAlternateSymbolDirectory=true;p.AlternateSymbolDirectory="alternate";Assert.Equal("alternate",p.SymbolDatabaseDirectory("default"));}
 [Fact] public void HashtagOptionsChangeActualCommentCandidates(){var source="// #ab #abcdef #two-words #go\nconst char* s=\"#not_a_comment\";";var p=new StartupPreferences {HashtagMinimumLength=3,HashtagAllowHyphens=true};var tags=CppDocumentNavigation.Hashtags(source,p);Assert.Single(tags);Assert.Equal("#two-words",tags[0].Name);p.HashtagAllowHex=true;Assert.Equal(2,CppDocumentNavigation.Hashtags(source,p).Count);}
 [Fact] public void LegacyHashtagCallRetainsShortTags(){Assert.Equal(2,CppDocumentNavigation.Hashtags("// #a #abc").Count);}
 [Fact] public void GeneratedDefaultValuesAreAnActualSwitch(){var noDefaults=CppRefactoring.CreateImplementation("void Tick(int count = 3);","Actor");var withDefaults=CppRefactoring.CreateImplementation("void Tick(int count = 3);","Actor",includeDefaults:true);Assert.DoesNotContain("= 3",noDefaults);Assert.Contains("= 3",withDefaults);}
 [Fact] public void SettingsBackupCanBeDisabled(){var path=Path.Combine(Path.GetTempPath(),"CA-no-backup-"+Guid.NewGuid()+".xml");try{var p=new StartupPreferences {RecoveryKeepBackup=false};p.Save(path);p.ClipboardLimit=12;p.Save(path);Assert.False(File.Exists(path+".bak"));Assert.Equal(12,StartupPreferences.Load(path).ClipboardLimit);}finally{File.Delete(path);File.Delete(path+".bak");}}
 [Fact] public void ExplicitIncludeRootProducesRelativeHeaderCandidate(){var root=Path.Combine(Path.GetTempPath(),"CA-include-"+Guid.NewGuid());var model=ProjectCompilationModel.Load(Path.GetTempPath());var file=Path.Combine(root,"sub","Thing.h");var candidate=Assert.Single(model.HeaderCandidates(new[]{file},new[]{root}));Assert.Equal("sub/Thing.h",candidate.Name);}
}
