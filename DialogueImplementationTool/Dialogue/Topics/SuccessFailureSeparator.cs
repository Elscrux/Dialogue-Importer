// using System;
// using System.Text.RegularExpressions;
// using DialogueImplementationTool.Dialogue.Responses;
// namespace DialogueImplementationTool.Dialogue.Topics; 
//
// public sealed class SuccessFailureSeparator : IDialogueTopicPostProcessor {
// 	// Success regex
// 	public Regex SuccessRegex { get; } = new(@"\[success\]", RegexOptions.IgnoreCase);
// 	public Regex FailureRegex { get; } = new(@"\[failure\]", RegexOptions.IgnoreCase);
//     
// 	public void Process(DialogueTopic topic) {
// 		if (topic.Responses.Count == 0) return;
//
// 		DialogueResponse? successResponse = null;
// 		DialogueResponse? failureResponse = null;
// 		foreach (var dialogueResponse in topic.Responses) {
// 			if (SuccessRegex.IsMatch(dialogueResponse.Response)) {
// 				successResponse = dialogueResponse;
// 			} else if (FailureRegex.IsMatch(dialogueResponse.Response)) {
// 				failureResponse = dialogueResponse;
// 			}
// 		}
//         
// 		if (successResponse == null && failureResponse == null) return;
//
// 		var successIndex = topic.Responses.IndexOf(successResponse);
// 		var failureIndex = topic.Responses.IndexOf(failureResponse);
//
// 		var min = Math.Min(successIndex, failureIndex);
// 		
// 	}
// }
