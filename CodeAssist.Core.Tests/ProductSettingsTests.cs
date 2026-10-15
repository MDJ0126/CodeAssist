using CodeAssist.Core;
using CodeAssist.Indexing;
namespace CodeAssist.Core.Tests;
public sealed class ProductSettingsTests
{
    [Fact] public void EditingDraftDoesNotChangeSavedPreferences()
    {
        var saved=new StartupPreferences {ClipboardLimit=10,TypeColor="#FFD700"};
        var draft=saved.Copy();draft.ClipboardLimit=25;draft.TypeColor="#4EC9B0";draft.InspectionMode=InspectionMode.Project;
        Assert.Equal(10,saved.ClipboardLimit);Assert.Equal("#FFD700",saved.TypeColor);Assert.Equal(InspectionMode.Ask,saved.InspectionMode);
    }
    [Fact] public void NewSettingsRoundTrip()
    {
        var path=Path.Combine(Path.GetTempPath(),"CodeAssistSettings-"+Guid.NewGuid()+".xml");
        try {
            var settings=new StartupPreferences {ClipboardLimit=23,LiveUpdateDelay=500,ContextBarEnabled=false,TypeColor="#4EC9B0",InspectionMode=InspectionMode.PerFile};
            settings.Save(path);var read=StartupPreferences.Load(path);
            Assert.Equal(23,read.ClipboardLimit);Assert.Equal(500,read.LiveUpdateDelay);Assert.False(read.ContextBarEnabled);
            Assert.Equal("#4EC9B0",read.TypeColor);Assert.Equal(InspectionMode.PerFile,read.InspectionMode);
        } finally {if(File.Exists(path))File.Delete(path);}
    }
    [Theory][InlineData(-1,1,50)][InlineData(5000,50,1000)]
    public void UserEditableLimitsAreBounded(int value,int clipboard,int delay)
    {
        var settings=new StartupPreferences {ClipboardLimit=value,LiveUpdateDelay=value};
        Assert.Equal(clipboard,settings.ClipboardLimit);Assert.Equal(delay,settings.LiveUpdateDelay);
    }
    [Fact] public void FailedSaveRetainsExistingPreferencesAndRemovesTemporaryFile()
    {
        var directory=Path.Combine(Path.GetTempPath(),"CodeAssistLockedSettings-"+Guid.NewGuid());
        var path=Path.Combine(directory,"settings.xml");
        try {
            new StartupPreferences {TypeColor="#FFD700"}.Save(path);
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)) {
                Assert.ThrowsAny<IOException>(()=>new StartupPreferences {TypeColor="#4EC9B0"}.Save(path));
            }
            Assert.Equal("#FFD700",StartupPreferences.Load(path).TypeColor);
            Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
        } finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void ClipboardResizeRetainsNewestEntries()
    {
        var history=new ClipboardHistory(4);
        foreach(var text in new[]{"a","b","c","d"})history.Record(text);
        history.Capacity=2;Assert.Equal(new[]{"d","c"},history.Items());
        history.Capacity=4;history.Record("e");Assert.Equal(new[]{"e","d","c"},history.Items());
    }
}
public sealed class ProjectSyntaxInspectionTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"CodeAssistInspection-"+Guid.NewGuid());
    [Fact] public void ScanFindsProjectIssueAndIgnoresBuildOutput()
    {
        Directory.CreateDirectory(Path.Combine(root,"Source"));Directory.CreateDirectory(Path.Combine(root,"Intermediate"));
        File.WriteAllText(Path.Combine(root,"Source","Broken.cpp"),"void Tick() {\nMesh->SetRelativeLocationAndRotation(FVector(0, 0, -88), FRotator(0, -90, 0);\n}");
        File.WriteAllText(Path.Combine(root,"Source","Clean.cpp"),"void Fine(){ Tick(); }");
        File.WriteAllText(Path.Combine(root,"Intermediate","Ignored.cpp"),"void Broken(");
        var result=ProjectSyntaxInspection.Scan(root);
        var issue=Assert.Single(result);Assert.EndsWith("Broken.cpp",issue.File);Assert.Equal(2,issue.Line);
        Assert.Contains("닫는 괄호",issue.Message);
    }
    [Fact] public void CancelledScanDoesNotReturnPartialSuccess()
    {
        Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"file.cpp"),"void Tick() {}");
        using var source=new CancellationTokenSource();source.Cancel();
        Assert.Throws<OperationCanceledException>(()=>ProjectSyntaxInspection.Scan(root,source.Token));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
