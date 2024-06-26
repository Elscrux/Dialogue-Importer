﻿using System;
using System.IO;
namespace DialogueImplementationTool.Services;

public sealed class OutputPathProvider {
    public string OutputPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

    public void CreateIfMissing() {
        if (!Directory.Exists(OutputPath)) Directory.CreateDirectory(OutputPath);
    }
}
