using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using DialogueImplementationTool.Converter;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Mutagen.Bethesda.Plugins;
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
        StreamReader? streamReader = null;
        var tryLoading = true;
        while (tryLoading)
            try {
                streamReader = new StreamReader(FilePath);
                tryLoading = false;
            } catch (Exception e) {
                switch (MessageBox.Show(e.Message)) {
                    case MessageBoxResult.None:
                    case MessageBoxResult.Cancel:
                    case MessageBoxResult.No:
                        tryLoading = false;
                        break;
                    case MessageBoxResult.OK:
                    case MessageBoxResult.Yes:
                        break;
                    default: throw new InvalidOperationException();
                }
            }

        if (streamReader is null) return [];

        var isPreHeader = true;
        var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => {
                isPreHeader = false;
                return args.Header;
            },
            ShouldSkipRecord = args => {
                // Skip rows at the start that before the header row
                if (!isPreHeader) return false;

                for (var i = 0; i < args.Row.ColumnCount; i++) {
                    var field = args.Row.GetField(i);
                    if (field == nameof(GenericDialogue.Line)) {
                        return false;
                    }
                }

                return true;
            },
        };
        using var csvReader = new CsvReader(streamReader, csvConfiguration);
        csvReader.Context.RegisterClassMap<GenericDialogueRecordMap>();
        csvReader.Context.TypeConverterCache.AddConverter(new MaleFemaleGenderTypeConverter());

        var fileName = fileSystem.Path.GetFileNameWithoutExtension(FilePath);
        var defaultFormKey = FormKey.Null;
        if (context.LinkCache.TryResolve<IVoiceTypeOrListGetter>(fileName, out var voiceTypeOrList)) {
            defaultFormKey = voiceTypeOrList.FormKey;
        }
        var voiceType = context.SelectRecord<IVoiceTypeOrList, IVoiceTypeOrListGetter>($"Voice Type or FormList of Voice Types for {fileName}", defaultFormKey);
        if (voiceType.EditorID is null) throw new InvalidOperationException("Voice Type is not set");

        return csvReader.GetRecords<GenericDialogue>()
            .Select(genericDialogue => {
                if (genericDialogue.Category.IsNullOrWhitespace()) return null;

                if (genericDialogue.Line.IsNullOrEmpty()) {
                    genericDialogue = genericDialogue with { Line = " " };
                }

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
                        { GenericMetaData.Category, categoryEnum },
                        { GenericMetaData.Subtype, subtypeEnum },
                        { GenericMetaData.Description, genericDialogue.Description },
                        { GenericMetaData.VoiceType, voiceType },
                        { GenericMetaData.GenericQuestFactory, new VoiceTypeGenericDialogueQuestFactory(context, voiceType) },
                        { GenericMetaData.GenericDialogTopicFactory, new NewDialogueTopicFactory(context) },
                        { GenericMetaData.PostProcessor, new NullPostProcessor() },
                    },
                };
                var topic = new DialogueTopic { TopicInfos = [topicInfo] };

                processor.Process(genericDialogue, topicInfo);

                return topic;
            })
            .WhereNotNull()
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
