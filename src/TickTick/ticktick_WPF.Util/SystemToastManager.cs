using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Toolkit.Uwp.Notifications;
using TickTickUtils.Lang;
using Windows.Foundation;
using Windows.UI.Notifications;

namespace ticktick_WPF.Util;

public class SystemToastManager
{
	public static void Notify(string id, string group, string img, string title, string content, Dictionary<string, List<ToastCommands>> paras = null, List<ToastCommands> commands = null, bool silent = false)
	{
		ToastContentBuilder toastContentBuilder = new ToastContentBuilder().AddArgument("action", id).AddArgument("conversationId", 9813);
		int? hintMaxLines = 2;
		ToastContentBuilder toastContentBuilder2 = toastContentBuilder.AddText(title, null, null, hintMaxLines).AddText(content).AddAppLogoOverride(new Uri(img), ToastGenericAppLogoCrop.Default);
		AddToastInput(toastContentBuilder2, paras);
		AddToastButtons(toastContentBuilder2, commands);
		toastContentBuilder2.SetToastScenario(ToastScenario.Reminder);
		if (silent)
		{
			toastContentBuilder2.AddAudio(null, false, true);
		}
		ToastNotification toastNotification = new ToastNotification(toastContentBuilder2.GetToastContent().GetXml())
		{
			Tag = id,
			Group = group
		};
		ToastNotification toastNotification2 = toastNotification;
		toastNotification2.Dismissed += async delegate(ToastNotification sender, ToastDismissedEventArgs args)
		{
			if (args.Reason == ToastDismissalReason.UserCanceled)
			{
				try
				{
					await SystemToastUtils.HandleReminderToastClosed(id, "dismiss");
				}
				catch (Exception ex)
				{
					UtilLog.Warn("handle reminder toast dismiss failed: " + ex.Message);
				}
			}
		};
		ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);
	}

	private static void AddToastInput(ToastContentBuilder t, Dictionary<string, List<ToastCommands>> paras)
	{
		if (paras == null || paras.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, List<ToastCommands>> para in paras)
		{
			if (para.Value == null || para.Value.Count == 0)
			{
				continue;
			}
			ToastSelectionBox toastSelectionBox = new ToastSelectionBox(para.Key)
			{
				DefaultSelectionBoxItemId = para.Value[0].Argument
			};
			foreach (ToastCommands item in para.Value)
			{
				toastSelectionBox.Items.Add(new ToastSelectionBoxItem(item.Argument, item.Content));
			}
			t.AddToastInput(toastSelectionBox);
		}
	}

	private static void AddToastButtons(ToastContentBuilder t, List<ToastCommands> commands)
	{
		if (commands == null || commands.Count == 0)
		{
			return;
		}
		foreach (ToastCommands command in commands)
		{
			t.AddButton(new ToastButton().SetContent(command.Content).AddArgument("action", command.Argument));
		}
	}
}
