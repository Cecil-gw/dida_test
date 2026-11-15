using Mono.Cecil;
var asm = AssemblyDefinition.ReadAssembly(args[0]);
foreach (var r in asm.MainModule.Resources.Take(30))
    Console.WriteLine(r.Name + "  " + r.ResourceType + (r is EmbeddedResource e ? " len=" + e.GetResourceData().Length : ""));
Console.WriteLine("total=" + asm.MainModule.Resources.Count);
