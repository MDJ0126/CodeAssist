using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=3)throw new ArgumentException("<installed Core DLL> <input cases JSON> <output JSON>");
var file=Path.GetFullPath(args[0]);
var assembly=Assembly.LoadFrom(file);
if(!string.Equals(Path.GetFullPath(assembly.Location),file,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Wrong assembly loaded");
object Call(string type,string method,Type[] signature,params object[] values)=>assembly.GetType("CodeAssist.Core."+type,true)!.GetMethod(method,signature)!.Invoke(null,values)!;
int Number(object value,string property)=>(int)value.GetType().GetProperty(property)!.GetValue(value)!;
string Text(object value,string property)=>(string)value.GetType().GetProperty(property)!.GetValue(value)!;
var rows=new List<object>();
using var input=JsonDocument.Parse(File.ReadAllText(args[1]));
foreach(var c in input.RootElement.EnumerateArray())
{
 var id=c.GetProperty("id").GetString()!;var text=c.GetProperty("text").GetString()!;var start=c.GetProperty("start").GetInt32();var length=c.GetProperty("length").GetInt32();
 string? error=null;object? after=null;
 try
 {
  var result=text;var selectionStart=start;var selectionLength=length;var caret=start+length;
  if(id=="scope-next")
  {caret=(int)Call("CppScopeNavigation","Move",new[]{typeof(string),typeof(int),typeof(bool)},text,caret,true);selectionStart=caret;selectionLength=0;}
  else if(id=="smart-select")
  {var selected=Call("CppSelection","Expand",new[]{typeof(string),typeof(int),typeof(int),typeof(bool)},text,start,length,false);selectionStart=Number(selected,"Start");selectionLength=Number(selected,"Length");caret=selectionStart+selectionLength;}
  else if(id=="surround-word")
  {var change=Call("CppEditing","Surround",new[]{typeof(string),typeof(int),typeof(int),typeof(char),typeof(bool)},text,start,length,'(',true);result=text.Remove(Number(change,"Start"),Number(change,"Length")).Insert(Number(change,"Start"),Text(change,"Text"));selectionStart=Number(change,"SelectionStart");selectionLength=Number(change,"SelectionLength");caret=selectionStart+selectionLength;}
  else if(id=="pair-insert")
  {result=text.Remove(start,length).Insert(start,"(");caret=start+1;var placement=assembly.GetType("CodeAssist.Core.ClosingBracePlacement",true)!;var insertion=Call("CppPairing","AfterOpening",new[]{typeof(string),typeof(int),typeof(char),placement,typeof(string)},result,caret,'(',Enum.Parse(placement,"SameLine"),"    ");result=result.Insert(caret,Text(insertion,"Text"));caret+=Number(insertion,"CaretOffset");selectionStart=caret;selectionLength=0;}
  else throw new ArgumentException("Unsupported component case: "+id);
  after=new{text=result,caret,selectionStart,selectionLength,selectedText=result.Substring(selectionStart,selectionLength)};
 }
 catch(Exception e){error=e.GetBaseException().Message;}
 rows.Add(new{id,after,error});
}
var report=new{scope="Installed Core component execution only. No VS pipeline, Undo, physical input or new VA execution.",assemblyPath=assembly.Location,assemblySHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))),cases=rows};
File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Installed Core cases executed: "+rows.Count);
