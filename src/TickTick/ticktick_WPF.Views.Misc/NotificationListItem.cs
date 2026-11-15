using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.EmbeddedBrowser;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Upgrade;

namespace ticktick_WPF.Views.Misc;

public class NotificationListItem : UserControl, IComponentConnector
{
	private NotificationViewModel _notificationItem;

	internal TextBlock TitleTextBlock;

	private bool _contentLoaded;

	public NotificationListItem()
	{
		InitializeComponent();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_notificationItem = base.DataContext as NotificationViewModel;
		GetTitle(_notificationItem?.notification);
	}

	private void OnTitleClick(object sender, MouseButtonEventArgs e)
	{
		if ((sender as TextBlock)?.Tag != null)
		{
			string text = ((TextBlock)sender).Tag.ToString();
			if (!string.IsNullOrEmpty(text))
			{
				try
				{
					Process.Start(text);
				}
				catch (Exception)
				{
				}
			}
		}
		if (_notificationItem?.type == "subscribeCharge")
		{
			App.Window.LeftTabBar.NotificationPopup.IsOpen = false;
			SettingDialog.ShowSettingDialog(SettingsType.Premium, App.Window);
		}
	}

	private async void OnNotificationAcceptClick(object sender, RoutedEventArgs e)
	{
		if (_notificationItem == null)
		{
			return;
		}
		_notificationItem.IsButtonEnable = "False";
		bool acceptShareReturn = false;
		switch (_notificationItem.actionStatus)
		{
		case 21:
		{
			if (!(_notificationItem.type == "team"))
			{
				break;
			}
			string value5 = await Communicator.HandleTeamInvite(_notificationItem.id, accept: true);
			if (string.IsNullOrEmpty(value5))
			{
				_notificationItem.ButtonVisibility = "Collapsed";
				_notificationItem.ActionVisibility = "Visible";
				_notificationItem.ActionText = Utils.GetString("Accepted");
				SyncManager.Sync();
				break;
			}
			string text = JsonConvert.DeserializeObject<ErrorModel>(value5)?.errorCode;
			if (!(text == "user_already_trial"))
			{
				if (text == "team_exceed_limit")
				{
					new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("JoinTeamExceedLimit"), Utils.GetString("GotIt"), "").ShowDialog();
				}
				else
				{
					new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				}
			}
			else
			{
				new CustomerDialog(Utils.GetString("OperateFailed"), string.Format(Utils.GetString("TeamUserAlreadyTrial"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog();
			}
			break;
		}
		case 0:
		{
			if (_notificationItem.type == "PayReminder")
			{
				Utils.StartUpgrade("before_expire");
				break;
			}
			long projectLimitNumber = Utils.GetUserLimit(Constants.LimitKind.ProjectNumber);
			List<string> teamIds = (from t in CacheManager.GetTeams()
				select t.id).ToList();
			List<ProjectModel> list = (UserDao.IsPro() ? (await ProjectDao.GetAllProjects(withInbox: false, withClosed: false)).Where((ProjectModel p) => !TeamDao.IsTeamExpired(p.teamId)).ToList() : (await ProjectDao.GetAllProjects(withInbox: false)).ToList());
			List<ProjectModel> projects = list;
			projects = projects.Where((ProjectModel p) => string.IsNullOrEmpty(p.teamId) || teamIds.Contains(p.teamId)).ToList();
			if (projectLimitNumber <= projects.Count)
			{
				if (UserDao.IsPro())
				{
					string content = string.Format(Utils.GetString("ListsLimitAdd"), projects.Count);
					CustomerDialog customerDialog4 = new CustomerDialog(Utils.GetString("ProjectLimitTips"), content, MessageBoxButton.OK);
					customerDialog4.Owner = Window.GetWindow(this);
					customerDialog4.ShowDialog();
				}
				else
				{
					ProChecker.ShowUpgradeDialog(ProType.MoreLists);
				}
				_notificationItem.IsButtonEnable = "True";
				return;
			}
			string value9 = await Communicator.SetAcceptShare(_notificationItem.projectid, _notificationItem.id, 1);
			if (string.IsNullOrEmpty(value9))
			{
				acceptShareReturn = true;
				await SyncManager.Sync();
				_notificationItem.notification.actionStatus = 1;
				GetTitle(_notificationItem.notification);
				break;
			}
			TeamErrorDataModel teamErrorDataModel2 = null;
			ErrorModel errorModel2 = JsonConvert.DeserializeObject<ErrorModel>(value9);
			try
			{
				teamErrorDataModel2 = JsonConvert.DeserializeObject<TeamErrorDataModel>(errorModel2.data.ToString());
			}
			catch
			{
			}
			if (errorModel2 == null)
			{
				break;
			}
			switch (errorModel2.errorCode)
			{
			case "user_email_not_verified":
				ShowAcceptFailureDialog();
				break;
			case "exceed_quota":
				if (UserDao.IsPro())
				{
					string content2 = string.Format(Utils.GetString("ListsLimitAdd"), projects.Count);
					CustomerDialog customerDialog5 = new CustomerDialog(Utils.GetString("ProjectLimitTips"), content2, MessageBoxButton.OK);
					customerDialog5.Owner = Window.GetWindow(this);
					customerDialog5.ShowDialog();
				}
				else
				{
					ProChecker.ShowUpgradeDialog(ProType.MoreLists);
				}
				break;
			case "team_project_exceed_quota":
				new CustomerDialog(Utils.GetString("AddToFailed"), Utils.GetString("GuestTeamProjectExceedQuota"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "visitor_exceed_quota":
			{
				if (errorModel2.data != null && errorModel2.data.TryGetValue("limit", out JToken value10))
				{
					int result2;
					int num2 = (int.TryParse(value10.ToString(), out result2) ? result2 : 29);
					string content3 = string.Format(Utils.GetString((num2 <= 1) ? "FreeLimitedSharingGuests" : "ProLimitedSharingGuests"), num2);
					CustomerDialog customerDialog6 = new CustomerDialog(Utils.GetString("LimitTips"), content3, MessageBoxButton.OK);
					customerDialog6.Owner = App.Window;
					customerDialog6.ShowDialog();
					return;
				}
				break;
			}
			case "no_team_permission":
				new CustomerDialog(Utils.GetString("AddToFailed"), string.Format(Utils.GetString("NoTeamPermission01"), teamErrorDataModel2?.projectOwnerName ?? " "), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "exceed_team_max_share_limit":
				if (UserDao.IsPro())
				{
					long userLimit2 = Utils.GetUserLimit(Constants.LimitKind.ShareUserNumber);
					string format2 = Utils.GetString("ProShareListLimit");
					new CustomerDialog(Utils.GetString("LimitTips"), string.Format(format2, userLimit2), MessageBoxButton.OK).ShowDialog();
				}
				else
				{
					ProChecker.ShowUpgradeDialog(ProType.MoreSharingMembers);
				}
				break;
			default:
				new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			}
			break;
		}
		case 1:
			if (_notificationItem.type == "projectPermission")
			{
				string value6 = await Communicator.AcceptApplyPermission(_notificationItem.id);
				if (string.IsNullOrEmpty(value6))
				{
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Accepted");
					AvatarHelper.ResetProjectAvatars(_notificationItem.notification.notificationUserData.projectId);
					SyncManager.Sync();
					break;
				}
				try
				{
					_ = JsonConvert.DeserializeObject<ErrorModel>(value6)?.errorCode;
					new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				}
				catch (Exception value7)
				{
					Console.WriteLine(value7);
					throw;
				}
			}
			else
			{
				if (!(_notificationItem.type == "team"))
				{
					break;
				}
				string value8 = await Communicator.AcceptTeamApply(_notificationItem.id);
				if (!string.IsNullOrEmpty(value8))
				{
					string text = JsonConvert.DeserializeObject<ErrorModel>(value8)?.errorCode;
					if (text == null)
					{
						return;
					}
					switch (text.Length)
					{
					case 18:
						switch (text[0])
						{
						case 'u':
							if (text == "user_already_trial")
							{
								new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("TeamAcceptMemberAlreadyTrial"), Utils.GetString("GotIt"), "").ShowDialog();
							}
							break;
						case 'n':
							if (text == "no_team_permission")
							{
								new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("TeamNotExist"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog();
							}
							break;
						}
						break;
					case 12:
						switch (text[0])
						{
						case 'e':
						{
							if (!(text == "exceed_limit"))
							{
								break;
							}
							TeamMessageModel teamMessageModel = await Communicator.GetTeamMessage(_notificationItem.teamId);
							if (teamMessageModel != null && teamMessageModel.trial)
							{
								CustomerDialog customerDialog2 = new CustomerDialog(Utils.GetString("MemberCountLimit"), string.Format(Utils.GetString("TeamMemberCountLimitText"), _notificationItem.teamName), Utils.GetString("BuyNow"), Utils.GetString("Cancel"));
								customerDialog2.ShowDialog();
								if (customerDialog2.DialogResult == true)
								{
									string arg2 = await Communicator.GetSignOnToken();
									Utils.TryProcessStartUrl(string.Format(BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token={0}&dest={1}", arg2, string.Format(Utils.GetString("RenewTeamUrl"), _notificationItem.teamId)));
								}
							}
							else
							{
								CustomerDialog customerDialog3 = new CustomerDialog(Utils.GetString("TeamMemberCountLimit"), string.Format(Utils.GetString("TeamMemberCountLimitText"), _notificationItem.teamName), Utils.GetString("AddLimit"), Utils.GetString("Cancel"));
								customerDialog3.ShowDialog();
								if (customerDialog3.DialogResult == true)
								{
									string arg3 = await Communicator.GetSignOnToken();
									Utils.TryProcessStartUrl(string.Format(BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token={0}&dest={1}", arg3, string.Format(Utils.GetString("manageTeamUrl"), _notificationItem.teamId)));
								}
							}
							break;
						}
						case 't':
							if (text == "team_expired" && new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("TeamExpired"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog() == true)
							{
								string arg = await Communicator.GetSignOnToken();
								Utils.TryProcessStartUrl(string.Format(BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token={0}&dest={1}", arg, string.Format(Utils.GetString("RenewTeamUrl"), _notificationItem.teamId)));
							}
							break;
						}
						break;
					case 19:
						if (text == "no_admin_permission")
						{
							new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("NoAdminPermission"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog();
						}
						break;
					case 17:
						if (text == "team_exceed_limit")
						{
							new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("UserTeamCountLimit"), _notificationItem.UserDisplayName), Utils.GetString("GotIt"), "").ShowDialog();
						}
						break;
					case 21:
						if (text == "user_already_approved")
						{
							Utils.Toast(string.Format(Utils.GetString("OtherAdminAccepted"), _notificationItem.UserDisplayName, _notificationItem.teamName));
							_notificationItem.ButtonVisibility = "Collapsed";
							_notificationItem.ActionVisibility = "Visible";
							_notificationItem.ActionText = Utils.GetString("Accepted");
						}
						break;
					case 20:
						if (text == "user_already_refused")
						{
							Utils.Toast(string.Format(Utils.GetString("OtherAdminRefused"), _notificationItem.UserDisplayName, _notificationItem.teamName));
							_notificationItem.ButtonVisibility = "Collapsed";
							_notificationItem.ActionVisibility = "Visible";
							_notificationItem.ActionText = Utils.GetString("Declined");
						}
						break;
					case 13:
					case 14:
					case 15:
					case 16:
						break;
					}
				}
				else
				{
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Accepted");
					SyncManager.Sync();
				}
				return;
			}
			break;
		case 9:
		{
			string value = await Communicator.HandleShareApplication(_notificationItem.id, "accept");
			if (string.IsNullOrEmpty(value))
			{
				acceptShareReturn = true;
				break;
			}
			TeamErrorDataModel teamErrorDataModel = null;
			ErrorModel errorModel = JsonConvert.DeserializeObject<ErrorModel>(value);
			try
			{
				teamErrorDataModel = JsonConvert.DeserializeObject<TeamErrorDataModel>(errorModel.data.ToString());
			}
			catch
			{
			}
			if (errorModel == null)
			{
				break;
			}
			switch (errorModel.errorCode)
			{
			case "no_team_permission":
				new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("NoTeamPermission02"), teamErrorDataModel?.applyUserName ?? " ", teamErrorDataModel?.teamName ?? " "), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "team_project_exceed_quota":
			{
				if (errorModel.data != null && errorModel.data.TryGetValue("teamId", out JToken value2))
				{
					string teamId = value2.ToString();
					ProChecker.ShowUpgradeDialog(ProType.TeamShareLimit, App.Window, teamId);
					return;
				}
				break;
			}
			case "visitor_exceed_quota":
			{
				if (errorModel.data != null && errorModel.data.TryGetValue("teamId", out JToken value3) && errorModel.data.TryGetValue("limit", out JToken value4))
				{
					int result;
					int num = (int.TryParse(value4.ToString(), out result) ? result : 29);
					TeamModel teamById = CacheManager.GetTeamById(value3.ToString());
					if (num >= 29)
					{
						CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProLimitedSharingGuests"), num), MessageBoxButton.OK);
						customerDialog.Owner = App.Window;
						customerDialog.ShowDialog();
					}
					else
					{
						ProChecker.ShowUpgradeDialog(ProType.MoreTeamGuest, null, teamById?.id);
					}
					return;
				}
				break;
			}
			case "exceed_quota":
				new CustomerDialog(Utils.GetString("HandleDefeat"), Utils.GetString("InviteeProjectOverLimit"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			case "exceed_team_max_share_limit":
				if (UserDao.IsPro())
				{
					long userLimit = Utils.GetUserLimit(Constants.LimitKind.ShareUserNumber);
					string format = Utils.GetString("ProShareListLimit");
					new CustomerDialog(Utils.GetString("LimitTips"), string.Format(format, userLimit), MessageBoxButton.OK).ShowDialog();
				}
				else
				{
					ProChecker.ShowUpgradeDialog(ProType.MoreSharingMembers);
				}
				break;
			default:
				new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				break;
			}
			break;
		}
		}
		if (acceptShareReturn)
		{
			_notificationItem.ButtonVisibility = "Collapsed";
			_notificationItem.ActionVisibility = "Visible";
			_notificationItem.ActionText = Utils.GetString("Accepted");
			if (_notificationItem.actionStatus == 0)
			{
				await ListViewContainer.AcceptShareList(_notificationItem.projectid);
			}
			SyncManager.Sync();
		}
		_notificationItem.IsButtonEnable = "True";
	}

	private async void ShowAcceptFailureDialog()
	{
		if (new CustomerDialog(Utils.GetString("SendVerifyEmail"), Utils.GetString("SendVerifyEmailHint"), Utils.GetString("SendVerifyEmail"), Utils.GetString("Cancel")).ShowDialog() == true)
		{
			await Communicator.ResentVerifyEmail();
		}
	}

	private async void NotificationDeclineClick(object sender, RoutedEventArgs e)
	{
		if (_notificationItem == null)
		{
			return;
		}
		_notificationItem.IsButtonEnable = "False";
		bool acceptShareReturn = false;
		switch (_notificationItem.actionStatus)
		{
		case 21:
			if (_notificationItem.type == "team")
			{
				if (string.IsNullOrEmpty(await Communicator.HandleTeamInvite(_notificationItem.id, accept: false)))
				{
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Declined");
				}
				else
				{
					new CustomerDialog(Utils.GetString("OperateFailed"), Utils.GetString("OperateFailedMessage"), Utils.GetString("GotIt"), "").ShowDialog();
				}
			}
			break;
		case 1:
		{
			if (_notificationItem.type == "projectPermission")
			{
				if (string.IsNullOrEmpty(await Communicator.RefuseApplyPermission(_notificationItem.id)))
				{
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Declined");
				}
				break;
			}
			string value = await Communicator.RefuseTeamApply(_notificationItem.id);
			if (!string.IsNullOrEmpty(value))
			{
				switch (JsonConvert.DeserializeObject<ErrorModel>(value)?.errorCode)
				{
				case "no_admin_permission":
					new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("NoAdminPermission"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog();
					break;
				case "team_expired":
					if (new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("TeamExpired"), _notificationItem.UserDisplayName), Utils.GetString("GotIt"), "").ShowDialog() == true)
					{
						string arg = await Communicator.GetSignOnToken();
						Utils.TryProcessStartUrl(string.Format(BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token={0}&dest={1}", arg, string.Format(Utils.GetString("RenewTeamUrl"), _notificationItem.teamId)));
					}
					break;
				case "user_already_approved":
					Utils.Toast(string.Format(Utils.GetString("OtherAdminAccepted"), _notificationItem.UserDisplayName, _notificationItem.teamName));
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Accepted");
					break;
				case "user_already_refused":
					Utils.Toast(string.Format(Utils.GetString("OtherAdminRefused"), _notificationItem.UserDisplayName, _notificationItem.teamName));
					_notificationItem.ButtonVisibility = "Collapsed";
					_notificationItem.ActionVisibility = "Visible";
					_notificationItem.ActionText = Utils.GetString("Declined");
					break;
				case "user_not_exist":
					new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("UserNotExist"), _notificationItem.UserDisplayName), Utils.GetString("GotIt"), "").ShowDialog();
					break;
				case "no_team_permission":
					new CustomerDialog(Utils.GetString("HandleDefeat"), string.Format(Utils.GetString("TeamNotExist"), _notificationItem.teamName), Utils.GetString("GotIt"), "").ShowDialog();
					break;
				}
			}
			else
			{
				acceptShareReturn = true;
			}
			break;
		}
		case 0:
			acceptShareReturn = string.IsNullOrEmpty(await Communicator.SetAcceptShare(_notificationItem.projectid, _notificationItem.id, 2));
			break;
		case 9:
			acceptShareReturn = string.IsNullOrEmpty(await Communicator.HandleShareApplication(_notificationItem.id, "refuse"));
			break;
		}
		if (acceptShareReturn)
		{
			_notificationItem.ButtonVisibility = "Collapsed";
			_notificationItem.ActionVisibility = "Visible";
			_notificationItem.ActionText = Utils.GetString("Declined");
		}
		_notificationItem.IsButtonEnable = "True";
	}

	private async Task GetTitle(NotificationModel notification)
	{
		if (notification == null)
		{
			return;
		}
		bool flag = await ProjectDao.CheckIfNewShowReminder();
		switch (notification.type)
		{
		case "TeamPayReminder":
			if (notification.notificationUserData.expiryDate.HasValue)
			{
				bool flag2 = notification.notificationUserData.expiryDate?.Year == DateTime.Today.Year;
				DateTime value = notification.notificationUserData.expiryDate.Value;
				_notificationItem.Title = string.Format(Utils.GetString("TeamPayReminder"), notification.notificationUserData.teamName, value.ToString(flag2 ? "M" : "D", App.Ci));
			}
			break;
		case "share":
		{
			string text12 = null;
			switch (notification.actionStatus)
			{
			case 0:
			case 2:
			case 3:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationTitleShareYou"), notification.notificationUserData.entityName);
				break;
			case 1:
			{
				if (!flag)
				{
					text12 = Utils.GetString("NotificationTitleShareYou");
					break;
				}
				string title10 = Utils.GetString("ShareListYouGoToEdit");
				TitleTextBlock.Inlines.Clear();
				foreach (string runText in GetRunTextList(title10))
				{
					switch (runText)
					{
					case "{0}":
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.userDisplayName));
						break;
					case "{1}":
						TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.entityName, notification.notificationUserData.entityId));
						break;
					case "{2}":
						TitleTextBlock.Inlines.Add(EditProjectRun(Utils.GetString("EditList"), notification.notificationUserData.entityId));
						break;
					default:
						TitleTextBlock.Inlines.Add(new Run(runText));
						break;
					}
				}
				break;
			}
			case 4:
				text12 = Utils.GetString("NotificationTitleJoin");
				break;
			case 5:
				text12 = Utils.GetString("NotificationTitleDeclineYou");
				break;
			case 6:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationTitleDelete"), notification.notificationUserData.entityName);
				break;
			case 7:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationTitleRemoveYou"), notification.notificationUserData.entityName);
				break;
			case 8:
				text12 = Utils.GetString("NotificationTitleLeave");
				break;
			case 12:
			{
				if (!flag)
				{
					text12 = Utils.GetString("NotificationTitleAgree");
					break;
				}
				string title9 = Utils.GetString("AgreeShareListGoToEdit").Replace("{0} ", "");
				TitleTextBlock.Inlines.Clear();
				foreach (string runText2 in GetRunTextList(title9))
				{
					if (!(runText2 == "{1}"))
					{
						if (runText2 == "{2}")
						{
							TitleTextBlock.Inlines.Add(EditProjectRun(Utils.GetString("EditList"), notification.notificationUserData.entityId));
						}
						else
						{
							TitleTextBlock.Inlines.Add(new Run(runText2));
						}
					}
					else
					{
						TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.entityName, notification.notificationUserData.entityId));
					}
				}
				break;
			}
			case 13:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationTitleDisAgree"), notification.notificationUserData.entityName);
				break;
			case 14:
				text12 = Utils.GetString("TransferListNotification");
				break;
			case 9:
			case 10:
			case 11:
				text12 = Utils.GetString("NotificationTitleWant");
				break;
			case 15:
			{
				if (!flag)
				{
					text12 = Utils.GetString("NotificationForShare15");
					break;
				}
				string title8 = Utils.GetString("SharedAutoJoinListGoToEdit");
				TitleTextBlock.Inlines.Clear();
				foreach (string runText3 in GetRunTextList(title8))
				{
					if (!(runText3 == "{0}"))
					{
						if (runText3 == "{1}")
						{
							TitleTextBlock.Inlines.Add(EditProjectRun(Utils.GetString("EditList"), notification.notificationUserData.entityId));
						}
						else
						{
							TitleTextBlock.Inlines.Add(new Run(runText3));
						}
					}
					else
					{
						TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.entityName, notification.notificationUserData.entityId));
					}
				}
				break;
			}
			}
			if (text12 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText4 in GetRunTextList(text12))
			{
				TitleTextBlock.Inlines.Add((runText4 == "{0}") ? GetProjectRun(notification.notificationUserData.entityName, notification.notificationUserData.entityId) : new Run(runText4));
			}
			break;
		}
		case "comment":
		{
			string text11 = null;
			switch (notification.notificationUserData.action)
			{
			case "MENTION":
				text11 = Utils.GetString("NotificationCommentMention");
				break;
			case "REPLY":
				text11 = Utils.GetString("NotificationCommentReply");
				break;
			case "COMMENT":
				text11 = Utils.GetString("NotificationCommentAdd");
				break;
			}
			if (text11 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText5 in GetRunTextList(text11))
			{
				if (!(runText5 == "{0}"))
				{
					if (runText5 == "{1}")
					{
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.title));
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(runText5));
					}
				}
				else
				{
					TitleTextBlock.Inlines.Add(GetTaskRun(notification.notificationUserData.taskTitle, notification.notificationUserData.taskId));
				}
			}
			break;
		}
		case "assign":
		{
			string title4 = Utils.GetString("NotificationTitleAssign");
			List<string> runTextList2 = GetRunTextList(title4);
			TitleTextBlock.Inlines.Clear();
			foreach (string item in runTextList2)
			{
				if (!(item == "{0}"))
				{
					if (item == "{1}")
					{
						TitleTextBlock.Inlines.Add(GetTaskRun(notification.notificationUserData.taskTitle, notification.notificationUserData.taskId));
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(item));
					}
				}
				else
				{
					TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
				}
			}
			break;
		}
		case "PayReminder":
			if (!notification.notificationUserData.expiryDate.HasValue || notification.notificationUserData.expiryDate.Value.Year == 1)
			{
				string title3 = Utils.GetString("NotificationRemiderToUpgrade");
				TitleTextBlock.Inlines.Clear();
				foreach (string runText6 in GetRunTextList(title3))
				{
					TitleTextBlock.Inlines.Add((runText6 == "{0}") ? GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId) : new Run(runText6));
				}
			}
			else
			{
				DateTime? expiryDate = notification.notificationUserData.expiryDate;
				string arg = ticktick_WPF.Util.DateUtils.FormatMonthDay(expiryDate.Value, DateFormatLen.Long);
				if ((expiryDate.Value - notification.createdTime).TotalDays > 1.0)
				{
					_notificationItem.Title = string.Format(Utils.GetString("RenewPro7DayWarn"), Utils.GetAppName(), arg);
				}
				else
				{
					_notificationItem.Title = string.Format(Utils.GetString("RenewPro1DayWarn"), Utils.GetAppName(), arg);
				}
			}
			break;
		case "forumTopic":
			_notificationItem.Title = "The topic '" + notification.notificationUserData.topicTitle + "' has new reply.";
			_notificationItem.Url = "https://help.ticktick.com" + notification.notificationUserData.topicUrl;
			break;
		case "support":
		{
			string text7 = Utils.GetString("TicketNotificationTitle");
			if (text7 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText7 in GetRunTextList(text7))
			{
				TitleTextBlock.Inlines.Add((runText7 == "{0}") ? GetTicketRun(notification.notificationUserData.ticketDescription, notification.notificationUserData.ticketId) : new Run(runText7));
			}
			break;
		}
		case "score":
		{
			string text10 = Utils.GetString("TicketScoreTitle");
			if (text10 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText8 in GetRunTextList(text10))
			{
				TitleTextBlock.Inlines.Add((runText8 == "{0}") ? GetTicketRun(notification.notificationUserData.ticketDescription, notification.notificationUserData.ticketId) : new Run(runText8));
			}
			break;
		}
		case "attend":
		{
			string text8 = null;
			switch (notification.actionStatus)
			{
			case 1:
			case 2:
			case 3:
				text8 = Utils.GetString("SaveAgendaNotification");
				break;
			case 4:
			case 5:
			case 6:
				text8 = Utils.GetString("OwnerDeletedSharedAgenda");
				break;
			}
			if (text8 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText9 in GetRunTextList(text8))
			{
				if (!(runText9 == "{0}"))
				{
					if (runText9 == "{1}")
					{
						TitleTextBlock.Inlines.Add(GetTaskRun(notification.notificationUserData.title, notification.notificationUserData.entityId));
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(runText9));
					}
				}
				else
				{
					TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.userDisplayName));
				}
			}
			break;
		}
		case "team":
			switch (notification.actionStatus)
			{
			case 1:
			case 15:
			case 16:
			case 20:
				_notificationItem.Title = string.Format(Utils.GetString("TeamAttendApply"), notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 2:
				_notificationItem.Title = string.Format(Utils.GetString("TeamAttendAdminAccepted"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 3:
				_notificationItem.Title = string.Format(Utils.GetString("TeamAttendAdminRefused"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 4:
				_notificationItem.Title = string.Format(Utils.GetString("TeamAttendAccepted"), notification.notificationUserData.teamName);
				break;
			case 5:
				_notificationItem.Title = string.Format(Utils.GetString("TeamAttendRefused"), notification.notificationUserData.teamName);
				break;
			case 6:
				_notificationItem.Title = string.Format(Utils.GetString("TeamExit"), notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 7:
				_notificationItem.Title = string.Format(Utils.GetString("TeamProjectHandle"), notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName, notification.notificationUserData.projectCount);
				break;
			case 8:
				_notificationItem.Title = string.Format(Utils.GetString("RemovedByAdmin"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 9:
				_notificationItem.Title = string.Format(Utils.GetString("RemovedByAdmin"), notification.notificationUserData.adminDisplayName, Utils.GetString("You"), notification.notificationUserData.teamName);
				break;
			case 10:
				_notificationItem.Title = string.Format(Utils.GetString("TeamProjectHandle"), Utils.GetString("Admin"), notification.notificationUserData.teamName, notification.notificationUserData.projectCount);
				break;
			case 11:
				_notificationItem.Title = string.Format(Utils.GetString("BecomeAdmin"), notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 12:
				_notificationItem.Title = string.Format(Utils.GetString("BecomeNormal"), notification.notificationUserData.userDisplayName, notification.notificationUserData.teamName);
				break;
			case 13:
			case 14:
				_notificationItem.Title = string.Format(Utils.GetString("BecomeSuper"), notification.notificationUserData.teamName);
				break;
			case 17:
				_notificationItem.Title = string.Format(Utils.GetString("TeamDeleted"), notification.notificationUserData.teamName);
				break;
			case 21:
			case 22:
			case 23:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationForTeam21"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.teamName);
				break;
			case 24:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationForTeam24"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.teamName);
				break;
			case 25:
				_notificationItem.Title = string.Format(Utils.GetString("NotificationForTeam25"), notification.notificationUserData.adminDisplayName, notification.notificationUserData.teamName);
				break;
			}
			break;
		case "task":
		{
			string text5 = null;
			switch (notification.actionStatus)
			{
			case 1:
				text5 = Utils.GetString("TeamAddTask");
				break;
			case 2:
				text5 = Utils.GetString("TeamDoTask");
				break;
			case 3:
				text5 = Utils.GetString("TeamUnDoTask");
				break;
			case 4:
				text5 = Utils.GetString("TeamDelTask");
				break;
			case 5:
				text5 = Utils.GetString("TeamMoveTask");
				break;
			case 6:
				text5 = Utils.GetString("TeamAbandonedTask");
				break;
			case 7:
				text5 = Utils.GetString("TeamReopenTask");
				break;
			}
			if (text5 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText10 in GetRunTextList(text5))
			{
				switch (runText10)
				{
				case "{1}":
					TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
					continue;
				case "{2}":
					TitleTextBlock.Inlines.Add(GetTaskRun(notification.notificationUserData.title, notification.notificationUserData.taskId));
					continue;
				case "{0}":
					continue;
				}
				if (!string.IsNullOrEmpty(runText10))
				{
					string text6 = runText10;
					if (TitleTextBlock.Inlines.Count == 0)
					{
						text6 = runText10.TrimStart();
					}
					TitleTextBlock.Inlines.Add(new Run(text6));
				}
			}
			break;
		}
		case "projectPermission":
		{
			string text3 = null;
			string text4 = null;
			switch (notification.actionStatus)
			{
			case 1:
			case 4:
			case 5:
				text3 = Utils.GetString("ApplyPermission");
				text4 = notification.notificationUserData.applyUserDisplayName;
				break;
			case 2:
				text3 = Utils.GetString("ApplyPermissionAccepted");
				text4 = notification.notificationUserData.ownerUserDisplayName;
				break;
			case 3:
				text3 = Utils.GetString("ApplyPermissionRefused");
				text4 = notification.notificationUserData.ownerUserDisplayName;
				break;
			}
			if (text3 == null)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			foreach (string runText11 in GetRunTextList(text3))
			{
				if (!(runText11 == "{0}"))
				{
					if (runText11 == "{1}")
					{
						TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(runText11));
					}
				}
				else
				{
					TitleTextBlock.Inlines.Add(new Run(text4));
				}
			}
			break;
		}
		case "project":
			switch (notification.actionStatus)
			{
			case 1:
			{
				string title7 = Utils.GetString("NotificationForProject1");
				foreach (string runText12 in GetRunTextList(title7))
				{
					switch (runText12)
					{
					case "{0}":
						TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
						break;
					case "{1}":
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.teamName));
						break;
					case "{2}":
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.userDisplayName));
						break;
					default:
						TitleTextBlock.Inlines.Add(new Run(runText12));
						break;
					}
				}
				break;
			}
			case 2:
			{
				string title6 = Utils.GetString("AArchivedListB");
				foreach (string runText13 in GetRunTextList(title6))
				{
					if (!(runText13 == "{0}"))
					{
						if (runText13 == "{1}")
						{
							TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
						}
						else
						{
							TitleTextBlock.Inlines.Add(new Run(runText13));
						}
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.userDisplayName));
					}
				}
				break;
			}
			case 3:
			{
				string title5 = Utils.GetString("AUnarchivedListB");
				foreach (string runText14 in GetRunTextList(title5))
				{
					if (!(runText14 == "{0}"))
					{
						if (runText14 == "{1}")
						{
							TitleTextBlock.Inlines.Add(GetProjectRun(notification.notificationUserData.projectName, notification.notificationUserData.projectId));
						}
						else
						{
							TitleTextBlock.Inlines.Add(new Run(runText14));
						}
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(notification.notificationUserData.userDisplayName));
					}
				}
				break;
			}
			}
			break;
		case "gracePeriod":
		{
			if (notification.actionStatus != 0)
			{
				break;
			}
			TitleTextBlock.Inlines.Clear();
			TitleTextBlock.Inlines.Add(new Run(string.Format(Utils.GetString("DearSomeOne"), notification.notificationUserData.userDisplayName) + ((Utils.IsCn() || Utils.IsJp()) ? "" : " ")));
			string text9 = Utils.GetString("SubscriptionGraceDateMessage");
			if (!string.IsNullOrEmpty(text9))
			{
				text9 = text9.Substring(0, 1).ToLower() + text9.Substring(1);
			}
			List<string> runTextList = GetRunTextList(text9);
			int val = 0;
			if (notification.notificationUserData.noGraceDate.HasValue)
			{
				val = (int)(notification.createdTime - notification.notificationUserData.noGraceDate.Value.Date).TotalDays;
			}
			string type = ((notification.notificationUserData.subscribeType == "apple") ? "Apple" : "Google");
			foreach (string item2 in runTextList)
			{
				if (!(item2 == "{0}"))
				{
					if (item2 == "{1}")
					{
						TitleTextBlock.Inlines.Add(new Run(Math.Max(1, val).ToString() ?? ""));
					}
					else
					{
						TitleTextBlock.Inlines.Add(new Run(item2));
					}
				}
				else
				{
					TitleTextBlock.Inlines.Add(new Run(type));
				}
			}
			Run run = new Run(((Utils.IsCn() || Utils.IsJp()) ? "" : " ") + Utils.GetString("LearnMore"))
			{
				Foreground = ThemeUtil.GetColor("PrimaryColor"),
				Cursor = Cursors.Hand
			};
			run.MouseLeftButtonUp += delegate
			{
				Utils.GotoPaySubscriptionHelp(type == "Apple");
			};
			TitleTextBlock.Inlines.Add(run);
			break;
		}
		case "subscribeFail":
		{
			string text = notification?.notificationUserData?.subscribeType;
			string subscribeTypeText = PremiumUtils.GetSubscribeTypeText(text);
			subscribeTypeText = (string.IsNullOrEmpty(subscribeTypeText) ? "PayPal" : subscribeTypeText);
			switch (text)
			{
			case "paypal_subscribe":
			case "google":
			case "stripe_subscribe":
			{
				string text2 = string.Empty;
				switch (text)
				{
				case "paypal_subscribe":
					text2 = "https://www.paypal.com";
					break;
				case "google":
					text2 = "https://myaccount.google.com";
					break;
				case "stripe_subscribe":
					text2 = "https://stripe.com";
					break;
				}
				if (string.IsNullOrEmpty(text2))
				{
					_notificationItem.Title = string.Format(Utils.GetString("PaymentFailedContent"), subscribeTypeText);
					break;
				}
				string title2 = Utils.GetString("PaymentFailedContent");
				TitleTextBlock.Inlines.Clear();
				foreach (string runText15 in GetRunTextList(title2))
				{
					TitleTextBlock.Inlines.Add((runText15 == "{0}") ? GetLinkRun(subscribeTypeText, text2) : new Run(runText15));
				}
				break;
			}
			default:
				_notificationItem.Title = string.Format(Utils.GetString("PaymentFailedContent"), subscribeTypeText);
				break;
			}
			break;
		}
		case "subscribeDuplicate":
		{
			string title = Utils.GetString("DuplicateSubscriptionContent");
			TitleTextBlock.Inlines.Clear();
			foreach (string runText16 in GetRunTextList(title))
			{
				TitleTextBlock.Inlines.Add((runText16 == "{0}") ? GetManageSubsRun() : new Run(runText16));
			}
			break;
		}
		}
		base.DataContext = _notificationItem;
		if (_notificationItem.Title != null && TitleTextBlock.Inlines.Count == 0)
		{
			TitleTextBlock.Text = _notificationItem.Title;
		}
	}

	private Run GetTaskRun(string text, string id)
	{
		if (text.Length > 400)
		{
			text = text.Substring(0, 400) + "...";
		}
		Run run = new Run(text);
		run.Background = new SolidColorBrush(Colors.Transparent);
		run.Foreground = ThemeUtil.GetColor("PrimaryColor");
		run.Cursor = Cursors.Hand;
		run.MouseLeftButtonUp += delegate
		{
			App.Window.NavigateTask(id, string.Empty);
			App.Window.LeftTabBar.NotificationPopup.IsOpen = false;
		};
		return run;
	}

	private Run EditProjectRun(string text, string id)
	{
		Run run;
		if (CacheManager.GetProjects().Exists((ProjectModel p) => p.id == id))
		{
			run = new Run(text)
			{
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = ThemeUtil.GetColor("PrimaryColor"),
				Cursor = Cursors.Hand
			};
			run.MouseLeftButtonUp += delegate
			{
				App.Window.EditProject(id);
				App.Window.LeftTabBar.NotificationPopup.IsOpen = false;
			};
		}
		else
		{
			run = new Run(text);
		}
		return run;
	}

	private Run GetProjectRun(string text, string id)
	{
		Run run;
		if (CacheManager.GetProjects().Exists((ProjectModel p) => p.id == id))
		{
			run = new Run(text)
			{
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = ThemeUtil.GetColor("PrimaryColor"),
				Cursor = Cursors.Hand
			};
			run.MouseLeftButtonUp += delegate
			{
				App.Window.NavigateNormalProject(id);
				App.Window.LeftTabBar.NotificationPopup.IsOpen = false;
			};
		}
		else
		{
			run = new Run(text);
		}
		return run;
	}

	private Run GetTicketRun(string text, string id)
	{
		Run run = new Run(text);
		run.Background = new SolidColorBrush(Colors.Transparent);
		run.Foreground = ThemeUtil.GetColor("PrimaryColor");
		run.Cursor = Cursors.Hand;
		run.MouseLeftButtonUp += async delegate
		{
			App.Window.LeftTabBar.NotificationPopup.IsOpen = false;
			if (EmbeddedBrowserFactory.CanOpenEmbeddedBrowser(Environment.OSVersion.Version, InternetExplorerBrowserEmulation.IsVersion11()))
			{
				try
				{
					int dotNetReleaseKey = Utils.GetDotNetReleaseKey();
					UtilLog.Info("db size : " + new FileInfo(AppPaths.TickTickDbPath).Length);
					UtilLog.Info("dotnet releaseKey : " + dotNetReleaseKey);
					UtilLog.Info("taskCount : " + TaskCache.LocalTaskViewModels.Count);
				}
				catch (Exception)
				{
				}
				NavigateWebBrowserWindow navigateWebBrowserWindow = new NavigateWebBrowserWindow("/v3/tickets/detail?id=" + id);
				navigateWebBrowserWindow.Show();
				navigateWebBrowserWindow.Topmost = true;
			}
			else
			{
				JumpWebFeedBack(id);
			}
		};
		return run;
	}

	private Run GetLinkRun(string text, string url)
	{
		Run run = new Run(text);
		run.Background = new SolidColorBrush(Colors.Transparent);
		run.Foreground = ThemeUtil.GetColor("PrimaryColor");
		run.Cursor = Cursors.Hand;
		run.MouseLeftButtonUp += async delegate
		{
			Utils.TryProcessStartUrl(url);
		};
		return run;
	}

	private Run GetManageSubsRun()
	{
		Run run = new Run(Utils.GetString("ManageSubscriptions"));
		run.Background = new SolidColorBrush(Colors.Transparent);
		run.Foreground = ThemeUtil.GetColor("PrimaryColor");
		run.Cursor = Cursors.Hand;
		run.MouseLeftButtonUp += async delegate
		{
			UserModel userModel = await UserManager.GetCurrentUser();
			if (userModel != null && userModel.subscribeType != "order")
			{
				ManageProSubscribeWindow manageProSubscribeWindow = new ManageProSubscribeWindow(await Communicator.GetSubscribeList());
				manageProSubscribeWindow.Owner = Window.GetWindow(this);
				manageProSubscribeWindow.ShowDialog();
			}
			else
			{
				Utils.TryProcessStartUrlWithToken("webapp/#settings/premium");
			}
		};
		return run;
	}

	private async void JumpWebFeedBack(string id)
	{
		Utils.TryProcessStartUrlWithToken("/v3/tickets/detail?id=" + id);
	}

	private List<string> GetRunTextList(string title)
	{
		List<string> list = new List<string>();
		if (title.Contains("{"))
		{
			int num = title.IndexOf("{", StringComparison.Ordinal);
			list.Add(title.Substring(0, num));
			list.Add(title.Substring(num, 3));
			list.AddRange(GetRunTextList(title.Substring(num + 3, title.Length - num - 3)));
		}
		else
		{
			list.Add(title);
		}
		return list;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/notificationlistitem.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((NotificationListItem)target).Loaded += OnLoaded;
			break;
		case 2:
			TitleTextBlock = (TextBlock)target;
			TitleTextBlock.MouseLeftButtonUp += OnTitleClick;
			break;
		case 3:
			((Button)target).Click += OnNotificationAcceptClick;
			break;
		case 4:
			((Button)target).Click += NotificationDeclineClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
