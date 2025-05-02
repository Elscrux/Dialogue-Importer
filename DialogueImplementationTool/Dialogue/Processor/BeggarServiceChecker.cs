using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class BeggarServiceChecker : IConversationProcessor {
    [GeneratedRegex(@"Have a coin, beggar\.|Here, have a gold piece\. \(1 gold\)")]
    public static partial Regex BeggarRegex { get; }

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!BeggarRegex.IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.Beggar;
        foreach (var topicInfo in topic.TopicInfos) {
            topicInfo.Script.EndScriptLines.Add("""
                Actor PlayerRef = Game.GetPlayer() 
                PlayerRef.RemoveItem(Gold, akOtherContainer = akSpeaker)
                FavorJobsBeggarsAbility.Cast(PlayerRef, PlayerRef)
                FavorJobsBeggarsMessage.Show()

                If akSpeaker.GetRelationshipRank(PlayerRef) == 0
                  akSpeaker.SetRelationshipRank(PlayerRef, 1)
                EndIf
                """);
            topicInfo.Script.Properties.Add(new ScriptPropertyName(new ScriptObjectProperty {
                    Name = "Gold",
                    Flags = ScriptProperty.Flag.Edited,
                    Object = Skyrim.MiscItem.Gold001,
                },
                "MiscObject"));
            topicInfo.Script.Properties.Add(new ScriptPropertyName(new ScriptObjectProperty {
                    Name = "FavorJobsBeggarsAbility",
                    Flags = ScriptProperty.Flag.Edited,
                    Object = Skyrim.Spell.FavorJobsBeggarsAbility,
                },
                "Spell"));
            topicInfo.Script.Properties.Add(new ScriptPropertyName(new ScriptObjectProperty {
                    Name = "FavorJobsBeggarsMessage",
                    Flags = ScriptProperty.Flag.Edited,
                    Object = Skyrim.Message.FavorJobsBeggarMessage,
                },
                "Message"));
        }
    }
}
