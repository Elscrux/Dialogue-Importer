using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Environments;
namespace DialogueImplementationTool.Script;

public sealed class PapyrusCompilerWrapper {
    public string PapyrusCompilerPath { get; }
    public string FlagsFilePath { get; }
    public string SourceDirectoryPath { get; }

    public PapyrusCompilerWrapper(IGameEnvironment gameEnvironment) {
        var gameDirectory = gameEnvironment.DataFolderPath.Directory;
        if (!gameDirectory.HasValue) throw new DirectoryNotFoundException("Game directory not found");

        var papyrusCompilerPath = Path.Combine(gameDirectory.Value, @"Papyrus Compiler\PapyrusCompiler.exe");
        if (!File.Exists(papyrusCompilerPath))
            throw new FileNotFoundException($"PapyrusCompiler.exe not found at {papyrusCompilerPath}");

        PapyrusCompilerPath = papyrusCompilerPath;

        SourceDirectoryPath = Path.Combine(gameEnvironment.DataFolderPath, "Scripts", "Source");
        if (!Directory.Exists(SourceDirectoryPath))
            throw new DirectoryNotFoundException($"Scripts\\Source directory not found at {SourceDirectoryPath}");

        FlagsFilePath = Path.Combine(gameEnvironment.DataFolderPath, "Scripts", "Source", "TESV_Papyrus_Flags.flg");
        if (!File.Exists(FlagsFilePath))
            throw new FileNotFoundException($"TESV_Papyrus_Flags.flg not found at {FlagsFilePath}");
    }

    public void Compile(string sourcePath, string outputPath, params string[] scriptLibraries) {
        var process = new Process {
            StartInfo = {
                FileName = PapyrusCompilerPath,
                Arguments = $"\"{sourcePath}\" -f=\"{FlagsFilePath}\" -i=\"{string.Join(';', scriptLibraries.Append(SourceDirectoryPath))}\" -o=\"{outputPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0) {
            throw new Exception($"PapyrusCompiler exited with code {process.ExitCode}\n{output}\n{error}");
        }
    }
}
