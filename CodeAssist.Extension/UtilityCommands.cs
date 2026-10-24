using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
namespace CodeAssist.Extension
{
    [Guid("7d0a1bc0-3932-4912-9e82-84bb1e541810")]
    public sealed class UtilityToolWindow:ToolWindowPane
    {
        internal readonly TabControl Tabs=new TabControl();
        public UtilityToolWindow(){Caption="CodeAssist Tools";Content=Tabs;ProductTheme.Apply(Tabs);}
    }
    internal static class UtilityCommands
    {
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var service=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;if(service==null)return;
            for(var id=0x190;id<=0x197;id++){var command=id;service.AddCommand(new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(async()=>{try{await ExecuteAsync(package,command);}catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();MessageBox.Show(error.Message,"CodeAssist",MessageBoxButton.OK,MessageBoxImage.Warning);}}).FileAndForget("CodeAssist/Utility"),new CommandID(GoToSymbolCommand.CommandSet,command)));}
        }
        private static async System.Threading.Tasks.Task ExecuteAsync(AsyncPackage package,int id)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;if(dte==null)return;
            if(id==0x196||id==0x197){if(!await CompilerActions.TryAsync(package,dte,true,id==0x197))MessageBox.Show("No compiler quick fixes are available at this location.","CodeAssist");return;}
            if(id==0x190||id==0x191)
            {
                var picker=new Microsoft.Win32.OpenFileDialog {Title=id==0x190?"CodeAssist: PDB Explorer":"CodeAssist: Address Resolver",Filter=id==0x190?"PDB files|*.pdb":"Binary images|*.exe;*.dll|All files|*.*"};if(picker.ShowDialog()!=true)return;
                var tool=id==0x190?"llvm-pdbutil.exe":"llvm-symbolizer.exe";var executable=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dte.FullName),"../../VC/Tools/Llvm/x64/bin",tool));
                if(!File.Exists(executable))throw new FileNotFoundException("LLVM tool not found. Install the Visual Studio C++ LLVM tools.",executable);
                var arguments=id==0x190?"dump --publics --globals "+Quote(picker.FileName):"--obj="+Quote(picker.FileName)+" --relative-address --functions --inlines ";
                if(id==0x191){var address=EditorFeatureCommands.Prompt(dte,"Relative virtual address (RVA), hexadecimal","0x1000");if(address==null)return;if(!System.Text.RegularExpressions.Regex.IsMatch(address,@"^(?:0[xX])?[0-9a-fA-F]{1,16}$"))throw new ArgumentException("Invalid RVA");arguments+=(address.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?address:"0x"+address);}
                var output=await RunToolAsync(executable,arguments,package.DisposalToken);await ShowAsync(package,Path.GetFileName(picker.FileName),new TextBox {Text=output,IsReadOnly=true,AcceptsReturn=true,FontFamily=new FontFamily("Consolas"),HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});return;
            }
            if(id==0x192)
            {
                if(dte.Debugger.CurrentMode!=EnvDTE.dbgDebugMode.dbgBreakMode)throw new InvalidOperationException("Pause at a breakpoint before reading memory.");
                var address=EditorFeatureCommands.Prompt(dte,"Memory address (hexadecimal), reads 64 bytes","0x0");if(address==null)return;if(!System.Text.RegularExpressions.Regex.IsMatch(address,@"^0[xX][0-9a-fA-F]{1,16}$"))throw new ArgumentException("Invalid address");
                var value=dte.Debugger.GetExpression("*(unsigned char (*)[64])"+address,false,1000);if(value==null||!value.IsValidValue)throw new InvalidOperationException(value?.Value??"Memory is unavailable");
                var text=new StringBuilder(address+Environment.NewLine+value.Value+Environment.NewLine);foreach(EnvDTE.Expression child in value.DataMembers)text.AppendLine(child.Name+" : "+child.Value);
                await ShowAsync(package,"Memory "+address,new TextBox {Text=text.ToString(),IsReadOnly=true,FontFamily=new FontFamily("Consolas"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto});return;
            }
            if(id==0x193)
            {
                var picker=new Microsoft.Win32.OpenFileDialog {Title="CodeAssist: Preview File",Filter="C/C++ files|*.h;*.hpp;*.cpp;*.c;*.cc;*.inl|All files|*.*"};if(picker.ShowDialog()!=true)return;
                if(new FileInfo(picker.FileName).Length>2*1024*1024)throw new InvalidOperationException("Preview is limited to 2 MiB.");var text=await System.Threading.Tasks.Task.Run(()=>File.ReadAllText(picker.FileName));await ShowAsync(package,Path.GetFileName(picker.FileName),new FlowDocumentScrollViewer {Document=ColoredDocument(text),IsToolBarVisible=true});return;
            }
            if(id==0x194)
            {
                var document=dte.ActiveDocument?.Object("TextDocument") as EnvDTE.TextDocument;if(document==null)return;var text=document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
                if(text.Length>2*1024*1024)throw new InvalidOperationException("Color printing is limited to 2 MiB.");var dialog=new PrintDialog();if(dialog.ShowDialog()==true){var documentToPrint=ColoredDocument(text);documentToPrint.PageWidth=dialog.PrintableAreaWidth;documentToPrint.ColumnWidth=dialog.PrintableAreaWidth;dialog.PrintDocument(((IDocumentPaginatorSource)documentToPrint).DocumentPaginator,"CodeAssist: "+dte.ActiveDocument.Name);}return;
            }
            if(id==0x195)
            {
                var document=dte.ActiveDocument;var selection=document?.Selection as EnvDTE.TextSelection;var textDocument=document?.Object("TextDocument") as EnvDTE.TextDocument;if(selection==null||textDocument==null)return;
                var root=Path.GetDirectoryName(document.FullName);while(root!=null&&!File.Exists(Path.Combine(root,"compile_commands.json"))&&!File.Exists(Path.Combine(root,"compile_flags.txt")))root=Path.GetDirectoryName(root);
                if(root==null&&!string.IsNullOrEmpty(StartupUi.Preferences.CompilationDatabaseDirectory))root=Path.GetDirectoryName(document.FullName);
                var text=textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
                var help=await CompilerSemanticService.Instance.QueryAsync("textDocument/signatureHelp",root,dte.FullName,document.FullName,text,selection.ActivePoint.Line,selection.ActivePoint.LineCharOffset,package.DisposalToken);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(!(help?["signatures"] is JArray signatures)||signatures.Count==0)throw new InvalidOperationException("No compiler signature is available at the caret. Configure a compilation database and place the caret in an argument list.");
                var lines=new StringBuilder("Active parameter: "+((int?)help["activeParameter"]+1)+Environment.NewLine);
                foreach(var signature in signatures)lines.AppendLine((string)signature["label"]).AppendLine((string)(signature["documentation"] is JObject documentation?documentation["value"]:signature["documentation"]));
                await ShowAsync(package,"Parameter Info",new TextBox {Text=lines.ToString(),IsReadOnly=true,AcceptsReturn=true,FontFamily=new FontFamily("Consolas"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
            }
        }
        internal static FlowDocument ColoredDocument(string text)
        {
            var document=new FlowDocument {FontFamily=new FontFamily("Consolas"),FontSize=12,PagePadding=new Thickness(24),Background=Brushes.White,Foreground=Brushes.Black};var paragraph=new Paragraph {Margin=new Thickness(0)};document.Blocks.Add(paragraph);var cursor=0;
            foreach(var token in CppSemanticParser.Parse(text).OrderBy(t=>t.Start))
            {if(token.Start<cursor)continue;if(token.Start>cursor)paragraph.Inlines.Add(new Run(text.Substring(cursor,token.Start-cursor)));var color=token.Kind==SemanticTokenKind.Type?StartupUi.Preferences.TypeColor:token.Kind==SemanticTokenKind.Function?StartupUi.Preferences.FunctionColor:StartupUi.Preferences.VariableColor;paragraph.Inlines.Add(new Run(text.Substring(token.Start,token.Length)){Foreground=(Brush)new BrushConverter().ConvertFromString(color)});cursor=token.Start+token.Length;}
            if(cursor<text.Length)paragraph.Inlines.Add(new Run(text.Substring(cursor)));return document;
        }
        internal static async System.Threading.Tasks.Task ShowAsync(AsyncPackage package,string title,object content)
        {await package.JoinableTaskFactory.SwitchToMainThreadAsync();var pane=await package.ShowToolWindowAsync(typeof(UtilityToolWindow),0,true,package.DisposalToken) as UtilityToolWindow;if(pane==null)return;var tab=new TabItem {Header=title,Content=content};pane.Tabs.Items.Add(tab);pane.Tabs.SelectedItem=tab;while(pane.Tabs.Items.Count>12)pane.Tabs.Items.RemoveAt(0);}
        private static string Quote(string value){if(value.IndexOf('"')>=0||value.IndexOf('\0')>=0)throw new ArgumentException("Invalid tool argument");return "\""+value+"\"";}
        private static async Task<string> RunToolAsync(string tool,string arguments,System.Threading.CancellationToken cancellation)
        {
            using(var process=new Process {StartInfo=new ProcessStartInfo(tool,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}})
            {
                process.Start();var output=ReadBoundedAsync(process.StandardOutput);var error=ReadBoundedAsync(process.StandardError);
                try{var all=System.Threading.Tasks.Task.WhenAll(output,error);if(await System.Threading.Tasks.Task.WhenAny(all,System.Threading.Tasks.Task.Delay(15000,cancellation))!=all){cancellation.ThrowIfCancellationRequested();throw new TimeoutException("LLVM analysis timed out");}var values=await all;await System.Threading.Tasks.Task.Run(()=>process.WaitForExit());if(process.ExitCode!=0)throw new InvalidOperationException(values[1]);return values[0]+values[1];}
                finally{if(!process.HasExited){process.Kill();process.WaitForExit();}}
            }
        }
        private static async Task<string> ReadBoundedAsync(StreamReader input)
        {var text=new StringBuilder();var buffer=new char[4096];int count;while((count=await input.ReadAsync(buffer,0,buffer.Length))>0){if(text.Length+count>2*1024*1024)throw new InvalidOperationException("LLVM output exceeded 2 MiB; narrow the input.");text.Append(buffer,0,count);}return text.ToString();}
    }
}
