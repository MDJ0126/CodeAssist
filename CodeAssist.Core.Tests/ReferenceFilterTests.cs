using CodeAssist.Core;
using Xunit;
public class ReferenceFilterTests
{
    [Theory]
    [InlineData("probe.GetHealth()", "probe GetHealth", true)]
    [InlineData("return 10", "probe GetHealth", false)]
    [InlineData("return 10", "return,probe", true)]
    [InlineData("probe.GetHealth()", "return,probe", true)]
    [InlineData("other.GetHealth()", "return,probe", false)]
    [InlineData("probe.GetHealth()", "PROBE gethealth", true)]
    [InlineData("probe.GetHealth()", "  probe   GetHealth  ", true)]
    [InlineData("anything", "", true)]
    [InlineData("anything", ",", false)]
    public void TermsAndAlternatives(string text,string query,bool expected)
        => Assert.Equal(expected, ReferenceFilter.Matches(text,query));
}
