using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=3 && args.Length!=4) throw new ArgumentException("ManagedDecompiler <assembly> <full type name> <output>");
var settings=new DecompilerSettings { ThrowOnAssemblyResolveErrors=false };
var assemblyPath=Path.GetFullPath(args[0]);
var resolver=new UniversalAssemblyResolver(assemblyPath,false,".NETFramework,Version=v4.8");
foreach(var folder in new[]{Path.GetDirectoryName(assemblyPath)!,"C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/PublicAssemblies","C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/PrivateAssemblies","C:/Windows/Microsoft.NET/Framework64/v4.0.30319","C:/Windows/Microsoft.NET/Framework64/v4.0.30319/WPF","C:/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/Editor"})
    if(Directory.Exists(folder)) resolver.AddSearchDirectory(folder);
var decompiler=new CSharpDecompiler(assemblyPath,resolver,settings);
if(args[1]=="--batch")
{
    using var manifest=JsonDocument.Parse(File.ReadAllText(args[2]));
    var output=Path.GetFullPath(args[3]); Directory.CreateDirectory(output);
    var records=new List<object>();
    foreach(var method in manifest.RootElement.EnumerateArray())
    {
        var token=method.GetProperty("token").GetString()!;
        var sourceText=decompiler.DecompileAsString(MetadataTokens.EntityHandle(int.Parse(token.Replace("0x",""),NumberStyles.HexNumber)));
        var path=Path.Combine(output,token+".cs");File.WriteAllText(path,sourceText);
        records.Add(new {Token=token,Type=method.GetProperty("type").GetString(),Name=method.GetProperty("name").GetString(),SourcePath=path,SourceSHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Warnings=sourceText.Split("Unknown result type").Length-1});
    }
    Console.WriteLine(JsonSerializer.Serialize(new {AssemblySHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath))),Decompiler="ICSharpCode.Decompiler 9.1.0.7988",Methods=records,Executed=false}));
    return;
}
var source=args.Length==4 ? decompiler.DecompileAsString(MetadataTokens.EntityHandle(int.Parse(args[3].Replace("0x",""),NumberStyles.HexNumber))) : decompiler.DecompileTypeAsString(new FullTypeName(args[1]));
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);File.WriteAllText(args[2],source);
Console.WriteLine(JsonSerializer.Serialize(new {Assembly=Path.GetFileName(args[0]),Type=args[1],AssemblySHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),Decompiler="ICSharpCode.Decompiler 9.1.0.7988",SourceSHA256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2]))),Scope="Static decompiler reconstruction; assembly not loaded or executed; not original source"}));
