using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class SnippetInputTests
{
    [Fact] public void DefaultAppearingLaterIsSharedByAllOccurrences() {
        var template = new SnippetTemplate("", "", "$Index$ $Index=i$ $Index_Upper$");
        Assert.Single(template.Inputs()); Assert.Equal("i i I",template.Expand(new Dictionary<string,string>()).Text);
        Assert.Equal("x x X",template.Expand(new Dictionary<string,string>{{"Index","x"}}).Text);
    }
    [Fact] public void ModifiersAreCaseSensitiveAndPascalOnlyChangesFirstCharacter() {
        var template = new SnippetTemplate("", "", "$Name_Upper$ $Name_Lower$ $Name_Pascal$ $Name_Camel$ $Name_upper$");
        var result = template.Expand(new Dictionary<string,string>{{"Name","hELLO_world"},{"Name_upper","literal"}});
        Assert.Equal("HELLO_WORLD hello_world HELLO_world hELLO_world literal",result.Text);
    }
    [Fact] public void EnvironmentIsResolvedBeforeDefaultsAndUnknownVariablesBecomeEmpty() {
        var environment = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"TEST_AUTHOR","Ada"}};
        var template = new SnippetTemplate("", "", "$Author=%test_author%$ %MISSING_TEST% %TEST_AUTHOR%");
        var expanded = template.WithEnvironment(key=>environment.TryGetValue(key,out var value)?value:null);
        Assert.Equal("Ada  Ada",expanded.Expand(new Dictionary<string,string>()).Text);
        Assert.Equal("Ada",Assert.Single(expanded.Inputs()).DefaultValue);
    }
    [Fact] public void ReservedFieldsDoNotCountAsUserInputsAndValuesRemainLiteral() {
        var values = new Dictionary<string,string>{{"FILE_BASE","actor"}};
        var template = new SnippetTemplate("", "", "$FILE_BASE_Upper$ $Selected Type=auto$ $end$");
        Assert.Equal("Selected Type",Assert.Single(template.Inputs(values)).Name);
        values["Selected Type"]="$literal$";
        var result=template.Expand(values);Assert.Equal("ACTOR $literal$ ",result.Text);Assert.Equal(result.Text.Length,result.Caret);
    }
}
