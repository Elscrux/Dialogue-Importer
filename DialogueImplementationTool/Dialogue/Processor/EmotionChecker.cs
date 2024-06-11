using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Services;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class EmotionChecker(IEmotionClassifier emotionClassifier) : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var topic in conversation.SelectMany(x => x.Topics).SelectMany(x => x.EnumerateLinks(true))) {
            foreach (var topicInfo in topic.TopicInfos) {
                Process(topicInfo);
            }
        }
    }

    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.SharedInfo is not null) {
            ProcessInternal(topicInfo.SharedInfo.ResponseDataTopicInfo);
        }

        ProcessInternal(topicInfo);

        void ProcessInternal(DialogueTopicInfo info) {
            foreach (var response in info.Responses) {
                var line = response.ScriptNote.IsNullOrEmpty()
                    ? response.Response
                    : $"[{response.ScriptNote}] {response.Response}";

                var emotionValue = emotionClassifier.Classify(line);
                response.Emotion = emotionValue.Emotion;
                response.EmotionValue = emotionValue.Value;
            }
        }
    }
}
