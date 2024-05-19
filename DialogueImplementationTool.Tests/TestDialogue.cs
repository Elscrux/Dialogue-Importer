using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
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

    private static DialogueTopic GetTopicInfos(ISpeaker speaker, string prompt, IEnumerable<string> infos) {
        return new DialogueTopic {
            TopicInfos = infos
                .Select(r => new DialogueTopicInfo {
                    Speaker = speaker,
                    Prompt = prompt,
                    Responses = [new DialogueResponse { Response = r }],
                })
                .ToList(),
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

    public static DialogueTopic GetSceneBranchesOldwallScene1() {
        var testConstants = new TestConstants();
        return GetTopicInfos(
            testConstants.Speaker1,
            string.Empty,
            [
                "Jastara: Godehard. Well met.",
                "Godehard: Well met, good day, good evening. Are you after another old war story of mine?",
                "Godehard: What do you say?",
                "Jastara: I wanted to write a song about it - for my daughter.",
                "Jastara:Who was your commander at the Red Ring?",
                "Godehard: Sevino Seloth, Captain from Cheydinhal, half man, half Dark Elf.",
                "Jastara: Thank you. That's what I had forgotten.",
            ]);
    }

    public static DialogueTopic GetSceneBranchesOldwallScene2() {
        var testConstants = new TestConstants();
        return GetTopicInfos(
            testConstants.Speaker1,
            string.Empty,
            [
                "Jastara: Godehard. Well met.",
                "Godehard: Well met, good day, good evening. Are you after another old war story of mine?",
                "Godehard: What do you think?",
                "Jastara: A story of my own I have to offer, that I do. Those brigands were back, and they made quite a lot of noise.",
                "Godehard: Is that so? Find them and scold them, I will, and with a guard or two at my back.",
                "Jastara: Thank you. That should be enough.",
            ]);
    }

    public static DialogueTopic GetGreetingTopicCraneShore1() {
        var testConstants = new TestConstants();
        return GetTopicInfos(
            testConstants.Speaker1,
            string.Empty,
            [
                "[initial] Come on in and look around. We stock everything the Company tells us to stock, and then some.",
                "[initial greeting] Come on in and look around. We stock everything the Company tells us to stock, and then some.",
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

    public static DialogueTopic GetDialogueTopicStyleGuideLinks() {
        var testConstants = new TestConstants();
        return new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = testConstants.Speaker1,
                    Prompt = "What are our next steps?",
                    Responses = [
                        new DialogueResponse {
                            Response =
                                "[DONE] We should bring this back to Telwyne. She'll probably have some ideas on where these eels might be burrowed.",
                        },
                        new DialogueResponse {
                            Response =
                                "I'll see you back at headquarters. [Objective Completed: Return to Carpius] [Objective Granted: Speak with Telwyne] [end dialogue]",
                        },
                    ],
                    Links = [
                        GetTopic(
                            testConstants.Speaker1,
                            "You need to get better at your job.",
                            "Oh, no. This really isn't going to look good in my next performance review. [merge to DONE above]"),
                        GetTopic(
                            testConstants.Speaker1,
                            "Don't be too hard on yourself.",
                            "I guess the effects of the bait must have thrown me off. [merge to DONE above]"),
                    ],
                },
            ],
        };
    }

    public static List<DialogueTopic> GetDialogueTopicStyleGuideOptionsLinks() {
        var testConstants = new TestConstants();
        return [
            new DialogueTopic {
                TopicInfos = [
                    new DialogueTopicInfo {
                        Speaker = testConstants.Speaker1,
                        Responses = [
                            new DialogueResponse {
                                Response =
                                    "[CONTINUE] Anyway, I need you to go fetch him before they try to drown him in the waves. Think you can manage that? [HERE]",
                            },
                        ],
                        Links = [
                            GetTopic(
                                testConstants.Speaker1,
                                "I'll bring Carpius back here.",
                                "Good. Do it now. The boss is breathing down my neck to get this resolved. The sooner this gets done with, the better. [Quest Started: As Above, So Below] [Objective Granted: Speak with Carpius] [end dialogue]"),
                            GetTopic(
                                testConstants.Speaker1,
                                "Actually, I need to be somewhere else.",
                                "Then why are you here? You damned locals are infuriating! [end dialogue]"),
                        ],
                    },
                ],
            },
            new DialogueTopic {
                TopicInfos = [
                    new DialogueTopicInfo {
                        Speaker = testConstants.Speaker1,
                        Prompt = "About that problem you needed help with...",
                        Responses = [
                            new DialogueResponse {
                                Response =
                                    "Decided you weren't so busy after all? [merge to HERE above]",
                            },
                        ],
                    },
                ],
            },
        ];
    }
}
