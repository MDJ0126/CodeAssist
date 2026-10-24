using CodeAssist.Core;
using CodeAssist.Semantics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace CodeAssist.Extension
{
    internal static class SemanticRefactoringCommands
    {
        internal static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var commands=await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            foreach(var id in new[]{0x1c0,0x1c1}){var captured=id;commands?.AddCommand(new OleMenuCommand((s,e)=>package.JoinableTaskFactory.RunAsync(async()=>{try{await ExecuteAsync(package,captured);}catch(Exception error){await package.JoinableTaskFactory.SwitchToMainThreadAsync();System.Windows.MessageBox.Show(error.Message,"CodeAssist Refactoring");}}).FileAndForget("CodeAssist/SemanticRefactoring"),new CommandID(GoToSymbolCommand.CommandSet,captured)));}
        }
        private static JObject Position(string text,int offset)
        {var line=0;var column=0;for(var i=0;i<offset;i++){if(text[i]=='\n'){line++;column=0;}else column++;}return new JObject {["line"]=line,["character"]=column};}
        private static IEnumerable<JToken> Locations(JToken result)=>result is JArray array?array:result is JObject?new JArray(result):new JArray();
        private static string LocationFile(JToken location)
        {var value=(string)(location["uri"]??location["targetUri"]);if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||!uri.IsFile)throw new NotSupportedException("Only local text files can be edited.");return uri.LocalPath;}
        private static JToken LocationStart(JToken location)=>(location["targetSelectionRange"]??location["range"])?["start"]??throw new ArgumentException("Invalid compiler location");
        private static string Read(IDictionary<string,string> documents,string file)
        {if(!documents.TryGetValue(file,out var text)){if(new FileInfo(file).Length>2*1024*1024)throw new NotSupportedException("Refactoring input is limited to 2 MiB per file.");text=File.ReadAllText(file);documents[file]=text;}return text;}
        private static async System.Threading.Tasks.Task ExecuteAsync(AsyncPackage package,int command)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();var dte=await package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;var model=await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;var manager=await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if(dte==null||model==null||manager==null||manager.GetActiveView(1,null,out var adapter)<0)return;var view=model.GetService<IVsEditorAdaptersFactoryService>().GetWpfTextView(adapter);if(view==null||view.IsClosed||!view.TextBuffer.ContentType.IsOfType("C/C++"))return;
            var file=dte.ActiveDocument?.FullName;if(string.IsNullOrEmpty(file))return;var snapshot=view.TextSnapshot;var active=snapshot.GetText();var caret=view.Caret.Position.BufferPosition;var line=caret.GetContainingLine();
            var root=Path.GetDirectoryName(file);while(root!=null&&!File.Exists(Path.Combine(root,"compile_commands.json"))&&!File.Exists(Path.Combine(root,"compile_flags.txt")))root=Path.GetDirectoryName(root);
            if(root==null&&!string.IsNullOrEmpty(StartupUi.Preferences.CompilationDatabaseDirectory))root=Path.GetDirectoryName(file);if(root==null)throw new NotSupportedException("Configure compile_commands.json or compile_flags.txt before semantic refactoring.");
            var before=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(EnvDTE.Document document in dte.Documents)try{if(new[]{".c",".cc",".cpp",".cxx",".h",".hh",".hpp",".hxx",".inl",".ipp"}.Contains(Path.GetExtension(document.FullName).ToLowerInvariant())&&document.Object("TextDocument") is EnvDTE.TextDocument text)before[document.FullName]=text.StartPoint.CreateEditPoint().GetText(text.EndPoint);}catch(System.Runtime.InteropServices.COMException){}
            before[file]=active;var token=package.DisposalToken;
            var deadline=DateTime.UtcNow.AddSeconds(30);
            Task<JToken> Query(string method,string text,int offset,JObject args=null,IDictionary<string,string> documents=null)
            {if(DateTime.UtcNow>deadline)throw new TimeoutException("Semantic refactoring exceeded 30 seconds; no files were changed.");var p=Position(text,offset);return CompilerSemanticService.Instance.QueryAsync(method,root,dte.FullName,file,text,(int)p["line"]+1,(int)p["character"]+1,token,args,documents??before);}
            if(command==0x1c0)
            {
                var input=EditorFeatureCommands.Prompt(dte,"New parameter order (one-based, e.g. 2,1; reorder only)","2,1");if(input==null)return;
                var order=input.Split(',').Select(v=>int.Parse(v.Trim(),System.Globalization.CultureInfo.InvariantCulture)-1).ToArray();
                var declarations=Locations(await Query("textDocument/declaration",active,caret)).Concat(Locations(await Query("textDocument/definition",active,caret))).ToArray();
                if(declarations.Length==0)throw new NotSupportedException("The compiler did not resolve a callable declaration.");
                var references=Locations(await Query("textDocument/references",active,caret,new JObject {["context"]=new JObject {["includeDeclaration"]=true}})).Concat(declarations).ToArray();
                var declarationKeys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var declaration in declarations){var path=LocationFile(declaration);var text=Read(before,path);var p=LocationStart(declaration);declarationKeys.Add(path+"|"+WorkspaceEdits.Offset(text,(int)p["line"],(int)p["character"]));}
                var changes=new JObject();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var reference in references)
                {
                    var path=LocationFile(reference);var text=Read(before,path);var p=LocationStart(reference);var offset=WorkspaceEdits.Offset(text,(int)p["line"],(int)p["character"]);var key=path+"|"+offset;if(!seen.Add(key))continue;
                    var edit=CppDocumentTransforms.ReorderParameters(text,offset,order,declarationKeys.Contains(key));var uri=new Uri(Path.GetFullPath(path)).AbsoluteUri;
                    if(!(changes[uri] is JArray edits)){edits=new JArray();changes[uri]=edits;}edits.Add(new JObject {["range"]=new JObject {["start"]=Position(text,edit.Start),["end"]=Position(text,edit.Start+edit.Length)},["newText"]=edit.Text});
                }
                if(seen.Count==0)throw new InvalidOperationException("No resolved callable references");
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();await WorkspaceEditPreview.ApplyAsync(package,dte,new JObject {["changes"]=changes},before,"Change Signature: reorder resolved parameters and call arguments");return;
            }
            var working=new Dictionary<string,string>(before,StringComparer.OrdinalIgnoreCase);var moved=0;
            try
            {
                for(var pass=0;pass<200;pass++)
                {
                    token.ThrowIfCancellationRequested();var current=working[file];JToken selectedEdit=null;
                    foreach(var method in CppSemanticParser.Parse(current).Where(t=>t.Kind==SemanticTokenKind.Function).GroupBy(t=>t.Start).Select(g=>g.First()).Take(2000))
                    {
                        var pos=Position(current,method.Start);var range=new JObject {["start"]=pos,["end"]=Position(current,method.Start+method.Length)};
                        var candidates=await Query("textDocument/codeAction",current,method.Start,new JObject {["range"]=range,["context"]=new JObject {["diagnostics"]=new JArray(),["only"]=new JArray("refactor")}},working) as JArray;
                        var action=candidates?.OfType<JObject>().FirstOrDefault(a=>(string)a["title"]=="Move function body to out-of-line"&&a["disabled"]==null);if(action==null)continue;
                        if(action["edit"]!=null)selectedEdit=action["edit"];
                        else if(ClangdClient.SupportsPreviewCommand(action["command"]))selectedEdit=await Query("codeassist/previewCommand",current,method.Start,(JObject)action["command"],working);
                        if(selectedEdit!=null)break;
                    }
                    if(selectedEdit==null)break;
                    foreach(var entry in WorkspaceEdits.Files(selectedEdit))
                    {var original=Read(working,entry.Key);Read(before,entry.Key);working[entry.Key]=WorkspaceEdits.Apply(original,entry.Value);}
                    moved++;
                }
            }
            finally
            {
                // All intermediate edits live only in clangd's document overlay; restore it before asking to apply.
                await CompilerSemanticService.Instance.QueryAsync("codeassist/sync",root,dte.FullName,file,active,1,1,System.Threading.CancellationToken.None,null,before);
            }
            if(moved==0)throw new NotSupportedException("No supported inline method bodies were found. Counterpart source files must already exist.");
            var combined=new JObject();foreach(var pair in working.Where(p=>before.ContainsKey(p.Key)&&before[p.Key]!=p.Value))combined[new Uri(Path.GetFullPath(pair.Key)).AbsoluteUri]=new JArray(new JObject {["range"]=new JObject {["start"]=Position(before[pair.Key],0),["end"]=Position(before[pair.Key],before[pair.Key].Length)},["newText"]=pair.Value});
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();await WorkspaceEditPreview.ApplyAsync(package,dte,new JObject {["changes"]=combined},before,"Move "+moved+" method implementations to source");
        }
    }
}
