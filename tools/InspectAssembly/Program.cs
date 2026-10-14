using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Resources;
using var stream = File.OpenRead(args[0]); using var pe = new PEReader(stream); var metadata = pe.GetMetadataReader();
var menuFound = false;
var facts = new {
    Resources = metadata.ManifestResources.Select(handle => {
        var resource = metadata.GetManifestResource(handle); var name = metadata.GetString(resource.Name);
        var data = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress).GetContent().ToArray();
        var at = (int)resource.Offset; var size = BitConverter.ToInt32(data, at);
        using var reader = new ResourceReader(new MemoryStream(data, at + 4, size, false));
        var keys = new List<object>(); var iterator = reader.GetEnumerator();
        while (iterator.MoveNext()) { var key = (string)iterator.Key; reader.GetResourceData(key, out var type, out var bytes);
            if (key == "CTMENU" && type == "ResourceTypeCode.ByteArray" && bytes.Length > 4) menuFound = true;
            keys.Add(new { Key=key, Type=type, Bytes=bytes.Length }); }
        return new { Name=name, Keys=keys };
    }).ToArray(),
    Dependencies = metadata.AssemblyReferences.Select(handle => { var value = metadata.GetAssemblyReference(handle); return new { Name=metadata.GetString(value.Name), Version=value.Version.ToString() }; }).ToArray()
};
if (!menuFound) throw new InvalidDataException("Managed CTMENU byte-array resource is missing");
var output = JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true });
if (args.Length > 1) File.WriteAllText(args[1], output);
Console.WriteLine(output);
