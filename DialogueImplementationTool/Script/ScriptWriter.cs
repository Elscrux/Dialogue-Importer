using System.IO;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Script;

public class ScriptWriter(OutputPathProvider outputPathProvider, PapyrusCompilerWrapper compiler) {
    public void WriteScript(string scriptName, string content, ModKey modKey) {
        var directoryInfo = new DirectoryInfo(Path.Combine(outputPathProvider.OutputPath, modKey.FileName));
        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, modKey.FileName));

        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();

        var scriptsDirectory = Path.Combine(directoryInfo.FullName, "Scripts");
        var scriptsSourceDirectory = Path.Combine(scriptsDirectory, "Source");
        Directory.CreateDirectory(scriptsSourceDirectory);
        var sourcePath = Path.Combine(scriptsSourceDirectory, scriptName + ".psc");
        File.WriteAllText(sourcePath, content);
        compiler.Compile(sourcePath, scriptsDirectory, scriptsSourceDirectory);
    }
}
