using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DialogueImplementationTool.Converter;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using ReactiveUI;
namespace DialogueImplementationTool.Parser;

public sealed class CsvDocumentParser(
    string path,
    IFileSystem fileSystem,
    IDialogueContext context)
    : ReactiveObject, IGenericDialogueParser {
    public string FilePath { get; } = path;

    public List<DialogueTopic> ParseGenericDialogue(IDialogueProcessor processor) {
        using var streamReader = new StreamReader(FilePath);
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var csvReader = new CsvReader(streamReader, csvConfiguration);
        csvReader.Context.RegisterClassMap<GenericDialogueRecordMap>();
        csvReader.Context.TypeConverterCache.AddConverter(new MaleFemaleGenderTypeConverter());

        var fileName = fileSystem.Path.GetFileNameWithoutExtension(FilePath);
        var voiceType = context.SelectRecord<VoiceType, IVoiceTypeGetter>($"Voice Type for {fileName}");
        if (voiceType.EditorID is null) throw new InvalidOperationException("Voice Type is not set");

        return csvReader.GetRecords<GenericDialogue>()
            .Where(g => !g.Line.IsNullOrEmpty())
            .Select(genericDialogue => {
                var (categoryEnum, subtypeEnum) = DialogCategoryConverter.Convert(genericDialogue.Category);
                var response = new DialogueResponse {
                    Response = genericDialogue.Line,
                    Emotion = genericDialogue.Emotion ?? Emotion.Neutral,
                    EmotionValue = genericDialogue.EmotionValue.HasValue ? (uint) genericDialogue.EmotionValue.Value : 50,
                    ScriptNote = genericDialogue.VaNotes,
                    StartNotes = genericDialogue.ExtraConditions.IsNullOrEmpty()
                        ? []
                        : [new Note { Text = genericDialogue.ExtraConditions }]
                };

                var topicInfo = new DialogueTopicInfo {
                    Responses = [response],
                    MetaData = new Dictionary<string, object> {
                        { "Category", categoryEnum },
                        { "Subtype", subtypeEnum },
                        { "Description", genericDialogue.Description },
                        { "VoiceType", voiceType },
                        { "GenericQuestFactory", new VoiceTypeGenericDialogueQuestFactory(context, voiceType) },
                    },
                };
                var topic = new DialogueTopic { TopicInfos = [topicInfo] };

                processor.Process(genericDialogue, topicInfo);

                return topic;
            })
            .ToList();
    }

    public sealed class GenericDialogueRecordMap : ClassMap<GenericDialogue> {
        public GenericDialogueRecordMap() {
            Map(m => m.Description).Name("Description");
            Map(m => m.Category).Name("Category");
            Map(m => m.Line).Name("Line");
            Map(m => m.ExtraConditions).Name("Imp Extra Conditions");
            Map(m => m.VaNotes).Name("VA notes");
            Map(m => m.Emotion).Name("Emotion");
            Map(m => m.EmotionValue).Name("Emotion Value");
            Map(m => m.Weather).Name("Weather");
            Map(m => m.Time).Name("Time");
            Map(m => m.PlayerSex).Name("Player Sex");
            Map(m => m.PlayerRace).Name("Player Race");
        }
    }
}
