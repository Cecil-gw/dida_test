using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views.AI;

internal static class AgentBridgeEvents
{
	public const string GetApprovals = "agent.getApprovals";

	public const string RespondToApproval = "agent.respondToApproval";

	public const string SubmitRating = "agent.submitRating";

	public const string GetState = "agent.getState";

	public const string SendMessage = "agent.sendMessage";

	public const string EditMessage = "agent.editMessage";

	public const string SwitchConversation = "agent.switchConversation";

	public const string NewConversation = "agent.newConversation";

	public const string StopGenerating = "agent.stopGenerating";

	public const string ScrollToBottom = "agent.scrollToBottom";

	public const string DebugNonBlockingAppUpdateRequired = "agent.debugNonBlockingAppUpdateRequired";

	public const string DebugBlockingAppUpdateRequired = "agent.debugBlockingAppUpdateRequired";

	public const string ExitFullCardListMode = "agent.exitFullCardListMode";

	public const string FillComposer = "agent.fillComposer";

	public const string ApprovalsChanged = "agent.approvalsChanged";

	public const string ShowFeedback = "agent.showFeedback";

	public const string RatingResult = "agent.ratingResult";

	public const string ConversationCreated = "agent.conversationCreated";

	public const string RefreshConversationTitle = "agent.refreshConversationTitle";

	public const string PageChanged = "agent.pageChanged";

	public const string TokenLimitReached = "agent.tokenLimitReached";

	public const string AppUpdateRequired = "agent.appUpdateRequired";

	public const string CardClick = "agent.cardClick";

	public const string ViewFullCardListMode = "agent.viewFullCardListMode";

	public const string StateChanged = "agent.stateChanged";

	public const string ScrollToBottomButtonVisibilityChanged = "agent.scrollToBottomButtonVisibilityChanged";

	private static readonly HashSet<string> ClientToWebEvents = new HashSet<string>(new string[13]
	{
		"agent.getApprovals", "agent.respondToApproval", "agent.submitRating", "agent.getState", "agent.sendMessage", "agent.editMessage", "agent.switchConversation", "agent.newConversation", "agent.stopGenerating", "agent.scrollToBottom",
		"agent.debugNonBlockingAppUpdateRequired", "agent.debugBlockingAppUpdateRequired", "agent.exitFullCardListMode"
	}, StringComparer.Ordinal);

	private static readonly HashSet<string> WebToClientEvents = new HashSet<string>(new string[13]
	{
		"agent.fillComposer", "agent.approvalsChanged", "agent.showFeedback", "agent.ratingResult", "agent.conversationCreated", "agent.refreshConversationTitle", "agent.pageChanged", "agent.tokenLimitReached", "agent.appUpdateRequired", "agent.cardClick",
		"agent.viewFullCardListMode", "agent.stateChanged", "agent.scrollToBottomButtonVisibilityChanged"
	}, StringComparer.Ordinal);

	public static bool CanCallWeb(string methodName)
	{
		if (methodName != null)
		{
			return ClientToWebEvents.Contains(methodName);
		}
		return false;
	}

	public static bool CanRegisterNativeMethod(string methodName)
	{
		if (methodName != null)
		{
			if (!WebToClientEvents.Contains(methodName))
			{
				return AgentNativeMethods.IsSupported(methodName);
			}
			return true;
		}
		return false;
	}
}
