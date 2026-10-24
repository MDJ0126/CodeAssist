using CodeAssist.Core;
using Xunit;
public class SymbolColorPaletteTests
{
    [Theory]
    [InlineData(SymbolKind.Type,"#112233")]
    [InlineData(SymbolKind.Function,"#223344")]
    [InlineData(SymbolKind.Variable,"#334455")]
    [InlineData(SymbolKind.Macro,"#445566")]
    [InlineData(SymbolKind.Specifier,"#445566")]
    public void CustomInkSurvivesBothThemes(SymbolKind kind,string expected)
    {
        var p=new StartupPreferences {TypeColor="#112233",FunctionColor="#223344",VariableColor="#334455",MacroColor="#445566"};
        Assert.Equal(expected,SymbolColorPalette.Resolve(p,kind,false));
        Assert.Equal(expected,SymbolColorPalette.Resolve(p,kind,true));
    }
    [Theory][InlineData(SymbolKind.Unknown)][InlineData(SymbolKind.Header)]
    public void NonSymbolsInheritIdeInk(SymbolKind kind)=>Assert.Null(SymbolColorPalette.Resolve(new StartupPreferences(),kind,false));
    [Theory]
    [InlineData(SymbolKind.Type,"#2B6C6E")]
    [InlineData(SymbolKind.Function,"#A31515")]
    [InlineData(SymbolKind.Variable,"#000000")]
    [InlineData(SymbolKind.Macro,"#6F008A")]
    public void LightDefaultsMatchEditor(SymbolKind kind,string expected)=>Assert.Equal(expected,SymbolColorPalette.Resolve(new StartupPreferences(),kind,true));
}
