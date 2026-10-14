using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 2 || args.Length > 3) throw new ArgumentException("Usage: VAStaticAnalysis <extension folder> <output json> [--all]");
var complete = args.Length == 3 && args[2] == "--all";
var focus = new Regex("Completion|Classifier|Reference|Parser|Parse|Database|Cache|TextView|Buffer|CommandFilter|Unreal|Inspection", RegexOptions.IgnoreCase);
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
    .ToDictionary(o => unchecked((ushort)o.Value));
var results = new List<object>();
foreach (var path in Directory.GetFiles(args[0], "*", complete ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Where(p =>
    (complete ? new[] { ".dll", ".exe", ".slp" } : new[] { ".dll", ".exe" }).Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)))
{
    using var stream = File.OpenRead(path);
    var hash = Convert.ToHexString(SHA256.HashData(stream));
    stream.Position = 0;
    using var pe = new PEReader(stream);
    var types = new List<object>();
    var refs = new List<string>();
    var pinvokes = new List<object>();
    if (pe.HasMetadata)
    {
        var md = pe.GetMetadataReader();
        string Label(EntityHandle h) => h.Kind switch
        {
            HandleKind.TypeReference => md.GetString(md.GetTypeReference((TypeReferenceHandle)h).Namespace) + "." + md.GetString(md.GetTypeReference((TypeReferenceHandle)h).Name),
            HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)h).Namespace) + "." + md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)h).Name),
            HandleKind.MemberReference => Label(md.GetMemberReference((MemberReferenceHandle)h).Parent) + "::" + md.GetString(md.GetMemberReference((MemberReferenceHandle)h).Name),
            HandleKind.MethodDefinition => Label(md.GetMethodDefinition((MethodDefinitionHandle)h).GetDeclaringType()) + "::" + md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)h).Name),
            HandleKind.MethodSpecification => Label(md.GetMethodSpecification((MethodSpecificationHandle)h).Method),
            _ => h.Kind.ToString()
        };
        refs.AddRange(md.AssemblyReferences.Select(h => md.GetString(md.GetAssemblyReference(h).Name)));
        foreach (var th in md.TypeDefinitions.Where(_ => Path.GetFileName(path).StartsWith("VA", StringComparison.OrdinalIgnoreCase) ||
            (complete && Path.GetRelativePath(args[0], path).StartsWith("SourceLinks", StringComparison.OrdinalIgnoreCase))))
        {
            var td = md.GetTypeDefinition(th);
            var name = Label(th);
            var methods = new List<object>();
            foreach (var mh in td.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                var methodName = md.GetString(m.Name);
                if ((m.Attributes & MethodAttributes.PinvokeImpl) != 0)
                {
                    var imp = m.GetImport();
                    pinvokes.Add(new { Method = name + "::" + methodName, Module = imp.Module.IsNil ? "<nil: unresolved import>" : md.GetString(md.GetModuleReference(imp.Module).Name), Entry = imp.Name.IsNil ? "<nil>" : md.GetString(imp.Name) });
                }
                if (!complete && !focus.IsMatch(name + " " + methodName)) continue;
                var calls = new List<string>();
                var instructions = new List<string>();
                var literals = new List<string>();
                var trace = name.EndsWith("VaCompletionUtils") || name.EndsWith("MacroParser") ||
                    (name.EndsWith("BufferListener") && new[] { "OnTimerTick", "CancelMyPendingParses", "OnBufferChanged" }.Contains(methodName));
                if (m.RelativeVirtualAddress != 0 && (m.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL)
                {
                    var il = pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()!;
                    for (int i = 0; i < il.Length;)
                    {
                        int offset = i;
                        ushort code = il[i++];
                        if (code == 0xfe) code = (ushort)(0xfe00 | il[i++]);
                        var op = opcodes[code];
                        int size = op.OperandType switch
                        {
                            OperandType.InlineNone => 0,
                            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                            OperandType.InlineVar => 2,
                            OperandType.InlineI8 or OperandType.InlineR => 8,
                            OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                            _ => 4
                        };
                        if (op.OperandType == OperandType.InlineMethod)
                            calls.Add($"IL_{offset:X4} {op.Name} {Label(MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i)))}");
                        if (op.OperandType == OperandType.InlineString)
                            literals.Add(md.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(il, i) & 0x00ffffff)));
                        if (trace)
                        {
                            var operand = size == 0 ? "" : Convert.ToHexString(il.AsSpan(i, size));
                            if (op.OperandType == OperandType.InlineMethod)
                                operand = Label(MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i)));
                            if (op.OperandType == OperandType.InlineBrTarget)
                                operand = $"IL_{i + size + BitConverter.ToInt32(il, i):X4}";
                            if (op.OperandType == OperandType.ShortInlineBrTarget)
                                operand = $"IL_{i + size + unchecked((sbyte)il[i]):X4}";
                            instructions.Add($"IL_{offset:X4} {op.Name} {operand}".TrimEnd());
                        }
                        i += size;
                    }
                }
                var parameters = m.GetParameters().Select(h => md.GetParameter(h))
                    .Select(p => new { Position = p.SequenceNumber, Name = md.GetString(p.Name) }).ToArray();
                methods.Add(new { Name = methodName, RVA = m.RelativeVirtualAddress, Attributes = m.Attributes.ToString(),
                    Parameters = parameters, Implementation = m.ImplAttributes.ToString(), Calls = calls, Instructions = instructions, Literals = literals });
            }
            if (methods.Count > 0) types.Add(new { Name = name, Methods = methods });
        }
    }
    results.Add(new { File = Path.GetRelativePath(args[0], path).Replace('\\', '/'), Size = new FileInfo(path).Length, SHA256 = hash,
        Machine = pe.PEHeaders.CoffHeader.Machine.ToString(), Managed = pe.HasMetadata,
        AssemblyReferences = refs, PInvokes = pinvokes, FocusedTypes = types });
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Inspected {results.Count} binaries; saved {args[1]}");
