using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Tests;

public static class TestDialogue {
    private static DialogueTopic GetTopic(ISpeaker speaker, string prompt) {
        return new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = speaker,
                    Prompt = prompt,
                },
            ],
        };
    }

    private static DialogueTopic GetTopic(ISpeaker speaker, string prompt, string response) {
        return new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = speaker,
                    Prompt = prompt,
                    Responses = [
                        new DialogueResponse {
                            Response = response,
                        },
                    ],
                },
            ],
        };
    }

    private static DialogueTopic GetTopic(ISpeaker speaker, string prompt, IEnumerable<string> responses) {
        return new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = speaker,
                    Prompt = prompt,
                    Responses = responses
                        .Select(r => new DialogueResponse { Response = r })
                        .ToList(),
                },
            ],
        };
    }

    public static List<GeneratedDialogue> TopicAsGeneratedDialogue(DialogueTopic topic) {
        var testConstants = new TestConstants();
        return [
            new GeneratedDialogue(testConstants.SkyrimDialogueContext,
                DialogueType.Dialogue,
                [topic],
                testConstants.Speaker1.FormKey),
        ];
    }

    public static DialogueTopic GetGreetingTopicCraneShore1() {
        var testConstants = new TestConstants();
        return GetTopic(
            testConstants.Speaker1,
            string.Empty,
            [
                "[initial] Come on in and look around. We stock everything the Company tells us to stock, and then some.",
                "You're talking to a Sun-Screamer, so watch your words.",
                "[if player is a Nord] You're not a Sea-Born are you? Or Talos forbid, a Scale-Fin?",
                "What do you want? Can't you see I'm busy?",
                "Oh look, another mainlander.",
                "[neutral] You're back. What is it this time?",
            ]);
    }

    public static DialogueTopic GetDialogueTopicCraneShore1() {
        var testConstants = new TestConstants();
        return new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = testConstants.Speaker1,
                    Prompt = "What can you tell me about Crane Shore?",
                    Responses = [
                        new DialogueResponse {
                            Response =
                                "The place has potential, but right now it's a Company mining town filled with crusty old veterans, Imperials, and nosy pissants like you.",
                        },
                    ],
                    Links = [
                        GetTopic(
                            testConstants.Speaker1,
                            "Are you always so hostile?"),
                        GetTopic(
                            testConstants.Speaker1,
                            "Why so hostile?",
                            "[annoyed] Hostile? You know nothing about me, stranger. My people were rulers once! Now we're reduced to mingling with milk drinkers like you. [end dialogue] [remove root option] [no farewell]"),
                        GetTopic(
                            testConstants.Speaker1,
                            "There's no need to be like that. We can be friends. (Illusion) [Hard]?",
                            [
                                "[failure] [dazed] Wha-what was that? What did you do? What are you, some kind of dark wizard? Get away from me! [end dialogue] [remove root option] [no farewell]",
                                "[success] We can't be friends, but you're right, I didn't need to act so brash. I'm sorry. How can I help you?",
                            ]),
                        GetTopic(
                            testConstants.Speaker1,
                            "You think Crane Shore could be more than it is? (Persuade) [average]",
                            [
                                "[success] Absolutely! We Nords have a proud history of settlement and statecraft. We share this ancestry with the native Roscreans.",
                                "But they've squandered it. While we rose to become kings and emperors, the natives of this land became insular, like cave dwellers.",
                                "We Sun-Screamers, we once ruled Morthal like no other clan in its history. All Skyrim respected us. It could be the same for us here in Roscrea. [unlock ROSCREA and CLAN] [remove root option] [back to root]",
                                "Anyway, why are we talking again?",
                                "[failure] Think? I know it could, but with you people like you around, things will never improve.",
                                "Do the whole town a favor and go back to wherever you came from. [end dialogue] [remove root option] [no farewell]",
                                "Anyway, why are we talking again?",
                            ]),
                    ],
                },
            ],
        };
    }
}
