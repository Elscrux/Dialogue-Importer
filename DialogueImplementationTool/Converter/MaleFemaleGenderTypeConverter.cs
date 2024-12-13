using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Converter;

public sealed class MaleFemaleGenderTypeConverter : DefaultTypeConverter {
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) {
        if (Enum.TryParse<MaleFemaleGender>(text, out var result)) {
            return result;
        }

        return null;
    }
}
