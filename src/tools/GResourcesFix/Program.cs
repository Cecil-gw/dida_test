using Mono.Cecil;

var sourceExe = args[0];
var targetExe = args[1];

var source = AssemblyDefinition.ReadAssembly(sourceExe);
var gr = source.MainModule.Resources.FirstOrDefault(r => r.Name.EndsWith(".g.resources")) as EmbeddedResource
    ?? throw new FileNotFoundException(".g.resources not found in source");

var resolver = new DefaultAssemblyResolver();
var fwDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget", "packages", "microsoft.netframework.referenceassemblies.net462", "1.0.3", "build", ".NETFramework", "v4.6.2");
resolver.AddSearchDirectory(fwDir);
resolver.AddSearchDirectory(Path.GetDirectoryName(targetExe)!);
var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
resolver.AddSearchDirectory(Path.Combine(windowsDir, "Microsoft.NET", "Framework64", "v4.0.30319", "WPF"));
resolver.AddSearchDirectory(Path.Combine(windowsDir, "Microsoft.NET", "Framework", "v4.0.30319", "WPF"));
var target = AssemblyDefinition.ReadAssembly(targetExe, new ReaderParameters
{
    InMemory = true,
    AssemblyResolver = resolver
});
var existing = target.MainModule.Resources.FirstOrDefault(r => r.Name.EndsWith(".g.resources"));
if (existing != null) target.MainModule.Resources.Remove(existing);
target.MainModule.Resources.Add(new EmbeddedResource("TickTick.g.resources", gr.Attributes, gr.GetResourceData()));
var tempExe = targetExe + ".injecting";
try
{
    target.Write(tempExe);
    File.Copy(tempExe, targetExe, overwrite: true);
}
finally
{
    if (File.Exists(tempExe)) File.Delete(tempExe);
}
Console.WriteLine("Injected .g.resources (" + gr.GetResourceData().Length + " bytes) into " + targetExe);
