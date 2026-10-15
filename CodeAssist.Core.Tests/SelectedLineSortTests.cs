using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public class SelectedLineSortTests
{
    [Fact] public void PartialLastLineIncludesWholeLineAndAddsEofNewline() {
        var text="prefix\nzebra\nalpha"; var edit=SelectedLineSort.Create(text,8,7,false,false);
        Assert.Equal("prefix\nalpha\nzebra\n",text.Remove(edit.Start,edit.Length).Insert(edit.Start,edit.Text));
    }
    [Fact] public void SelectionEndingAtNextLineStartDoesNotIncludeThatLine() {
        var text="z\r\na\r\nkeep"; var edit=SelectedLineSort.Create(text,0,6,false,false);
        Assert.Equal("a\r\nz\r\nkeep",text.Remove(edit.Start,edit.Length).Insert(edit.Start,edit.Text));
    }
    [Fact] public void StableCaseInsensitiveOrderingPreservesDuplicateLines() {
        var edit=SelectedLineSort.Create("b\nA\na\nb\n",0,8,true,true); Assert.Equal("b\nb\nA\na\n",edit.Text);
    }
    [Fact] public void EmptySelectionDoesNotEditDocument() {
        var edit=SelectedLineSort.Create("text",2,0,false,false); Assert.Equal(0,edit.Length);Assert.Empty(edit.Text);
    }
}
