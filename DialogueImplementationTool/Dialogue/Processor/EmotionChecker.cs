using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Services;
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
                var (emotion, value) = emotionClassifier.Classify(response.Response);
                response.Emotion = emotion;
                response.EmotionValue = value;
            }
        }
    }
}
