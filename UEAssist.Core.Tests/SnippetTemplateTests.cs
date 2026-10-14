using UEAssist.Core;
namespace UEAssist.Core.Tests;
public class SnippetTemplateTests
{
    [Fact]
    public void ReservedDateAndTimeUseDocumentedFormatsAndOneTimestamp()
    {
        var values = SnippetReservedValues.Create("Actor.cpp", "value", new DateTime(2026, 1, 2, 3, 4, 5),
            System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        var expansion = new SnippetTemplate("", "", "$DATE$ $DAY$ $DAY_02$ $MONTH$ $MONTH_02$ $YEAR_02$ $HOUR$ $HOUR_02$ $MINUTE$ $SECOND$ $DAYLONGNAME_EN$ $FILE_BASE_UPPER$ $selected$").Expand(values);
        Assert.Equal("2026/01/02 2 02 1 01 26 3 03 04 05 Friday ACTOR value", expansion.Text);
        Assert.Empty(expansion.Missing);
        Assert.False(values.ContainsKey("FILE_BASE_CAMEL"));
    }
    [Fact]
    public void ParsesFormFeedRecordsWithoutLosingMultilineBodies()
    {
        var templates = SnippetTemplate.Parse("\f\r\na:Guard:guard:\r\n#ifndef $Name$\r\n$selected$$end$\r\n#endif\r\n\fa::inc:\n#include \"$Name$\"\f");
        Assert.Equal(2, templates.Count); Assert.Equal("Guard", templates[0].Title);
        Assert.Contains("\r\n#endif\r\n", templates[0].Body); Assert.Equal("inc", templates[1].ToString());
    }
    [Fact]
    public void RepeatedVariablesAreLiteralAndCaretTracksExpandedSelection()
    {
        var template = new SnippetTemplate("Wrap", "", "$Name$($selected$)$end$; // $Name$ $$");
        var expanded = template.Expand(new Dictionary<string, string> { ["Name"] = "Call", ["selected"] = "$1\\x" });
        Assert.Equal("Call($1\\x); // Call $", expanded.Text);
        Assert.Equal("Call($1\\x)".Length, expanded.Caret); Assert.Empty(expanded.Missing);
    }
    [Fact]
    public void MissingVariablesStayVisibleAndAreReported()
    {
        var expanded = new SnippetTemplate("", "", "$Unknown$ $Unknown$").Expand(new Dictionary<string, string>());
        Assert.Equal("$Unknown$ $Unknown$", expanded.Text); Assert.Equal(new[] { "Unknown" }, expanded.Missing);
    }
    [Fact]
    public void EscapedDollarDoesNotIntroduceAUserField()
    {
        Assert.Empty(new SnippetTemplate("", "", "$$Name").Variables());
        Assert.Equal(new[] { "selected", "Name" }, new SnippetTemplate("", "", "$selected$$end$$Name$").Variables());
    }
}
