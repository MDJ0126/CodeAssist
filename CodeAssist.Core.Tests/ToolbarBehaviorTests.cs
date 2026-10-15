using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class ToolbarBehaviorTests
{
    private sealed class Value { public bool Enabled; public string Filter=""; }
    [Fact] public void SkipRestoresOriginallyDisabledBreakpointsAndPreservesManualEdits()
    {
        var a=new Value{Enabled=true};var b=new Value{Enabled=false};var state=new ReversibleOverrides<Value,bool>();
        state.Apply(new[]{a,b},v=>v.Enabled,(v,x)=>v.Enabled=x,_=>false);Assert.False(a.Enabled);Assert.False(b.Enabled);
        a.Enabled=true;state.Restore(v=>v.Enabled,(v,x)=>v.Enabled=x);Assert.True(a.Enabled);Assert.False(b.Enabled);Assert.False(state.Active);
    }
    [Fact] public void RepeatedThreadBindingKeepsOriginalFilter()
    {
        var v=new Value{Filter="ProcessId = 1"};var state=new ReversibleOverrides<Value,string>();
        state.Apply(new[]{v},x=>x.Filter,(x,s)=>x.Filter=s,s=>"("+s+") & (ThreadId = 2)");
        state.Apply(new[]{v},x=>x.Filter,(x,s)=>x.Filter=s,s=>s+"bad");state.Restore(x=>x.Filter,(x,s)=>x.Filter=s);Assert.Equal("ProcessId = 1",v.Filter);
    }
    [Fact] public void FailedSetterRollsBackPreviouslyChangedItems()
    {
        var a=new Value{Enabled=true};var b=new Value{Enabled=true};var state=new ReversibleOverrides<Value,bool>();
        Assert.Throws<InvalidOperationException>(()=>state.Apply(new[]{a,b},v=>v.Enabled,(v,x)=>{if(ReferenceEquals(v,b)&&!x)throw new InvalidOperationException();v.Enabled=x;},_=>false));Assert.True(a.Enabled);Assert.True(b.Enabled);Assert.False(state.Active);
    }
    [Fact] public void ContextNavigationSkipsCommentsStringsAndShadowedLocalsAndWraps()
    {
        var text="void F(){ int Value=0; Use(Value); { int Value=1; Use(Value); } Use(Value); } // Value\nconst char* s=\"Value\";";
        var first=text.IndexOf("Value");var next=ContextReferenceNavigation.Move(text,first,true)!;Assert.Equal(text.IndexOf("Use(Value)")+4,next.Start);
        var last=text.LastIndexOf("Use(Value)")+4;Assert.Equal(last,ContextReferenceNavigation.Move(text,next.Start,true)!.Start);Assert.Equal(first,ContextReferenceNavigation.Move(text,last,true)!.Start);
        Assert.Null(ContextReferenceNavigation.Move(text,text.LastIndexOf("Value"),true));
    }
    [Fact] public void NativeCompletionColorPreferenceSurvivesSerializationCopy()
    { var p=new StartupPreferences{CompletionUseVisualStudioColors=true};Assert.True(p.Copy().CompletionUseVisualStudioColors); }
}