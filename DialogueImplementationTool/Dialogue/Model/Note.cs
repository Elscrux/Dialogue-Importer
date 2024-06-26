﻿using System.Collections.Generic;
using System.Drawing;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed class Note {
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<Color> Colors { get; init; } = [];
}
