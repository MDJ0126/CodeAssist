using CodeAssist.Core;
using System.Globalization;
namespace CodeAssist.Core.Tests;
public class SnippetWorkspaceTests
{
    [Fact] public void WorkspaceNamesAndGuidVariantsUseOneGuid() {
        var values=SnippetReservedValues.Create("Actor.cpp","",DateTime.UnixEpoch,CultureInfo.InvariantCulture);
        var guid=Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        SnippetReservedValues.AddWorkspace(values,"Game.vcxproj","Workspace.sln",guid,CultureInfo.InvariantCulture);
        var template=new SnippetTemplate("","","$PROJECT_NAME$ $PROJECT_EXT$ $SOLUTION_NAME_UPPER$ $GUID_STRING$ $GUID_STRING$ $GUID_SYMBOL_UPPER$");
        var result=template.Expand(values);
        Assert.Equal("Game vcxproj WORKSPACE 01234567-89ab-cdef-0123-456789abcdef 01234567-89ab-cdef-0123-456789abcdef 01234567_89AB_CDEF_0123_456789ABCDEF",result.Text);
        Assert.Empty(result.Missing);Assert.Empty(template.Inputs(values));
    }
    [Fact] public void MissingWorkspaceDoesNotBecomeInputAndUnverifiedGuidFormatsStayMissing() {
        var values=new Dictionary<string,string>();SnippetReservedValues.AddWorkspace(values,null,null,Guid.Empty,CultureInfo.InvariantCulture);
        Assert.Equal("",values["PROJECT_FILE"]);Assert.Equal("",values["SOLUTION_PATH"]);
        Assert.Equal(new[]{"GUID_STRUCT"},new SnippetTemplate("","","$GUID_STRUCT$").Expand(values).Missing);
    }
}
