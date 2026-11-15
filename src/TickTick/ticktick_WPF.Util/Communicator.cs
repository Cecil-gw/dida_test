using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices.Models;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public static class Communicator
{
	public static async Task<UserModel> SignOn(string username, string password, string verKey = null, string verCode = null)
	{
		JObject jObject = new JObject();
		bool flag = Utils.IsDida() && Utils.IsMobilePhone(username);
		jObject.Add(flag ? "phone" : "username", username);
		jObject.Add("password", password);
		if (!string.IsNullOrEmpty(verKey))
		{
			jObject.Add("verKey", verKey);
			jObject.Add("verCode", verCode);
		}
		return await ApiClient.PostJsonAsync<UserModel>(BaseUrl.GetUrl("/api/v2/user/signon"), jObject);
	}

	public static async Task<bool> IsNewUser()
	{
		try
		{
			return await ApiClient.GetJsonAsync<bool>(BaseUrl.GetUrl("/api/v2/user/isJustRegistered"));
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static async Task<int> IsUserNewState()
	{
		try
		{
			return await ApiClient.GetJsonAsync<int>(BaseUrl.GetUrl("/api/v3/subscribe/free_trial/user"));
		}
		catch (Exception)
		{
		}
		return 2;
	}

	public static async Task SignOut()
	{
		try
		{
			await ApiClient.GetJsonAsync<string>(BaseUrl.GetUrl("/api/v2/user/signout"));
		}
		catch (Exception)
		{
		}
	}

	public static async Task<UserModel> GetUserStatus()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/status", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			UserModel userModel = JsonConvert.DeserializeObject<UserModel>(text);
			if (userModel != null && userModel.userId != null && userModel.userId != "")
			{
				userModel.token = LocalSettings.Settings.LoginUserAuth;
				return userModel;
			}
			return new UserModel
			{
				username = text
			};
		}
		catch (Exception)
		{
			return new UserModel
			{
				username = text
			};
		}
	}

	public static async Task<string> GetInviteCode()
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/signup/inviteCode", "", null, null, "GET");
	}

	public static async Task<UserModel> Signup(string name, string username, string password, string verifyText, bool isPhone, string verKey = null, string verCode = null)
	{
		JObject jObject = new JObject();
		jObject.Add(isPhone ? "phone" : "username", username);
		jObject.Add("password", password);
		if (!string.IsNullOrEmpty(verKey))
		{
			jObject.Add("verKey", verKey);
			jObject.Add("verCode", verCode);
		}
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/signup" + (isPhone ? ("/?code=" + verifyText) : ""), JsonConvert.SerializeObject(jObject), null, null, "POST", fulluri: false, isNeedErrorReturn: true);
		try
		{
			UserModel userModel = JsonConvert.DeserializeObject<UserModel>(text);
			if (userModel != null && userModel.userId != null && userModel.userId != "")
			{
				return userModel;
			}
			return new UserModel
			{
				username = text
			};
		}
		catch (Exception)
		{
			return new UserModel
			{
				username = text
			};
		}
	}

	public static async Task<UserInfoModel> GetUserInfo()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/profile", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<UserInfoModel>(text);
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
			return new UserInfoModel();
		}
	}

	public static async Task<(string, List<TaskModel>)> BatchCheckTrash(long? next, int limit, int type = 1)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/all/trash/page?next={next}&limit={limit}&type={type}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (string.IsNullOrEmpty(text))
		{
			return (null, null);
		}
		_ = string.Empty;
		_ = string.Empty;
		string value;
		string item;
		try
		{
			value = JObject.Parse(text)["tasks"].ToString();
			item = JObject.Parse(text)["next"].ToString();
		}
		catch (Exception ex)
		{
			UtilLog.Info(ex.Message);
			return (null, null);
		}
		try
		{
			List<TaskModel> item2 = JsonConvert.DeserializeObject<List<TaskModel>>(value);
			return (item, item2);
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ex2.Message);
			return (null, null);
		}
	}

	public static async Task<SyncBean> BatchCheck(long point, string lastVersion)
	{
		BatchCheckCollectModel collectModel = new BatchCheckCollectModel
		{
			point = point,
			time = DateTime.Now
		};
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v3/batch/check/" + point + "?lastVersion=" + lastVersion, "", null, LocalSettings.Settings.LoginUserAuth, "GET", fulluri: false, isNeedErrorReturn: true);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		collectModel.endTime = DateTime.Now;
		try
		{
			ErrorModel errorModel = JsonConvert.DeserializeObject<ErrorModel>(text);
			if (errorModel != null && errorModel.errorId != null)
			{
				collectModel.errorId = errorModel.errorId;
				return null;
			}
		}
		catch (Exception)
		{
		}
		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
		List<string> list = new List<string>();
		JObject jObject = null;
		try
		{
			jObject = JObject.Parse(text);
		}
		catch (Exception)
		{
		}
		string checks = null;
		try
		{
			JToken jToken = null;
			JToken jToken2 = null;
			if (jObject != null)
			{
				if (jObject.TryGetValue("syncTaskBean", out JToken value))
				{
					JObject jObject2 = JObject.Parse(value.ToString());
					if (jObject2.TryGetValue("deletedForever", out JToken value2))
					{
						jToken = value2;
					}
					if (jObject2.TryGetValue("deletedInTrash", out JToken value3))
					{
						jToken2 = value3;
					}
				}
				if (jObject.TryGetValue("checks", out JToken value4))
				{
					checks = value4.ToString();
				}
			}
			if (jToken == null)
			{
				list.Add("deletedForever");
			}
			if (jToken2 == null)
			{
				list.Add("deletedInTrash");
			}
		}
		catch (JsonReaderException)
		{
		}
		jsonSerializerSettings.ContractResolver = new SyncTaskBeanLimitPropsContractResolver(list.ToArray(), retain: false);
		try
		{
			SyncBean syncBean = JsonConvert.DeserializeObject<SyncBean>(text, jsonSerializerSettings);
			syncBean = TaskService.HandleTaskBeforeSave(syncBean);
			if (syncBean != null)
			{
				syncBean.checks = checks;
				collectModel.checkPoint = syncBean.checkPoint;
				collectModel.sync = true;
			}
			return syncBean;
		}
		catch (Exception ex4)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex4);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
		}
		return null;
	}

	public static async Task<bool> DeleteProjectGroup(string groupId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/projectGroup/" + groupId, "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
		return true;
	}

	public static async Task<string> UpdatePutProject(ProjectModel project)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + project.id, JsonConvert.SerializeObject(project), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<string> UpdateProject(string projectId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId, "", null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<ObservableCollection<TaskModel>> PullServerTasksByProjectId(string id)
	{
		string text = (string.IsNullOrEmpty(id) ? (await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/tasks", "", null, LocalSettings.Settings.LoginUserAuth, "GET")) : (await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + id + "/tasks", "", null, LocalSettings.Settings.LoginUserAuth, "GET")));
		try
		{
			ObservableCollection<TaskModel> observableCollection = JsonConvert.DeserializeObject<ObservableCollection<TaskModel>>(text);
			UtilLog.Info($"PullServerTasksByProjectId count:  {observableCollection?.Count}");
			return observableCollection;
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
		}
		return null;
	}

	public static async Task<BatchUpdateResult> BatchUpdateProjectGroup(SyncProjectGroupBean syncBean)
	{
		return JsonConvert.DeserializeObject<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/projectGroup", JsonConvert.SerializeObject(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchTaskOrderUpdateResult> BatchUpdateTaskOrder(SyncTaskOrderBean taskOrderBean)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/taskOrder", JsonConvert.SerializeObject(taskOrderBean), null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			return JsonConvert.DeserializeObject<BatchTaskOrderUpdateResult>(text);
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
			return null;
		}
	}

	public static async Task<BatchSyncOrderUpdateResult> BatchUpdateSyncOrder(SyncOrderBean syncOrderBean)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/order", JsonConvert.SerializeObject(syncOrderBean), null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			return JsonConvert.DeserializeObject<BatchSyncOrderUpdateResult>(text);
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
			return null;
		}
	}

	public static async Task<BatchSyncOrderUpdateResult> BatchUpdateTaskSyncOrder(SyncOrderBean syncOrderBean)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v3/batch/order", JsonConvert.SerializeObject(syncOrderBean), null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			return JsonConvert.DeserializeObject<BatchSyncOrderUpdateResult>(text);
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
			return null;
		}
	}

	public static async Task<ShareAddUserMode> AddShareUser(string projectId, string toUsername, Constants.ProjectPermission permission)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/share", "{\"toUsername\": \"" + toUsername + "\"" + ((permission == Constants.ProjectPermission.person) ? "}" : (",\"permission\":\"" + permission.ToString() + "\"}")), null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true);
		try
		{
			ShareAddUserMode shareAddUserMode = JsonConvert.DeserializeObject<ShareAddUserMode>(text);
			if (shareAddUserMode != null && shareAddUserMode.toUsername != null && shareAddUserMode.toUsername != "")
			{
				return shareAddUserMode;
			}
			return new ShareAddUserMode
			{
				fromUsername = text
			};
		}
		catch (Exception)
		{
			return new ShareAddUserMode
			{
				fromUsername = text
			};
		}
	}

	public static async Task<ShareAddUsersMode> AddShareUsers(string projectId, List<ShareInviteTarget> targets, Constants.ProjectPermission permission)
	{
		List<ShareInviteRequest> value = (from target in targets?.Where((ShareInviteTarget target) => target != null)
			select ShareInviteRequest.Create(target, permission)).ToList() ?? new List<ShareInviteRequest>();
		string value2 = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/shares", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true);
		try
		{
			return JsonConvert.DeserializeObject<ShareAddUsersMode>(value2);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<bool?> CheckSharePhone(string phone)
	{
		string text = Utils.NormalizeMainlandPhone(phone);
		if (!Utils.IsDida() || string.IsNullOrEmpty(text))
		{
			return false;
		}
		try
		{
			return await ApiClient.GetJsonAsync<bool>(BaseUrl.GetUrl("/api/v2/phone/check?phone=" + Uri.EscapeDataString(text)));
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex));
			return null;
		}
	}

	public static async Task<string> DeleteShareUser(string projectId, string recordId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/share/" + recordId, "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<string> GetShareLink(string projectId, bool isInviteUrl = false, Constants.ProjectPermission permission = Constants.ProjectPermission.person)
	{
		string result;
		if (isInviteUrl)
		{
			result = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/collaboration/invite-url", "", null, LocalSettings.Settings.LoginUserAuth, "GET", fulluri: false, isNeedErrorReturn: true);
		}
		else
		{
			string text = ((permission == Constants.ProjectPermission.person) ? null : permission.ToString());
			result = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/collaboration/invite" + (string.IsNullOrEmpty(text) ? "" : ("?permission=" + text)), "", null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true);
		}
		return result;
	}

	public static async Task<string> DeleteShareLink(string projectId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/collaboration/invite", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<string> GetShareUserList(string projectId, bool openToTeam)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + (openToTeam ? "/users" : "/shares"), "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<string> GetRecentProjectUsersList()
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/share/recentProjectUsers", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<List<ShareContactsModel>> GetShareContactsList()
	{
		string json = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/share/shareContacts", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<ShareContactsModel>>(JObject.Parse(json)["contacts"].ToString());
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<bool> DeleteShareContact(string email, string phone = null)
	{
		string text = BuildShareContactDeleteQuery(email, phone);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		return await ApiClient.DeleteAsync(BaseUrl.GetApiDomain() + "/api/v2/share/shareContacts?" + text);
	}

	public static string BuildShareContactDeleteQuery(string email, string phone = null)
	{
		List<string> list = new List<string>();
		string text = (Utils.IsDida() ? Utils.NormalizeMainlandPhone(email) : string.Empty);
		if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrEmpty(text))
		{
			list.Add("toEmail=" + Uri.EscapeDataString(email));
		}
		string text2 = (Utils.IsDida() ? Utils.NormalizeMainlandPhone(phone) : string.Empty);
		if (string.IsNullOrEmpty(text2))
		{
			text2 = text;
		}
		if (!string.IsNullOrEmpty(text2))
		{
			list.Add("toPhone=" + Uri.EscapeDataString(text2));
		}
		return string.Join("&", list);
	}

	public static async Task<string> GetAllShareContacts()
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/share/contacts", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<List<TeamContactsModel>> GetTeamContactsList(string teamId)
	{
		string json = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/team/{teamId}/share/shareContacts", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<TeamContactsModel>>(JObject.Parse(json)["contacts"].ToString());
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<UserPublicProfilesModel> GetUserPublicProfiles(string userCode)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/pub/api/v2/userPublicProfiles", "[\"" + userCode + "\"]", null, LocalSettings.Settings.LoginUserAuth);
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				return JsonConvert.DeserializeObject<UserPublicProfilesModel>(text.Substring(1, text.Length - 2));
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static async Task<List<UserPublicProfilesModel>> GetUsersPublicProfiles(List<string> userCodes)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[");
		for (int i = 0; i < userCodes.Count; i++)
		{
			stringBuilder.Append("\"" + userCodes[i] + "\"");
			if (i < userCodes.Count - 1)
			{
				stringBuilder.Append(",");
			}
		}
		stringBuilder.Append("]");
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/pub/api/v2/userPublicProfiles", stringBuilder.ToString(), null, LocalSettings.Settings.LoginUserAuth);
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<UserPublicProfilesModel>>(value);
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static async Task<string> GetNotificationList()
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/notification?autoMarkRead=false", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<string> SetAcceptShare(string projectId, string id, int actionStatus)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectId}/share/accept/{id}?actionStatus={actionStatus}", "", null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<NotificationUnreadCount> GetNotificationCount()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/notification/unread", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<NotificationUnreadCount>(value) ?? new NotificationUnreadCount();
		}
		catch (Exception)
		{
			return new NotificationUnreadCount();
		}
	}

	public static async Task<string> GetTencentOpenId(string accessToken)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"https://graph.qq.com/oauth2.0/me?access_token={accessToken}", "", null, null, "GET", fulluri: true);
		try
		{
			return JObject.Parse(text.Substring(text.IndexOf("{", StringComparison.Ordinal), text.IndexOf("}", StringComparison.Ordinal) - text.IndexOf("{", StringComparison.Ordinal) + 1))["openid"].ToString();
		}
		catch (Exception)
		{
			return "";
		}
	}

	public static async Task<string> ThirdChangePassword(string code, string password1, string password2, string userAuth)
	{
		JObject jObject = new JObject();
		jObject.Add("code", code);
		jObject.Add("newPassword1", password1);
		jObject.Add("newPassword2", password2);
		try
		{
			return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/third/changePassword", jObject.ToString(), null, userAuth, "POST", fulluri: false, isNeedErrorReturn: true);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<UserModel> ThirdSignon(string mode = "", string code = "", string site = "", string accessToken = "", string uId = "", string accessTokenSecret = "")
	{
		string text = "";
		switch (mode)
		{
		case "login_Google":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/OAuth2?site={site}&accessToken={accessToken}&uId={uId}", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		case "login_Facebook":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/facebook/validate?access_token={accessToken}", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		case "login_Twitter":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/twitter?accessToken={accessToken}&accessTokenSecret={accessTokenSecret}", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		case "login_Weibo":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/weibo/validate?code={code}", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		case "login_QQ":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/OAuth2?site={site}&accessToken={accessToken}&uId={uId}", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		case "login_Wechat":
			text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/user/sign/wechat/validate?code={code}&state=", "", null, null, "GET", fulluri: false, isNeedErrorReturn: true);
			break;
		}
		try
		{
			UserModel userModel = JsonConvert.DeserializeObject<UserModel>(text);
			if ((userModel != null && userModel.userId != null && userModel.userId != "") || !string.IsNullOrEmpty(userModel?.authId))
			{
				return userModel;
			}
			return new UserModel
			{
				username = text
			};
		}
		catch (Exception)
		{
			return new UserModel
			{
				username = text
			};
		}
	}

	public static async Task<ObservableCollection<LimitsModel>> GetLimits()
	{
		string json = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/configs/limits", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		ObservableCollection<LimitsModel> observableCollection = new ObservableCollection<LimitsModel>();
		try
		{
			JObject jObject = JObject.Parse(json);
			LimitsModel limitsModel = JsonConvert.DeserializeObject<LimitsModel>(jObject["free"].ToString());
			limitsModel.type = "FREE";
			LimitsModel limitsModel2 = JsonConvert.DeserializeObject<LimitsModel>(jObject["pro"].ToString());
			limitsModel2.type = "PRO";
			LimitsModel limitsModel3 = JsonConvert.DeserializeObject<LimitsModel>(jObject["team"].ToString());
			limitsModel3.type = "TEAM";
			observableCollection.Add(limitsModel);
			observableCollection.Add(limitsModel2);
			observableCollection.Add(limitsModel3);
		}
		catch (Exception)
		{
		}
		return observableCollection;
	}

	public static async Task<BatchUpdateResult> BatchUpdateProject(SyncProjectBean syncBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/project", JsonConvert.SerializeObject(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchUpdateResult> BatchUpdateFilter(SyncFilterBean syncBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/filter", JsonConvert.SerializeObject(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<List<MatrixProfileSyncModel>> GetMatrixProfiles()
	{
		return SafeConvertResult<List<MatrixProfileSyncModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/matrixProfiles", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
	}

	public static async Task<BatchUpdateResult> BatchUpdateMatrixProfiles(ModelSyncBean<MatrixProfileSyncModel> syncBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/matrixProfiles/batch", JsonConvert.SerializeObject(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchUpdateResult> BatchUpdateTag(SyncTagBean syncBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/tag", JsonConvert.SerializeObject(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchUpdateResult> BatchUpdateTaskProject(List<MoveOrRestoreProject> data)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/taskProject", JsonConvert.SerializeObject(data), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchUpdateResult> BatchRestoreTask(List<MoveOrRestoreProject> data)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/trash/restore", JsonConvert.SerializeObject(data), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<BatchUpdateResult> BatchUpdateTask(SyncTaskBean syncBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/task", SerializeTaskUpdate(syncBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	private static string SerializeTaskUpdate(SyncTaskBean syncBean)
	{
		JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
		jsonSerializerSettings.ContractResolver = new SyncTaskBeanLimitPropsContractResolver(new string[1] { "notionBlock" }, retain: false);
		JsonSerializerSettings settings = jsonSerializerSettings;
		return JsonConvert.SerializeObject(syncBean, settings);
	}

	public static async Task<BatchUpdateResult> BatchDeleteTask(List<TaskProject> deleteFromTrash)
	{
		string result = ((!App.Instance.TimeZoneChangeHandling) ? (await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/task?deleteforever=true", JsonConvert.SerializeObject(deleteFromTrash), null, LocalSettings.Settings.LoginUserAuth, "DELETE")) : null);
		return SafeConvertResult<BatchUpdateResult>(result);
	}

	public static async Task<bool> DeletePomo(string pomoId, bool isPomo)
	{
		if (string.IsNullOrEmpty(pomoId))
		{
			return false;
		}
		return string.IsNullOrEmpty(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/pomodoro" + (isPomo ? "" : "/timing") + "/" + pomoId, string.Empty, null, LocalSettings.Settings.LoginUserAuth, "DELETE", fulluri: false, isNeedErrorReturn: true));
	}

	public static async Task<BatchUpdateResult> BatchUpdatePomos(BaseSyncBean<PomodoroModel> pomos, bool isPomo)
	{
		if (pomos == null || !pomos.Any())
		{
			return null;
		}
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/pomodoro" + (isPomo ? "" : "/timing"), JsonConvert.SerializeObject(pomos), null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			string text2 = "add:";
			foreach (PomodoroModel item in pomos.Add)
			{
				text2 = text2 + item.Id + $"_{item.StartTime}_{item.EndTime},";
			}
			text2 += " update:";
			foreach (PomodoroModel item2 in pomos.Update)
			{
				text2 = text2 + item2.Id + $"_{item2.StartTime}_{item2.EndTime},";
			}
			text2 = text2 + " isPomo:" + isPomo;
			UtilLog.Info("BatchUpdatePomos: " + text2 + "\r\n--" + text);
		}
		catch (Exception)
		{
		}
		return SafeConvertResult<BatchUpdateResult>(text);
	}

	private static List<EventUpdateResult> SafeConvertCalendarResult(string result)
	{
		if (!string.IsNullOrEmpty(result))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<EventUpdateResult>>(result);
			}
			catch (Exception)
			{
				return new List<EventUpdateResult>();
			}
		}
		return new List<EventUpdateResult>();
	}

	private static T SafeConvertResult<T>(string result)
	{
		if (!string.IsNullOrEmpty(result))
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(result);
			}
			catch (Exception ex)
			{
				string text = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text = text + "\r\n" + result;
				}
				UtilLog.Warn(text);
				return default(T);
			}
		}
		return default(T);
	}

	public static async Task<string> GetSignOnToken()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/requestSignOnToken", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(text))
		{
			return (text.IndexOf("token", StringComparison.Ordinal) != -1) ? JObject.Parse(text)["token"].ToString() : "";
		}
		return LocalSettings.Settings.LoginUserAuth;
	}

	public static async Task<List<TaskModel>> GetClosedTasks(string prjId, DateTime? from, DateTime? to, int limit)
	{
		if (to.HasValue)
		{
			string text = ((!from.HasValue) ? "" : from.Value.ToUniversalTime().ToString("yyyy-MM-dd HH':'mm':'ss", CultureInfo.InvariantCulture));
			string text2 = to.Value.ToUniversalTime().ToString("yyyy-MM-dd HH':'mm':'ss", CultureInfo.InvariantCulture);
			string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{prjId}/closed/?from={text}&to={text2}&limit={limit}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
			if (!string.IsNullOrEmpty(value))
			{
				try
				{
					List<TaskModel> list = JsonConvert.DeserializeObject<List<TaskModel>>(value);
					if (list != null && list.Count > 0)
					{
						foreach (TaskModel item in list)
						{
							TaskService.ClearDueDate(item);
							if (string.IsNullOrEmpty(item.userId))
							{
								item.userId = LocalSettings.Settings.LoginUserId;
							}
							if (item.tags != null)
							{
								item.tag = TagSerializer.ToJsonContent(item.tags.ToList());
							}
							if (item.exDate != null)
							{
								item.exDates = ExDateSerilizer.ToString(item.exDate);
							}
						}
					}
					return list;
				}
				catch (Exception)
				{
					return null;
				}
			}
		}
		return null;
	}

	public static async Task<string> ReminderToPay(string projectId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectId}/reminderToPay", "", null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<string> HandleShareApplication(string notificationId, string type)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/collaboration/{type}?notificationId={notificationId}", "", null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<string> CheckUpdate()
	{
		string domainUrl = BaseUrl.DomainUrl;
		string text = (Environment.Is64BitOperatingSystem ? "win64" : "win");
		return await ticktick_WPF.Util.Network.Network.GetHttpRealUrlRequest(domainUrl + "/static/getApp/download?type=" + text, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<List<CommentModel>> GetComments(string projectSid, string taskSid)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectSid}/task/{taskSid}/comments", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<CommentModel>>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<string> DeleteComment(string projectSid, string taskSid, string commentSid)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectSid}/task/{taskSid}/comment/{commentSid}", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<string> AddComment(RemoteCommentModel comment, bool isUpdate = false)
	{
		string text = $"/api/v2/project/{TaskCache.GetTaskById(comment.taskId)?.ProjectId ?? comment.projectId}/task/{comment.taskId}/comment";
		if (isUpdate)
		{
			text = text + "/" + comment.id;
		}
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest(text, JsonConvert.SerializeObject(comment, Formatting.None, new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		}), null, LocalSettings.Settings.LoginUserAuth, isUpdate ? "PUT" : "POST");
	}

	public static async Task<List<HolidayModel>> GetRecentHoliday()
	{
		string currentRegion = HolidayRegion.cn;
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("https://pull.dida365.com/common/calendar/holiday_cn.json", "", null, LocalSettings.Settings.LoginUserAuth, "GET", fulluri: true);
		try
		{
			List<HolidayModel>? list = JsonConvert.DeserializeObject<List<HolidayModel>>(value);
			list.ForEach(delegate(HolidayModel item)
			{
				item.region = currentRegion;
			});
			return list;
		}
		catch (Exception)
		{
			return new List<HolidayModel>();
		}
	}

	public static async Task<PomodoroModel> PullRemoteFocus(string id, bool isPomo)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		string text = (isPomo ? "/api/v2/pomodoro/" : "/api/v2/pomodoro/timing/");
		try
		{
			return await ApiClient.GetJsonAsync<PomodoroModel>(BaseUrl.GetUrl(text + Uri.EscapeDataString(id)));
		}
		catch (ApiException ex) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			ApiError error = ex.Error;
			return error != null && error.StatusCode == HttpStatusCode.NotFound;
		}).Invoke())
		{
			return null;
		}
	}

	public static async Task<List<PomodoroModel>> PullRemotePomos(DateTime from, DateTime to)
	{
		try
		{
			List<PomodoroModel> list = JsonConvert.DeserializeObject<List<PomodoroModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/pomodoros?from={Utils.GetTimeStamp(from)}&to={Utils.GetTimeStamp(to)}", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
			if (list != null && list.Count > 0)
			{
				return list;
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<List<PomodoroModel>> PullRemoteTimings(DateTime from, DateTime to)
	{
		try
		{
			List<PomodoroModel> list = JsonConvert.DeserializeObject<List<PomodoroModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/pomodoros/timing?from={Utils.GetTimeStamp(from)}&to={Utils.GetTimeStamp(to)}", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
			if (list != null && list.Count > 0)
			{
				return list;
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<PomoConfigModel> GetRemotePomoConfig()
	{
		try
		{
			return JsonConvert.DeserializeObject<PomoConfigModel>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/pomodoro", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task UpdateRemotePomoConfig(PomoConfigModel config)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/pomodoro", JsonConvert.SerializeObject(config), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<List<TaskModel>> SearchRemoteTasks(string key)
	{
		try
		{
			List<TaskModel> list = JsonConvert.DeserializeObject<List<TaskModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/search/task?keywords={key}", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
			return (list == null || list.Count <= 0) ? new List<TaskModel>() : list;
		}
		catch (Exception)
		{
			return new List<TaskModel>();
		}
	}

	public static async Task<string> GetTaskActivities(string projectId, string taskId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v1/project/{projectId}/task/{taskId}/activity", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<string> GetProjectActivities(string projectId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v1/project/{projectId}/activity", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<string> RenameTag(string original, string revised)
	{
		TagModifyModel value = new TagModifyModel
		{
			name = original,
			newName = revised
		};
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/tag/rename", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	[Obsolete("使用DeleteTag2,解决异常符号无法删除标签的问题")]
	public static async Task DeleteTag(string tag)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/tag?name=" + Utils.UrlEncode(tag), "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task DeleteTag2(string tag)
	{
		JObject jObject = new JObject();
		jObject.Add("name", tag);
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/tag/delete", JsonConvert.SerializeObject(jObject), null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<CalendarSubscribeProfileModel> SubscribeCalendar(CalendarSubscribeProfileModel profile)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/subscribe", JsonConvert.SerializeObject(profile), null, LocalSettings.Settings.LoginUserAuth);
		return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<CalendarSubscribeProfileModel>(value) : null;
	}

	public static async Task<List<CalendarSubscribeProfileModel>> BatchSubscribeCalendar(IEnumerable<string> urls)
	{
		var jsonObj = new { urls };
		try
		{
			List<CalendarSubscribeProfileModel> list = await ApiClient.PostJsonAsync<List<CalendarSubscribeProfileModel>>(BaseUrl.GetUrl("/api/v2/calendar/batch/subscribe"), jsonObj);
			if (list == null)
			{
				return null;
			}
			return list;
		}
		catch (ApiException ex)
		{
			UtilLog.Error("BatchSubscribeCalendar Error:" + ex.ErrorCode + ex.ErrorMessage);
		}
		return null;
	}

	public static async Task UnsubscribeCalendar(string id)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/unsubscribe/" + id, "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<bool> BatchUnsubscribeCalendar(IEnumerable<string> ids)
	{
		try
		{
			await ApiClient.DeleteJsonAsync(BaseUrl.GetUrl("/api/v2/calendar/unsubscribe"), ids);
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Error("BatchUnsubscribeCalendar:" + ex.Message);
			return false;
		}
	}

	public static async Task<List<CalendarSubscribeProfileModel>> GetCalendarSubscriptions()
	{
		try
		{
			return JsonConvert.DeserializeObject<List<CalendarSubscribeProfileModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/subscription", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<List<BindCalendarAccountModel>> GetBindCalendarAccounts(DateTime? start, DateTime? end)
	{
		_ = 1;
		try
		{
			if (!start.HasValue || !end.HasValue)
			{
				return JsonConvert.DeserializeObject<List<BindCalendarAccountModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/accounts", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
			}
			return JsonConvert.DeserializeObject<List<BindCalendarAccountModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/accounts", "{\"begin\" : \"" + start.Value.ToString() + "\",\"end\": \"" + end.Value.ToString() + "\"}", null, LocalSettings.Settings.LoginUserAuth, "GET"));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<BindCalendarsCollection> GetBindCalendarEvents()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/events/all", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<BindCalendarsCollection>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return new BindCalendarsCollection();
		}
	}

	public static async Task<BindCalendarsCollection> GetBindCalendarEvents(DateTime startTime, DateTime endTime)
	{
		try
		{
			string text = startTime.ToString(UtcDateTimeConverter.GetConverterValue(startTime));
			string text2 = endTime.ToString(UtcDateTimeConverter.GetConverterValue(endTime));
			return JsonConvert.DeserializeObject<BindCalendarsCollection>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/events/all", "{\"begin\": \"" + text + "\",\"end\": \"" + text2 + "\"}", null, LocalSettings.Settings.LoginUserAuth));
		}
		catch (Exception)
		{
			return new BindCalendarsCollection();
		}
	}

	public static async Task<BindCalendarAccountModel> BindCalendarAccount(string code, string site, string url)
	{
		return JsonConvert.DeserializeObject<BindCalendarAccountModel>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind?code=" + code + "&state=" + site + "&channel=win&redirectUrl=" + url, "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
	}

	public static async Task<BindCalendarAccountModel> BindCalDavAccount(BindAccountModel model)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/bind", auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
		if (text.Contains("caldav_bind_faild"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_faild");
		}
		if (text.Contains("caldav_bind_duplicate"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_duplicate");
		}
		if (text.Contains("caldav_bind_exceed"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_exceed");
		}
		return JsonConvert.DeserializeObject<BindCalendarAccountModel>(text);
	}

	public static async Task<string> UpdateCalDavAccount(string accoundId, BindAccountModel model)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/update/" + accoundId, auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
	}

	public static async Task UnbindCalendar(string calendarId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/" + calendarId, "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<List<BindCalendarModel>> GetBindCalDavEvents(string calendarId, DateTime? pullStart, DateTime? pullEnd)
	{
		string text = pullStart?.ToString(UtcDateTimeConverter.GetConverterValue(pullStart.Value));
		string text2 = pullEnd?.ToString(UtcDateTimeConverter.GetConverterValue(pullEnd.Value));
		string content = "{ \"begin\": \"" + text + "\",\"end\": \"" + text2 + "\" }";
		string text3 = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v4/calendar/bind/events/{calendarId}", content, null, LocalSettings.Settings.LoginUserAuth);
		if (!pullStart.HasValue && !pullEnd.HasValue)
		{
			CalendarService.TryLogResult(calendarId, text3);
		}
		if (text3.Contains("calendar_authorization_invalid"))
		{
			throw new CustomException.CalendarExpiredException();
		}
		try
		{
			return JsonConvert.DeserializeObject<List<BindCalendarModel>>(text3);
		}
		catch (Exception)
		{
			UtilLog.Warn(text3);
			return new List<BindCalendarModel>();
		}
	}

	public static async Task<List<BindCalendarModel>> GetBindCalendarEvents(string calendarId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/calendar/bind/events/{calendarId}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (text.Contains("calendar_authorization_invalid"))
		{
			throw new CustomException.CalendarExpiredException();
		}
		CalendarService.TryLogResult(calendarId, text);
		try
		{
			return JsonConvert.DeserializeObject<List<BindCalendarModel>>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return new List<BindCalendarModel>();
		}
	}

	public static async Task<List<BindCalendarModel>> GetExchangeEvents(string calendarId, DateTime? startTime, DateTime? endTime)
	{
		string text = startTime?.ToString(UtcDateTimeConverter.GetConverterValue(startTime.Value));
		string text2 = endTime?.ToString(UtcDateTimeConverter.GetConverterValue(endTime.Value));
		string content = "{ \"begin\": \"" + text + "\",\"end\": \"" + text2 + "\" }";
		string text3 = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/calendar/bind/events/exchange/{calendarId}", content, null, LocalSettings.Settings.LoginUserAuth);
		if (!startTime.HasValue && !endTime.HasValue)
		{
			CalendarService.TryLogResult(calendarId, text3);
		}
		if (text3.Contains("calendar_authorization_invalid"))
		{
			throw new CustomException.CalendarExpiredException();
		}
		try
		{
			return JsonConvert.DeserializeObject<List<BindCalendarModel>>(text3);
		}
		catch (Exception)
		{
			UtilLog.Warn(text3);
			return new List<BindCalendarModel>();
		}
	}

	public static async Task<ErrorModel> CheckExchangeAccount(string account)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/calendar/exchange/check?account={account}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<ErrorModel>(value);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<BindCalendarAccountModel> BindExchangeAccount(BindAccountModel model)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/exchange", auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
		if (text.Contains("calendar_bind_faild"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_faild");
		}
		if (text.Contains("calendar_bind_duplicate"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_duplicate");
		}
		if (text.Contains("calendar_bind_exceed"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_exceed");
		}
		return JsonConvert.DeserializeObject<BindCalendarAccountModel>(text);
	}

	public static async Task<string> UpdateExchangeAccount(string accoundId, BindAccountModel model)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/exchange/update/" + accoundId, auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
	}

	public static async Task<BindCalendarAccountModel> BindICloudAccount(BindAccountModel model)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/bind/icloud", auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
		if (text.Contains("caldav_bind_faild"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_faild");
		}
		if (text.Contains("caldav_bind_duplicate"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_duplicate");
		}
		if (text.Contains("caldav_bind_exceed"))
		{
			throw new CustomException.CalendarBindException("calendar_bind_exceed");
		}
		return JsonConvert.DeserializeObject<BindCalendarAccountModel>(text);
	}

	public static async Task<string> UpdateICloudAccount(string accoundId, BindAccountModel model)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/update/icloud/" + accoundId, auth: LocalSettings.Settings.LoginUserAuth, content: JsonConvert.SerializeObject(model));
	}

	public static async Task<string> GetICloudGuideUrl()
	{
		string json = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/icloud/support/url", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JObject.Parse(json)["url"]?.ToString();
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<bool> UploadBindCalenderSort(params UploadBindCalendarSortDto[] dtos)
	{
		try
		{
			return string.IsNullOrEmpty(await ApiClient.PostJsonAsync<string>(BaseUrl.GetUrl("/api/v2/calendar/subscribe/sortOrder"), dtos));
		}
		catch (Exception ex)
		{
			UtilLog.Error("UploadBindCalenderSort: " + ex.Message);
			return false;
		}
	}

	public static async Task MergeTag(string tag, string newTag)
	{
		TagModifyModel value = new TagModifyModel
		{
			name = tag,
			newName = newTag
		};
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/tag/merge", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<bool> CheckTagInShareTask(string tag)
	{
		try
		{
			return await ApiClient.PostJsonAsync<bool>(BaseUrl.GetUrl("/api/v2/tag/check/shareTask"), tag);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<UserSettingsModel> GetUserSettings()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/settings?includeWeb=true", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<UserSettingsModel>(value) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task SaveUserSettings(UserSettingsModel model)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/settings?includeWeb=true", JsonConvert.SerializeObject(model), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<UserPreferenceModel> GetUserPreference(long mtime)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/ext" + $"?mtime={mtime}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<UserPreferenceModel>(value) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<bool> SaveUserPreference(UserPreferenceModel model)
	{
		JObject jObject = JObject.FromObject(model);
		if (jObject.TryGetValue("smartProjectsOption", out JToken value) && value.SelectToken("smartProjects") is JArray jArray)
		{
			foreach (JToken item in jArray)
			{
				if (item.SelectToken("timeline") is JObject jObject2)
				{
					jObject2.Remove("range");
					jObject2.Remove("DayWidthIndex");
					jObject2.Remove("sortType");
				}
			}
		}
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestSuccess("/api/v2/user/preferences/ext", jObject.ToString(), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<ProjectIdentityModel> DuplicateProject(string projectId, ProjectModel project, int type = 3)
	{
		type = Math.Max(1, Math.Min(3, type));
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectId}/duplicate" + "?option=" + type, JsonConvert.SerializeObject(project), null, LocalSettings.Settings.LoginUserAuth);
		if (!string.IsNullOrEmpty(value))
		{
			return JsonConvert.DeserializeObject<ProjectIdentityModel>(value);
		}
		return null;
	}

	public static async Task ResentVerifyEmail()
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/resentVerifyEmail", "", null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<TaskAttendModel> LoadTaskAttendInfo(string attendId, string copyTaskId)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/{attendId}/attendees?taskId={copyTaskId}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<TaskAttendModel>(value) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task DeleteTaskAttend(string projectId, string taskId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/{projectId}/delete/{taskId}", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<string> CreateTaskAttend(string projectId, string taskId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/invitation/create?projectId={projectId}&taskId={taskId}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		return (!string.IsNullOrEmpty(text)) ? text : string.Empty;
	}

	public static async Task<string> OpenInvitation(string attendId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/pub/api/v2/task-attend/invitation/{attendId}/open", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		return (!string.IsNullOrEmpty(text)) ? text : string.Empty;
	}

	public static async Task<string> EchoInvitation(string attendId, string status)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/invitation/{attendId}/response?status={status}", "", null, LocalSettings.Settings.LoginUserAuth);
		return (!string.IsNullOrEmpty(text)) ? text : string.Empty;
	}

	public static async Task DeleteAttachment(string projectId, string taskId, string attachmentId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v1/attachment/{projectId}/{taskId}/{attachmentId}", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task RemoveAttendee(string projectId, string taskId, string userCode)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/{projectId}/attendees/{taskId}?userCode={userCode}", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task RemoveAgenda(string projectId, string taskId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task-attend/{projectId}/attend/{taskId}", "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<BatchUpdateResult> BatchUpdateColumns(SyncColumnBean syncBean)
	{
		string content = JsonConvert.SerializeObject(syncBean);
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/column", content, null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<List<ColumnModel>> GetRemoteColumnsByProjectId(string projectId)
	{
		try
		{
			string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/column/project/{projectId}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
			return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<List<ColumnModel>>(value) : new List<ColumnModel>();
		}
		catch (Exception)
		{
			return new List<ColumnModel>();
		}
	}

	public static async Task<SyncColumnBean> CheckRemoteColumnChanged(long checkPoint)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/column?from={checkPoint}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return (!string.IsNullOrEmpty(text)) ? JsonConvert.DeserializeObject<SyncColumnBean>(text) : new SyncColumnBean();
		}
		catch (Exception ex)
		{
			string text2 = ExceptionUtils.BuildExceptionMessage(ex);
			if (App.IsAdmin)
			{
				text2 = text2 + "\r\n" + text;
			}
			UtilLog.Warn(text2);
		}
		return null;
	}

	public static async Task<bool> ClearTrash()
	{
		try
		{
			return await ApiClient.DeleteAsync(BaseUrl.GetUrl("/api/v2/trash/cleanUp"));
		}
		catch (ApiException)
		{
			return false;
		}
	}

	public static async Task<List<TeamModel>> GetAllTeams()
	{
		try
		{
			return JsonConvert.DeserializeObject<List<TeamModel>>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/teams", "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<string> CreateTeam(string name)
	{
		ServerTeamModel value = new ServerTeamModel
		{
			name = name
		};
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/team", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<TeamMessageModel> GetTeamMessage(string teamId)
	{
		return JsonConvert.DeserializeObject<TeamMessageModel>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/team/" + teamId, "", null, LocalSettings.Settings.LoginUserAuth, "GET"));
	}

	public static async Task<string> GetTeamMember(string teamId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/team/{teamId}/members", "", null, LocalSettings.Settings.LoginUserAuth, "GET", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<ErrorModel> TransferProject(string projectId, string toUserCode)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/project/{projectId}/transfer?toUserCode={toUserCode}", "", null, LocalSettings.Settings.LoginUserAuth);
		return (!string.IsNullOrEmpty(value)) ? JsonConvert.DeserializeObject<ErrorModel>(value) : null;
	}

	public static async Task<string> AcceptTeamApply(string id)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/team/collaboration/accept?notificationId={id}", "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
		return (!string.IsNullOrEmpty(text)) ? text : string.Empty;
	}

	public static async Task<string> RefuseTeamApply(string notificationItemId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/team/collaboration/refuse?notificationId={notificationItemId}", "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return string.Empty;
	}

	public static async Task QueryMember(string teamId)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/team/" + teamId + "/members", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<ErrorModel> UpgradeTeamProject(string projectId, string teamId)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/upgrade?teamId=" + teamId, "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
		return string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<ErrorModel>(value);
	}

	public static async Task<ErrorModel> DegradeTeamProject(string projectId)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/degrade", "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
		return string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<ErrorModel>(value);
	}

	public static async Task<int?> CheckShareProjectQuota(string projectId)
	{
		(string, bool) tuple = await ticktick_WPF.Util.Network.Network.GetHttpWebRequestResult($"/api/v2/project/{projectId}/share/check-quota", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		int? result = ParseShareProjectQuota(tuple.Item1, tuple.Item2);
		if (!result.HasValue)
		{
			UtilLog.Warn($"Share quota check failed: projectId {projectId}, requestSucceeded {tuple.Item2}");
		}
		return result;
	}

	internal static int? ParseShareProjectQuota(string result, bool success)
	{
		if (!success || string.IsNullOrWhiteSpace(result))
		{
			return null;
		}
		try
		{
			JToken jToken = JToken.Parse(result);
			if (jToken.Type == JTokenType.Integer)
			{
				return jToken.ToObject<int>();
			}
			int result2;
			return (jToken.Type == JTokenType.String && int.TryParse(jToken.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result2)) ? new int?(result2) : ((int?)null);
		}
		catch (JsonException)
		{
			return null;
		}
		catch (OverflowException)
		{
			return null;
		}
	}

	public static async Task<string> SetPermission(string projectId, Constants.ProjectPermission permission, string recordId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/permission?permission=" + permission.ToString() + "&recordId=" + recordId, "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task ApplyPermission(string projectId, Constants.ProjectPermission write)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/" + projectId + "/permission/apply?permission=" + write, "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<string> AcceptApplyPermission(string notificationId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/permission/accept?notificationId=" + notificationId, "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<string> RefuseApplyPermission(string notificationId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/project/permission/refuse?notificationId=" + notificationId, "", null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<List<EventUpdateResult>> BatchUpdateCalendarEvents(SyncEventBean bean)
	{
		return SafeConvertCalendarResult(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/bind/events/batch", JsonConvert.SerializeObject(bean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<List<EventUpdateResult>> BatchUpdateCalDavEvents(SyncEventBean bean)
	{
		return SafeConvertCalendarResult(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v4/calendar/bind/events/batch", JsonConvert.SerializeObject(bean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<TemplatesModel> PullTemplates()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/templates", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<TemplatesModel>(value);
		}
		catch (Exception)
		{
			return new TemplatesModel();
		}
	}

	public static async Task<BatchUpdateResult> PushTemplates(TaskTemplateSyncBean syncBean, bool isNote = false)
	{
		string content = JsonConvert.SerializeObject(syncBean);
		string api = "/api/v2/templates" + (isNote ? "/note" : "/task");
		string loginUserAuth = LocalSettings.Settings.LoginUserAuth;
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest(api, content, null, loginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true));
	}

	public static async Task<ProjectTemplatesDto> QueryProjectTemplate(long timestamp)
	{
		try
		{
			return await ApiClient.GetJsonAsync<ProjectTemplatesDto>(BaseUrl.GetUrl(string.Format("{0}?timestamp={1}", "/api/v2/projectTemplates/all", timestamp)));
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return null;
		}
	}

	public static async Task<bool> ApplyProjectTemplate(string projectTemplateId)
	{
		try
		{
			return await ApiClient.PostJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/templates/project/{projectTemplateId}/apply"), null);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return false;
		}
	}

	public static void UploadLogs(string content, string ticketId, string tt)
	{
		string text = "ticket.ticktick.com";
		text = "support.dida365.com";
		if (!BaseUrl.IsDefault)
		{
			string ip = HttpDnsHandler.GetIp(text);
			if (string.IsNullOrEmpty(ip))
			{
				text = ip;
			}
		}
		ticktick_WPF.Util.Network.Network.PostLog("https://" + text + "/api/v1/ticket/" + ticketId + "/attachment", content, tt);
	}

	public static void MarkReadNotification(string category)
	{
		ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/notification/markRead?category=" + category, "", null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<List<HabitSectionModel>> PullRemoteHabitSections()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitSections", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<HabitSectionModel>>(value);
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	public static async Task<List<HabitModel>> PullRemoteHabits()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habits", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		List<HabitModel> result = null;
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				result = JsonConvert.DeserializeObject<List<HabitModel>>(text);
			}
			catch (Exception ex)
			{
				string text2 = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text2 = text2 + "\r\n" + text;
				}
				UtilLog.Warn(text2);
			}
		}
		return result;
	}

	public static async Task<TaskModel> GetTask(string taskId, string projectId, bool withChildren = false)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/task/{taskId}?projectId={projectId}&withChildren={withChildren}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				TaskModel? taskModel = JsonConvert.DeserializeObject<TaskModel>(text);
				if (taskModel?.id == null)
				{
					UtilLog.Warn(text);
				}
				return taskModel;
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static async Task<string> CommitHabitCheckIns(SyncHabitCheckInBean bean)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitCheckins/batch", JsonConvert.SerializeObject(bean), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<string> CommitHabitRecords(SyncHabitRecordBean bean)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitRecords", JsonConvert.SerializeObject(bean), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<List<HabitRecordModel>> PullHabitRecords(string habitId, int stamp)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return null;
		}
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitRecords/" + habitId + ((stamp > 0) ? ("?afterStamp=" + stamp) : string.Empty), "", null, LocalSettings.Settings.LoginUserAuth, "GET", fulluri: false, isNeedErrorReturn: true);
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<HabitRecordModel>>(value);
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	public static async Task<HabitRecordCheckResult> PullHabitsRecords(List<string> habitIds, int stamp)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return null;
		}
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitRecords/" + BuildHabitArgIds(habitIds) + ((stamp > 0) ? ("&afterStamp=" + stamp) : string.Empty), "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<HabitRecordCheckResult>(value);
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	public static async Task<CheckInCollection> PullHabitsCheckIns(List<string> habitIds, int stamp)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return null;
		}
		JObject jObject = new JObject();
		JArray jArray = new JArray();
		foreach (string habitId in habitIds)
		{
			jArray.Add(habitId);
		}
		jObject.Add("habitIds", jArray);
		jObject.Add("afterStamp", stamp);
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitCheckins/query", JsonConvert.SerializeObject(jObject), null, LocalSettings.Settings.LoginUserAuth);
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				return JsonConvert.DeserializeObject<CheckInCollection>(text);
			}
			catch (Exception ex)
			{
				string text2 = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text2 = text2 + "\r\n" + text;
				}
				UtilLog.Warn(text2);
			}
		}
		return null;
	}

	private static string BuildHabitArgIds(IEnumerable<string> habitIds)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("?");
		foreach (string habitId in habitIds)
		{
			stringBuilder.Append("habitIds=" + habitId + "&");
		}
		string text = stringBuilder.ToString();
		if (text.EndsWith("&"))
		{
			text = text.Substring(0, text.Length - 1);
		}
		return text;
	}

	public static async Task<List<HabitCheckInModel>> PullHabitCheckIns(string habitId, int stamp)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return null;
		}
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitCheckins/" + habitId + ((stamp > 0) ? ("?afterStamp=" + stamp) : string.Empty), "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<HabitCheckInModel>>(value);
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static async Task<HabitSettingsModel> GetRemoteHabitConfig()
	{
		try
		{
			string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/habit?platform=windows", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
			return string.IsNullOrEmpty(value) ? null : JsonConvert.DeserializeObject<HabitSettingsModel>(value);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task PushHabitConfig(HabitSettingsModel config)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/preferences/habit?platform=windows", JsonConvert.SerializeObject(config), null, LocalSettings.Settings.LoginUserAuth, "PUT");
	}

	public static async Task<string> UpdateHabit(SyncHabitBean habitBean)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habits/batch", JsonConvert.SerializeObject(habitBean), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<BatchUpdateResult> UpdateHabitSections(SyncHabitSectionsBean sectionBean)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/habitSections/batch", JsonConvert.SerializeObject(sectionBean), null, LocalSettings.Settings.LoginUserAuth);
		if (text != null && (text.Contains("id2error") || text.Contains("id2etag")))
		{
			return JsonConvert.DeserializeObject<BatchUpdateResult>(text);
		}
		return null;
	}

	public static async Task<PushModel> RegisterPushService(string id, string pushToken)
	{
		PushModel value = new PushModel
		{
			id = id,
			pushToken = pushToken,
			hl = Utils.GetLanguage()
		};
		string value2 = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/push/register", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			return JsonConvert.DeserializeObject<PushModel>(value2);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task NotifyCloseReminder(RemindMessage model)
	{
		UtilLog.Warn("close remider :" + await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/task/closeRemind", JsonConvert.SerializeObject(model), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task TryNotifyCloseReminder(RemindMessage model)
	{
		try
		{
			await NotifyCloseReminder(model);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("close reminder failed: " + ex.Message);
		}
	}

	public static async Task NotifyPushArrive(PushArriveModel model)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/push/stats/arrive", JsonConvert.SerializeObject(new List<PushArriveModel> { model }), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<SyncTaskParentRes> BatchPushTaskParent(List<SyncTaskParentModel> models)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/batch/taskParent", JsonConvert.SerializeObject(models), null, LocalSettings.Settings.LoginUserAuth);
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				return JsonConvert.DeserializeObject<SyncTaskParentRes>(text);
			}
			catch (Exception ex)
			{
				string text2 = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text2 = text2 + "\r\n" + text;
				}
				UtilLog.Warn(text2);
				return new SyncTaskParentRes();
			}
		}
		return new SyncTaskParentRes();
	}

	public static async Task SetTaskAssign(List<AssignModel> models)
	{
		await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/task/assign", JsonConvert.SerializeObject(models), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<SearchResultModel> GetSearchedResult(string searchKey, SearchFilterModel filterModel)
	{
		string text = $"/api/v2/search/all?keywords={searchKey}";
		if (filterModel.ProjectIds != null)
		{
			text = filterModel.ProjectIds.Aggregate(text, (string text2, string projectId) => text2 + "&projectId=" + projectId);
		}
		if (filterModel.Tags != null)
		{
			text = filterModel.Tags.Aggregate(text, (string text2, string tag) => text2 + "&tags=" + tag);
		}
		if (filterModel.Status.HasValue)
		{
			text = text + "&status=" + filterModel.Status.Value;
		}
		if (filterModel.Start.HasValue)
		{
			text = text + "&dueFrom=" + filterModel.Start.Value.ToString(UtcDateTimeConverter.GetConverterValue(filterModel.Start.Value));
		}
		if (filterModel.End.HasValue)
		{
			text = text + "&dueTo=" + filterModel.End.Value.ToString(UtcDateTimeConverter.GetConverterValue(filterModel.End.Value));
		}
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest(text, "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				SearchResultModel searchResultModel = JsonConvert.DeserializeObject<SearchResultModel>(value);
				if (searchResultModel?.Tasks != null)
				{
					if (searchResultModel.Tasks.Count > 0)
					{
						foreach (TaskModel task in searchResultModel.Tasks)
						{
							TaskService.ClearDueDate(task);
							if (string.IsNullOrEmpty(task.userId))
							{
								task.userId = Utils.GetCurrentUserIdInt().ToString();
							}
							task.GetStringFromArray();
						}
					}
					return searchResultModel;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static async Task<List<TaskModel>> GetSearchedTasks(string searchKey, SearchFilterModel filterModel)
	{
		string text = $"/api/v2/search/task?keywords={searchKey}";
		if (filterModel.ProjectIds != null)
		{
			text = filterModel.ProjectIds.Aggregate(text, (string text2, string projectId) => text2 + "&projectId=" + projectId);
		}
		if (filterModel.Tags != null)
		{
			text = filterModel.Tags.Aggregate(text, (string text2, string tag) => text2 + "&tags=" + tag);
		}
		if (filterModel.Status.HasValue)
		{
			text = text + "&status=" + filterModel.Status.Value;
		}
		if (filterModel.Start.HasValue)
		{
			text = text + "&dueFrom=" + filterModel.Start.Value.ToString(UtcDateTimeConverter.GetConverterValue(filterModel.Start.Value));
		}
		if (filterModel.End.HasValue)
		{
			text = text + "&dueTo=" + filterModel.End.Value.ToString(UtcDateTimeConverter.GetConverterValue(filterModel.End.Value));
		}
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest(text, "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				List<TaskModel> list = JsonConvert.DeserializeObject<List<TaskModel>>(value);
				if (list != null && list.Count > 0)
				{
					foreach (TaskModel item in list)
					{
						TaskService.ClearDueDate(item);
						if (string.IsNullOrEmpty(item.userId))
						{
							item.userId = Utils.GetCurrentUserIdInt().ToString();
						}
						if (item.tags != null)
						{
							item.tag = TagSerializer.ToJsonContent(item.tags.ToList());
						}
						if (item.exDate != null)
						{
							item.exDates = ExDateSerilizer.ToString(item.exDate);
						}
					}
					return list;
				}
			}
			catch (Exception)
			{
				return new List<TaskModel>();
			}
		}
		return new List<TaskModel>();
	}

	public static async Task<AttachmentModel> UpdateAttachment(AttachmentModel attachment, string taskId, string projectId)
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v1/attachment/{projectId}/{taskId}/{attachment.id}" + "/status?status=" + attachment.status, "", null, LocalSettings.Settings.LoginUserAuth);
		try
		{
			AttachmentModel attachmentModel = JsonConvert.DeserializeObject<AttachmentModel>(value);
			if (attachmentModel?.id == attachment.id)
			{
				return attachmentModel;
			}
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static async Task<UserModel> LoginApple(string accessToken, string uId, string email, string username)
	{
		string apiDomain = BaseUrl.GetApiDomain();
		string api = apiDomain + "/api/v2/user/sign/OAuth2/apple?accessToken=" + accessToken + "&uId=" + uId + "&platform=windows";
		JObject jObject = new JObject();
		if (!string.IsNullOrEmpty(email))
		{
			jObject.Add("email", email);
		}
		if (!string.IsNullOrEmpty(username))
		{
			jObject.Add("name", username);
		}
		string loginReturn = string.Empty;
		try
		{
			loginReturn = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest(api, jObject.ToString(), null, null, "POST", fulluri: true, isNeedErrorReturn: true);
			UserModel userModel = JsonConvert.DeserializeObject<UserModel>(loginReturn);
			if ((userModel != null && userModel.userId != null && userModel.userId != "") || !string.IsNullOrEmpty(userModel?.authId))
			{
				return userModel;
			}
			return new UserModel
			{
				username = loginReturn
			};
		}
		catch (Exception)
		{
			return new UserModel
			{
				username = loginReturn
			};
		}
	}

	public static async Task PullAppConfig(bool fromLogin = false, bool ignoreEtag = false)
	{
		LocalSettings settings = LocalSettings.Settings;
		string text = BaseUrl.GetApiDomain() + "/pub/api/v1/app/config";
		if (fromLogin)
		{
			text += "?from=login";
		}
		if (!ignoreEtag)
		{
			string appConfigEtag = settings.ExtraSettings.AppConfigEtag;
			if (!string.IsNullOrEmpty(appConfigEtag))
			{
				text = text + "?etag=" + appConfigEtag;
			}
		}
		Dictionary<string, object> dictionary = await ApiClient.GetJsonAsync<Dictionary<string, object>>(text);
		if (dictionary == null || dictionary.Count <= 0)
		{
			return;
		}
		foreach (string key in dictionary.Keys)
		{
			switch (key)
			{
			case "etag":
				LocalSettings.Settings.ExtraSettings.AppConfigEtag = dictionary[key].ToString();
				LocalSettings.Settings.Save();
				break;
			case "interval":
			{
				int num = int.Parse(dictionary[key].ToString());
				int appConfigInterval = LocalSettings.Settings.ExtraSettings.AppConfigInterval;
				if (num != appConfigInterval)
				{
					LocalSettings.Settings.ExtraSettings.AppConfigInterval = num;
					LocalSettings.Settings.Save();
					AppConfigManager.StartPolling();
				}
				break;
			}
			case "ab":
			{
				AbBestData abBestData = JsonConvert.DeserializeObject<AbBestData>(dictionary[key].ToString());
				if (abBestData?.best != null && abBestData.best.Count > 0)
				{
					ABTestManager.HandleBestTest(abBestData.best);
				}
				break;
			}
			case "releaseNote":
				UpgradeAppService.ShowVersionNews(JsonConvert.DeserializeObject<ReleaseNoteDetailModel>(dictionary[key].ToString()));
				break;
			case "importantVersion":
				UpgradeAppService.CheckAutoUpdate(JsonConvert.DeserializeObject<ImportantVersionModel>(dictionary[key].ToString()));
				break;
			}
		}
	}

	public static async Task PullAppConfigQueryImportantVersion()
	{
		_ = LocalSettings.Settings;
		Dictionary<string, object> dictionary = await ApiClient.GetJsonAsync<Dictionary<string, object>>(BaseUrl.GetApiDomain() + "/pub/api/v1/app/config");
		if (dictionary == null || dictionary.Count <= 0)
		{
			return;
		}
		foreach (string key in dictionary.Keys)
		{
			if (key == "importantVersion")
			{
				UpgradeAppService.CheckAutoUpdate(JsonConvert.DeserializeObject<ImportantVersionModel>(dictionary[key].ToString()), fromConfigChange: true);
			}
		}
	}

	public static async Task<TabPlanData> ApplyABTestGroupResult(TestInfo testInfo)
	{
		string uri = BaseUrl.GetXApiDomain() + "/datacollect/pub/v1/ab/group";
		try
		{
			return await ApiClient.PostJsonStrAsync<TabPlanData>(uri, JsonConvert.SerializeObject(testInfo));
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			return new TabPlanData();
		}
	}

	public static async Task<TestCode2PlanCodeInfo> GetABTestGroupResult(BatchTestInfo testInfo)
	{
		string uri = BaseUrl.GetXApiDomain() + "/datacollect/pub/v1/ab/group/result";
		try
		{
			return await ApiClient.PostJsonStrAsync<TestCode2PlanCodeInfo>(uri, JsonConvert.SerializeObject(testInfo));
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			return new TestCode2PlanCodeInfo();
		}
	}

	public static async Task<bool> PostUserDevice(string json)
	{
		return await ticktick_WPF.Util.Network.Network.PostDataCollect("https://xapi.dida365.com/datacollect/device/update", json);
	}

	public static async Task PostException(Exception e, ExceptionType type)
	{
		string uri = "https://xapi.dida365.com/datacollect/log/ex";
		try
		{
			string json = JsonConvert.SerializeObject(new ExceptionCollectModel(e, type));
			await ticktick_WPF.Util.Network.Network.PostDataCollect(uri, json);
		}
		catch (Exception)
		{
		}
	}

	public static async Task<bool> PostUserEvents(List<string> postFiles)
	{
		return await ticktick_WPF.Util.Network.Network.PostFiles("https://xapi.dida365.com/datacollect/event/upload", postFiles);
	}

	[Obsolete]
	public static async void PostUserLogs()
	{
		List<string> logs = TicketLogUtils.GetUnPostLogs();
		string uri = "https://xapi.dida365.com/datacollect/log/upload";
		if (logs == null || logs.Count == 0 || !(await ticktick_WPF.Util.Network.Network.PostFiles(uri, logs)))
		{
			return;
		}
		foreach (string item in logs)
		{
			if (File.Exists(item))
			{
				string text = Path.GetFileName(item).Replace("log", "postlog");
				string destFileName = Path.GetDirectoryName(item) + "\\" + text;
				FileInfo fileInfo = new FileInfo(item);
				fileInfo.Attributes = FileAttributes.Normal;
				try
				{
					fileInfo.MoveTo(destFileName);
				}
				catch
				{
					fileInfo.Delete();
				}
			}
		}
	}

	public static async Task<YearPromoModel> GetYearPromo()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/pub/api/v1/promo/year2021", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<YearPromoModel>(value) ?? new YearPromoModel();
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<SubscribeCalendarModel> GetSubscribeEventsByCalId(string calendarId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/subscribe/events/" + calendarId, "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (text != null && text.Contains("calendar_fetch"))
		{
			throw new CustomException.CalendarExpiredException();
		}
		try
		{
			CalendarService.TryLogResult(calendarId, text);
			return JsonConvert.DeserializeObject<SubscribeCalendarModel>(text);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<List<CourseScheduleModel>> GetAllCourseSchedules()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v1/course/timetable", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<CourseScheduleModel>>(value);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<BindCalendarAccountModel> SubscribeOutlook(string code)
	{
		string url = Utils.UrlEncode("https://" + BaseUrl.GetDomain() + "/calendar/bind/outlook");
		return await BindCalendarAccount(code, "outlook", url);
	}

	public static async Task<CalendarEventData> GetBindOutlookEvents(DateTime? pullStart, DateTime? pullEnd)
	{
		string text = pullStart?.ToString(UtcDateTimeConverter.GetConverterValue(pullStart.Value));
		string text2 = pullEnd?.ToString(UtcDateTimeConverter.GetConverterValue(pullEnd.Value));
		string content = "{ \"begin\": \"" + text + "\",\"end\": \"" + text2 + "\" }";
		string text3 = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/calendar/bind/events/outlook", content, null, LocalSettings.Settings.LoginUserAuth);
		if (!pullStart.HasValue && !pullEnd.HasValue)
		{
			CalendarService.TryLogResult("outlook", text3);
		}
		try
		{
			return JsonConvert.DeserializeObject<CalendarEventData>(text3);
		}
		catch (Exception)
		{
			UtilLog.Warn(text3);
			return null;
		}
	}

	public static async Task<CalendarEventData> GetCacheEventsByAccountId(string accountId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/calendar/cache/{accountId}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<CalendarEventData>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<bool> PostArchivedCourses(CourseArchivePostModel model)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestSuccess("/api/v1/course/archived", JsonConvert.SerializeObject(model), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<List<CourseArchiveSyncModel>> GetArchivedCourses()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v1/course/archived", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<CourseArchiveSyncModel>>(value);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<bool> PostArchivedEvents(EventArchivePostModel model)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestSuccess("/api/v2/calendar/archivedEvent", JsonConvert.SerializeObject(model), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<List<EventArchiveSyncModel>> GetArchivedEvents()
	{
		string value = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/archivedEvent", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<EventArchiveSyncModel>>(value);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<FocusSyncResultBean> UploadFocusOptions(List<FocusOptionModel> options)
	{
		FocusOptionBean value = new FocusOptionBean
		{
			opList = options,
			lastPoint = LocalSettings.Settings.FocusPushPoint
		};
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/focus/batch/focusOp", JsonConvert.SerializeObject(value), null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: false, null, needReconnect: true, defaultApi: false, useMs: true);
		try
		{
			return JsonConvert.DeserializeObject<FocusSyncResultBean>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<StatisticsModel> GetStatistics()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/pomodoros/statistics/generalForDesktop", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<StatisticsModel>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<List<PomoTask>> GetRecentFocusTasks()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/pomodoro/bind/recent", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<PomoTask>>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<List<PomodoroModel>> GetFocusTimeline(long point)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/pomodoros/timeline?to={point}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<PomodoroModel>>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<List<TimerModel>> PullRemoteTimers()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/timer", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		List<TimerModel> result = null;
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				result = JsonConvert.DeserializeObject<List<TimerModel>>(text);
			}
			catch (Exception ex)
			{
				string text2 = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text2 = text2 + "\r\n" + text;
				}
				UtilLog.Warn(text2);
			}
		}
		return result;
	}

	public static async Task<BatchUpdateResult> UpdateTimers(ModelSyncBean<TimerModel> timerBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/timer", JsonConvert.SerializeObject(timerBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<TimerOverviewModel> PullTimerOverview(string id)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/timer/overview/" + id, "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (string.IsNullOrEmpty(text) || text.Contains("error"))
		{
			return null;
		}
		try
		{
			return JsonConvert.DeserializeObject<TimerOverviewModel>(text);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<string> PullTimerStatistics(string id, DateTime start, DateTime end, string interval)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/timer/statistics/{id}/{DateUtils.GetDateNum(start)}/{DateUtils.GetDateNum(end)}/{interval}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		try
		{
			return text;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static async Task<List<PomodoroModel>> GetTimerFocusTimeline(string timerId, long point)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/timer/timeline/{timerId}?to={point}", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			return JsonConvert.DeserializeObject<List<PomodoroModel>>(text);
		}
		catch (Exception)
		{
			UtilLog.Warn(text);
			return null;
		}
	}

	public static async Task<string> HandleTeamInvite(string notificationId, bool accept)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest($"/api/v2/team/accept/invite?notificationId={notificationId}&actionStatus={(accept ? 22 : 23)}", "", null, LocalSettings.Settings.LoginUserAuth, "POST", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<long> GetTeamShareCount(string teamId)
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/team/" + teamId + "/shareProjectNum", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		try
		{
			if (text.Contains("current") && text.Contains("max"))
			{
				JObject jObject = JObject.Parse(text);
				if (jObject.TryGetValue("current", out JToken value) && jObject.TryGetValue("max", out JToken value2))
				{
					return value2.Value<long>() - value.Value<long>();
				}
			}
		}
		catch (Exception)
		{
			return 1L;
		}
		return 1L;
	}

	public static async Task<(string, bool)> SetProjectOpenToTeam(string projectId, bool isChecked)
	{
		JObject jObject = new JObject();
		jObject.Add("id", projectId);
		jObject.Add("openToTeam", isChecked);
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestResult("/api/v2/project", jObject.ToString(), null, LocalSettings.Settings.LoginUserAuth, "PUT", fulluri: false, isNeedErrorReturn: true);
	}

	public static async Task<bool> IsTwitterUser()
	{
		try
		{
			if (JObject.Parse(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/user/userBindingInfo", "", null, LocalSettings.Settings.LoginUserAuth, "GET")).TryGetValue("thirdSiteBinds", out JToken value) && value is JArray jArray)
			{
				foreach (JToken item in jArray)
				{
					if (item is JObject jObject && jObject.TryGetValue("siteId", out JToken value2) && value2.ToString() == "7")
					{
						return true;
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	public static async Task<List<ThirdSiteBindType>> GetAllThirdSiteBindTypes()
	{
		List<ThirdSiteBindType> list = new List<ThirdSiteBindType>();
		try
		{
			foreach (UserBindingInfo.ThirdSideBind thirdSiteBind in (await ApiClient.GetJsonAsync<UserBindingInfo>(BaseUrl.GetUrl("/api/v2/user/userBindingInfo"))).thirdSiteBinds)
			{
				list.Add((ThirdSiteBindType)thirdSiteBind.siteId);
			}
		}
		catch (Exception)
		{
		}
		return list;
	}

	public static async Task<bool> UpdateReminderDelayModels(ReminderDelaySyncModel syncModel)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestSuccess("/api/v2/reminder/delay", JsonConvert.SerializeObject(syncModel), null, LocalSettings.Settings.LoginUserAuth);
	}

	public static async Task<string> GetProjectFeedsCode(string projectId)
	{
		(string, bool) tuple = await ticktick_WPF.Util.Network.Network.GetHttpWebRequestResult("/api/v2/calendar/feeds/code/new/" + projectId, "", null, LocalSettings.Settings.LoginUserAuth);
		if (tuple.Item2)
		{
			return tuple.Item1;
		}
		return null;
	}

	public static async Task<string> GetAllProjectFeedsCode()
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/calendar/feeds/code", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
	}

	public static async Task<bool> DeleteProjectFeedsCode(string projectId)
	{
		return await ticktick_WPF.Util.Network.Network.GetHttpWebRequestSuccess("/api/v2/calendar/feeds/cancel/" + projectId, "", null, LocalSettings.Settings.LoginUserAuth, "DELETE");
	}

	public static async Task<BatchUpdateResult> MoveColumnAsync(List<MoveColumnArgs> args)
	{
		return await ApiClient.PostJsonAsync<BatchUpdateResult>(BaseUrl.GetUrl("/api/v2/batch/columnProject"), args);
	}

	public static async Task<PaymentModelV1> PaymentV1(PayModel model)
	{
		try
		{
			return await ApiClient.PostJsonAsync<PaymentModelV1>(BaseUrl.GetUrl("/api/v1/payment"), model);
		}
		catch (ApiException ex)
		{
			UtilLog.Info("PaymentV1 Error " + ex.ErrorMessage + " " + ex.ErrorCode);
			return null;
		}
	}

	public static async Task<List<SubscribeSpecModel>> GetSubscribeSpecList()
	{
		return await ApiClient.GetJsonAsync<List<SubscribeSpecModel>>(BaseUrl.GetUrl("/pub/api/v2/subscribe/subscribe_spec"));
	}

	public static async Task<List<OrderSpecModel>> GetOrderSpecList()
	{
		return await ApiClient.GetJsonAsync<List<OrderSpecModel>>(BaseUrl.GetUrl("/pub/api/v2/payment/order_spec"));
	}

	public static async Task<PaymentStatus> CheckPaymentOrder(string orderId)
	{
		return await ApiClient.GetJsonAsync<PaymentStatus>(BaseUrl.GetUrl("/api/v1/payment/check/" + orderId));
	}

	public static async Task<bool> CancelPaymentSubs(string subsType, string profileId = null)
	{
		try
		{
			string text = BaseUrl.GetUrl("/api/v1/payment/cancel/" + subsType);
			if (!string.IsNullOrEmpty(profileId))
			{
				text = text + "?profileId=" + profileId;
			}
			return await ApiClient.PutJsonSuccessAsync(text, null);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return false;
		}
	}

	public static async Task<bool> CheckPaymentBlackList()
	{
		return await ApiClient.GetJsonAsync<bool>(BaseUrl.GetUrl("/api/v2/payment/isBlacklist"));
	}

	public static async Task<List<SubscribeInfoModel>> GetSubscribeList()
	{
		try
		{
			return await ApiClient.GetJsonAsync<List<SubscribeInfoModel>>(BaseUrl.GetUrl("/api/v2/subscribe/list"));
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return null;
		}
	}

	public static async Task<SubscribePriceInfoModel> GetSubscribeInfo(string profileId)
	{
		try
		{
			string text = BaseUrl.GetUrl("/api/v2/subscribe/info");
			if (!string.IsNullOrEmpty(profileId))
			{
				text = text + "?profileId=" + Uri.EscapeDataString(profileId);
			}
			return await ApiClient.GetJsonAsync<SubscribePriceInfoModel>(text);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
			return null;
		}
	}

	public static async Task<GuideProjectModel> GetGuideProject(string plan)
	{
		return await ApiClient.GetJsonAsync<GuideProjectModel>(BaseUrl.GetUrl($"/pub/api/v2/guide/project?p={plan}"));
	}

	public static async Task<List<TaskModel>> GetGuideTask()
	{
		return await ApiClient.GetJsonAsync<List<TaskModel>>(BaseUrl.GetUrl("/pub/api/v2/guide/task"));
	}

	public static async Task<CalendarConnectInfo> GetCalendarConnect(string code, string site)
	{
		string arg = "dida365.com";
		try
		{
			return await ApiClient.GetJsonAsync<CalendarConnectInfo>(BaseUrl.GetUrl($"/api/v2/calendar/connect?state={site}&code={code}&channel=win&redirectUrl={arg}"));
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static async Task<bool> BindProjectCalendar(string connectId, ProjectBindCalendarModel model)
	{
		try
		{
			return await ApiClient.PostJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/calendar/{connectId}/bind/project"), model);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<bool> ModifyProjectCalendar(string connectId, ModifyBindProjectCalendarModel model)
	{
		try
		{
			return await ApiClient.PutJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/calendar/{connectId}/bind/project"), model);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<ThirdAccountsModel> GetCalendarThirdAccounts()
	{
		try
		{
			return await ApiClient.GetJsonAsync<ThirdAccountsModel>(BaseUrl.GetUrl("/api/v2/calendar/third/accounts"));
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static async Task<TaskGoogleEventInfoSyncResult> GetTaskGoogleEventInfos(long checkpoint, string cursor, int limit)
	{
		try
		{
			string text = BaseUrl.GetUrl("/api/v1/google-event-infos/sync") + $"?checkpoint={checkpoint}&limit={limit}";
			if (!string.IsNullOrEmpty(cursor))
			{
				text = text + "&cursor=" + Uri.EscapeDataString(cursor);
			}
			return await ApiClient.GetJsonAsync<TaskGoogleEventInfoSyncResult>(text);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("GetTaskGoogleEventInfos failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			return null;
		}
	}

	public static async Task<bool> CalendarSetVisible(string id, string calendarId, string status)
	{
		try
		{
			return await ApiClient.PutJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/calendar/{id}/{calendarId}/show?visibility={status}&isMobile=false"), null);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<bool> CalendarUrlSetVisible(string calendarId, string status)
	{
		try
		{
			return await ApiClient.PutJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/calendar/subscription/{calendarId}/show?visibility={status}&isMobile=false"), null);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<bool> UpdateBindAccountCalendarDesc(string accountId, string desc)
	{
		try
		{
			var jsonObj = new { desc };
			return await ApiClient.PutJsonSuccessAsync(BaseUrl.GetUrl($"/api/v2/calendar/{accountId}/desc"), jsonObj);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static async Task<bool> UnConnectCalendar(string connectId)
	{
		return await ApiClient.DeleteAsync(BaseUrl.GetUrl($"/api/v2/calendar/connect/{connectId}"));
	}

	public static async Task<bool> UnbindProjectCalendar(string connectId, string id)
	{
		return await ApiClient.DeleteAsync(BaseUrl.GetUrl($"/api/v2/calendar/{connectId}/bind/project?id={id}"));
	}

	public static async Task<CalendarContentCalendar> CreateGoogleCalendar(string connectId, string name)
	{
		try
		{
			JObject jObject = new JObject();
			jObject.Add("calendarName", name);
			return await ApiClient.PostJsonAsync<CalendarContentCalendar>(BaseUrl.GetUrl($"/api/v2/calendar/{connectId}/create"), jObject);
		}
		catch
		{
			return null;
		}
	}

	public static async Task<string> CheckPhoneSigned(string phoneNumber, string verKey, string verCode)
	{
		JObject jObject = new JObject();
		jObject.Add("phone", phoneNumber);
		if (!string.IsNullOrEmpty(verKey))
		{
			jObject.Add("verCode", verCode);
			jObject.Add("verKey", verKey);
		}
		try
		{
			JObject jObject2 = await ApiClient.PostJsonAsync<JObject>(BaseUrl.GetUrl("/pub/api/v2/user/check/phone"), jObject);
			if (jObject2 == null)
			{
				return null;
			}
			if (jObject2.TryGetValue("exists", out JToken value))
			{
				return value.ToString().ToLower();
			}
		}
		catch (ApiException ex)
		{
			return ex.ErrorCode;
		}
		return null;
	}

	public static async Task<string> CheckEmailSigned(string email, string verKey, string verCode)
	{
		JObject jObject = new JObject();
		jObject.Add("email", email);
		if (!string.IsNullOrEmpty(verKey))
		{
			jObject.Add("verCode", verCode);
			jObject.Add("verKey", verKey);
		}
		try
		{
			JObject jObject2 = await ApiClient.PostJsonAsync<JObject>(BaseUrl.GetUrl("/pub/api/v2/user/check/email"), jObject);
			if (jObject2 == null)
			{
				return null;
			}
			if (jObject2.TryGetValue("exists", out JToken value))
			{
				return value.ToString().ToLower();
			}
		}
		catch (ApiException ex)
		{
			return ex.ErrorCode;
		}
		return null;
	}

	public static async Task<(string, int)> SendMfaVerifyCode(string method, string name, int type, string vid, bool isSign)
	{
		JObject jObject = new JObject();
		jObject.Add("method", method);
		if (!isSign)
		{
			jObject.Add("name", name);
			jObject.Add("type", type);
		}
		try
		{
			JToken value;
			return (await ApiClient.PostJsonAsync<JObject>(BaseUrl.GetUrl(isSign ? "/api/v2/user/sign/mfa/code" : "/api/v2/user/mfa/code"), jObject, isSign ? ApiClient.GetHttpClient(new List<(string, string)> { ("x-verify-id", vid) }) : null)).TryGetValue("oid", out value) ? (value.ToString(), 0) : ("error", -1);
		}
		catch (ApiException ex)
		{
			long num = -1L;
			if (ex.Data.Contains("count"))
			{
				num = (ex.Data["count"] as long?) ?? (-1);
			}
			return (ex.ErrorCode, (int)num);
		}
	}

	public static async Task<string> SendVerifyCode(string phone, string captchaKey, string captcha)
	{
		JObject jObject = new JObject();
		jObject.Add("phone", phone);
		jObject.Add("type", 1);
		if (!string.IsNullOrEmpty(captchaKey))
		{
			jObject.Add("verCode", captcha);
			jObject.Add("verKey", captchaKey);
		}
		try
		{
			return (await ApiClient.PostJsonSuccessAsync(BaseUrl.GetUrl("/api/v2/user/sms/signup/code"), jObject)) ? null : "error";
		}
		catch (ApiException ex)
		{
			return ex.ErrorCode;
		}
	}

	public static async Task<(string, string)> GetCaptcha()
	{
		try
		{
			JObject jObject = await ApiClient.GetJsonAsync<JObject>(BaseUrl.GetUrl("/pub/captcha"));
			if (jObject.TryGetValue("k", out JToken value) && jObject.TryGetValue("img", out JToken value2))
			{
				return (value.ToString(), value2.ToString());
			}
		}
		catch (ApiException ex)
		{
			return (ex.ErrorCode, ex.ErrorMessage);
		}
		return (null, null);
	}

	public static async Task<string> GetMailTaskCode(bool isNew)
	{
		_ = 1;
		try
		{
			if (!isNew)
			{
				return await ApiClient.GetJsonAsync<string>(BaseUrl.GetUrl("/api/v2/settings/mail/task/code"));
			}
			return await ApiClient.PostJsonAsync<string>(BaseUrl.GetUrl("/api/v2/settings/mail/task/code/new"), null);
		}
		catch (ApiException)
		{
			return null;
		}
	}

	public static async Task<(TelegramBindCodeDto dto, string errorCode)> GetTelegramBindCode()
	{
		try
		{
			return (dto: await ApiClient.PostJsonAsync<TelegramBindCodeDto>(BaseUrl.GetUrl("/api/v2/third/telegram/bind/code"), null), errorCode: null);
		}
		catch (ApiException ex)
		{
			return (dto: null, errorCode: ex.ErrorCode);
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			return (dto: null, errorCode: "error");
		}
	}

	public static async Task<string> CloseOrOpenProject(string projectId, bool close)
	{
		try
		{
			return (await ApiClient.PostJsonSuccessAsync(BaseUrl.GetUrl(string.Format("/api/v2/project/{0}/close?closed={1}&toAll={2}", projectId, close.ToString().ToLower(), "true")), null)) ? null : "error";
		}
		catch (ApiException ex)
		{
			return ex.ErrorCode;
		}
	}

	public static async Task<MFAModel> GetMfaSettings(string vid, bool isSign)
	{
		try
		{
			return await ApiClient.GetJsonAsync<MFAModel>(BaseUrl.GetUrl(isSign ? "/api/v2/user/sign/mfa/setting" : "/api/v2/user/mfa/setting"), isSign ? ApiClient.GetHttpClient(new List<(string, string)> { ("x-verify-id", vid) }) : null);
		}
		catch (ApiException)
		{
			return null;
		}
	}

	public static async Task<UserModel> VerifyMfaAndSignOn(string oid, string method, string code, string authId)
	{
		JObject jObject = new JObject();
		jObject.Add("oid", oid);
		jObject.Add("method", method);
		jObject.Add("code", code);
		return await ApiClient.PostJsonAsync<UserModel>(BaseUrl.GetUrl("/api/v2/user/sign/mfa/code/verify"), jObject, ApiClient.GetHttpClient(new List<(string, string)> { ("x-verify-id", authId) }));
	}

	public static async Task<TagCompletedTasks> GetTagCompletedTasks(List<string> tags, string token, int limit)
	{
		JObject jObject = new JObject();
		jObject.Add("tags", new JArray(tags));
		jObject.Add("token", token);
		jObject.Add("limit", 200);
		try
		{
			TagCompletedTasks tagCompletedTasks = await ApiClient.PostJsonAsync<TagCompletedTasks>(BaseUrl.GetUrl("/api/v2/tag/completedTask"), jObject);
			if (tagCompletedTasks != null)
			{
				List<TaskModel> tasks = tagCompletedTasks.tasks;
				if (tasks.Count > 0)
				{
					foreach (TaskModel item in tasks)
					{
						TaskService.ClearDueDate(item);
						if (string.IsNullOrEmpty(item.userId))
						{
							item.userId = LocalSettings.Settings.LoginUserId;
						}
						if (item.tags != null)
						{
							item.tag = TagSerializer.ToJsonContent(item.tags.ToList());
						}
						if (item.exDate != null)
						{
							item.exDates = ExDateSerilizer.ToString(item.exDate);
						}
					}
				}
			}
			return tagCompletedTasks;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task PostNotionSync(string syncMode = "full")
	{
		JObject jsonObj = new JObject { { "syncMode", syncMode } };
		try
		{
			await ApiClient.PostJsonAsync<TagCompletedTasks>(BaseUrl.GetUrl("/api/v2/connect/notion"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public static async Task<bool?> CheckPaymentCompleted()
	{
		try
		{
			PaymentStatus paymentStatus = await ApiClient.GetJsonAsync<PaymentStatus>(BaseUrl.GetUrl("/api/v2/payment/check"));
			if (paymentStatus.status == "COMPLETED" || paymentStatus.status == "REQUEST_REFUND" || paymentStatus.status == "REQUEST_INVOICE" || paymentStatus.status == "INVOICED")
			{
				return true;
			}
			if (paymentStatus.status == "PENDING" || string.IsNullOrEmpty(paymentStatus.status))
			{
				return null;
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return false;
	}

	public static async Task<bool> NeedShowNotionPage()
	{
		try
		{
			return await ApiClient.GetJsonAsync<bool>(BaseUrl.GetUrl("/api/v2/page/show?type=1"));
		}
		catch (ApiException)
		{
			return false;
		}
	}

	public static async Task<PomoMonthChangeResultModel> CheckPomoRecChangedMonth(long checkPoint = 0L)
	{
		try
		{
			PomoMonthChangeResultModel pomoMonthChangeResultModel = await ApiClient.GetJsonAsync<PomoMonthChangeResultModel>(string.Format(BaseUrl.GetUrl("/api/v2/pomodoro/record/{0}"), checkPoint));
			if (pomoMonthChangeResultModel == null)
			{
				return null;
			}
			return pomoMonthChangeResultModel;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<PomoFocusRecQueryResultModel> QueryPomoRecWithFilter(PomoFocusRecFilterModel filter)
	{
		try
		{
			PomoFocusRecQueryResultModel pomoFocusRecQueryResultModel = await ApiClient.PostJsonAsync<PomoFocusRecQueryResultModel>(BaseUrl.GetUrl("/api/v2/pomodoro/filter"), filter);
			if (pomoFocusRecQueryResultModel == null)
			{
				return null;
			}
			return pomoFocusRecQueryResultModel;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<bool> DeleteMultiPomoRec(IEnumerable<string> pomodoroIds, IEnumerable<string> timingIds)
	{
		try
		{
			JObject jObject = new JObject();
			jObject.Add("pomodoroIds", new JArray(pomodoroIds));
			jObject.Add("timingIds", new JArray(timingIds));
			return await ApiClient.DeleteJsonAsync(BaseUrl.GetUrl("/api/v2/pomodoro"), jObject);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return false;
	}

	public static async Task<bool> DeleteAllPomoRec()
	{
		try
		{
			return await ApiClient.DeleteAsync(BaseUrl.GetUrl("/api/v2/pomodoro/all"));
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return false;
	}

	public static async Task<CountdownCollection> PullRemoteCountdowns()
	{
		string text = await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/countdown/list", "", null, LocalSettings.Settings.LoginUserAuth, "GET");
		CountdownCollection result = null;
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				result = JsonConvert.DeserializeObject<CountdownCollection>(text, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				});
			}
			catch (Exception ex)
			{
				string text2 = ExceptionUtils.BuildExceptionMessage(ex);
				if (App.IsAdmin)
				{
					text2 = text2 + "\r\n" + text;
				}
				UtilLog.Warn(text2);
			}
		}
		return result;
	}

	public static async Task<BatchUpdateResult> UpdateCountdowns(SyncCountdownBean countdownBean)
	{
		return SafeConvertResult<BatchUpdateResult>(await ticktick_WPF.Util.Network.Network.GetHttpWebRequest("/api/v2/countdown/batch", JsonConvert.SerializeObject(countdownBean), null, LocalSettings.Settings.LoginUserAuth));
	}

	public static async Task<string> UpdateCountdown(SyncCountdownBean syncBean)
	{
		return null;
	}

	public static async Task<string> UploadCountdownBackground(string filePath)
	{
		return null;
	}

	public static async Task<bool> DownloadCountdownBackground(string countdownId, string attachmentId, string savePath)
	{
		return true;
	}

	public static async Task<string> GetUserRegTime()
	{
		return await ApiClient.GetJsonAsync<string>(BaseUrl.GetUrl("/api/v2/user/regTime"));
	}

	public static async Task<AIAgentTokenUsageDto> GetAIAgentTokenUsage()
	{
		return (await ApiClient.GetJsonAsync<AIAgentTokenUsageDto>(BaseUrl.GetAIAgentUrl("/api/v1/token-usage/current"))) ?? new AIAgentTokenUsageDto();
	}

	public static async Task<AIAgentPrivacySettingDto> GetAIAgentPrivacySetting()
	{
		return (await ApiClient.GetJsonAsync<AIAgentPrivacySettingDto>(BaseUrl.GetAIAgentUrl("/api/v1/user-settings/privacy"))) ?? new AIAgentPrivacySettingDto();
	}

	public static async Task<AIAgentPrivacySettingDto> UpdateAIAgentPrivacySetting(bool isEnabled)
	{
		try
		{
			return (await ApiClient.PutJsonAsync<AIAgentPrivacySettingDto>(BaseUrl.GetAIAgentUrl("/api/v1/user-settings/privacy"), new AIAgentPrivacySettingUpdateDto
			{
				DataCollectionOptIn = isEnabled
			})) ?? new AIAgentPrivacySettingDto
			{
				DataCollectionOptIn = isEnabled
			};
		}
		catch (ApiException)
		{
			try
			{
				AIAgentPrivacySettingDto aIAgentPrivacySettingDto = await GetAIAgentPrivacySetting();
				if (aIAgentPrivacySettingDto.DataCollectionOptIn.HasValue && aIAgentPrivacySettingDto.IsEnabled == isEnabled)
				{
					return aIAgentPrivacySettingDto;
				}
			}
			catch (Exception ex2)
			{
				UtilLog.Info("AI privacy settings verification failed: " + ex2);
			}
			throw;
		}
	}

	public static async Task PostAITaskScene(AIUserInputDto dto, BlockingCollection<string> collection, CancellationToken? ct)
	{
		AITaskSceneInputDto value = new AITaskSceneInputDto
		{
			userInput = JsonConvert.SerializeObject(dto)
		};
		await ApiClient.PostJsonStreamAsync(BaseUrl.GetUrl("/ai/api/v2/task/scene") + "?stream=true", JsonConvert.SerializeObject(value), collection, ct);
	}

	public static async Task PostAITaskAdviceStream(AITaskAdviceInputDto dto, BlockingCollection<string> collection, CancellationToken? ct)
	{
		await ApiClient.PostJsonStreamAsync(BaseUrl.GetUrl("/ai/api/v2/task/advice") + "?stream=true", JsonConvert.SerializeObject(dto), collection, ct);
	}

	public static async Task PostAITaskDecompStream(AITaskDecompInputDto dto, BlockingCollection<string> collection, CancellationToken? ct)
	{
		await ApiClient.PostJsonStreamAsync(BaseUrl.GetUrl("/ai/api/v2/task/decomposition") + "?stream=true", JsonConvert.SerializeObject(dto), collection, ct);
	}

	public static async Task PostAITaskImproveStream(AITaskImproveInputDto dto, BlockingCollection<string> collection, CancellationToken? ct)
	{
		await ApiClient.PostJsonStreamAsync(BaseUrl.GetUrl("/ai/api/v2/task/improve") + "?stream=true", JsonConvert.SerializeObject(dto), collection, ct);
	}

	public static async Task<Dictionary<string, int>> GetTaskActivityCount(IEnumerable<string> taskIds)
	{
		try
		{
			var jsonObj = new
			{
				taskIds = taskIds,
				action = "T_DUE"
			};
			return await ApiClient.PostJsonAsync<Dictionary<string, int>>(BaseUrl.GetUrl("/api/v2/task/activity/count"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<Dictionary<string, int>> GetAllTaskActivityCount()
	{
		try
		{
			var jsonObj = new
			{
				action = "T_DUE"
			};
			return await ApiClient.PostJsonAsync<Dictionary<string, int>>(BaseUrl.GetUrl("/api/v2/task/activity/count/all"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<List<AIRecommendPropertyDto>> AIRecommendUpload(List<AIRecommendInputDto> input)
	{
		try
		{
			var jsonObj = new
			{
				tasks = input
			};
			return await ApiClient.PostJsonAsync<List<AIRecommendPropertyDto>>(BaseUrl.GetAIUrl("/ai/api/v2/task/recommend"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<(AITaskIdDto dto, string errorCode)> AIRecommendUploadToActQueue(List<AIRecommendInputDto> input)
	{
		try
		{
			var jsonObj = new
			{
				tasks = input
			};
			return (dto: await ApiClient.PostJsonAsync<AITaskIdDto>(BaseUrl.GetAIUrl("/ai/api/v2/task/recommend/task"), jsonObj), errorCode: null);
		}
		catch (ApiException ex)
		{
			return (dto: null, errorCode: ex.ErrorCode);
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			return (dto: null, errorCode: null);
		}
	}

	public static async Task<(CommonDto<List<AIRecommendPropertyDto>> dto, string errorCode)> AIRecommendQueryActResult(string taskId)
	{
		try
		{
			return (dto: await ApiClient.GetJsonAsync<CommonDto<List<AIRecommendPropertyDto>>>(BaseUrl.GetAIUrl("/ai/api/v2/task/recommend/task?taskId=" + taskId)), errorCode: null);
		}
		catch (ApiException ex)
		{
			return (dto: null, errorCode: ex.ErrorCode);
		}
		catch (Exception ex2)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex2));
			return (dto: null, errorCode: null);
		}
	}

	public static async Task<List<AIRecommendHistoryDto>> AIRecommendHistory(IEnumerable<string> taskIds)
	{
		try
		{
			var jsonObj = new { taskIds };
			return await ApiClient.PostJsonAsync<List<AIRecommendHistoryDto>>(BaseUrl.GetAIUrl("/ai/api/v2/task/recommend/history"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<(AIVoiceTranscriptDto dto, string errorCode)> AIVoiceTranscript(string filePath, string attachmentId)
	{
		try
		{
			return (dto: await ApiClient.PostFormAsync<AIVoiceTranscriptDto>(BaseUrl.GetAIUrl("/ai/api/v2/voice/task/transcriptions"), new(string, string, bool)[2]
			{
				("file", filePath, true),
				("attId", attachmentId, false)
			}), errorCode: null);
		}
		catch (ApiException ex)
		{
			if (!string.IsNullOrEmpty(ex.ErrorCode))
			{
				return (dto: null, errorCode: ex.ErrorCode);
			}
			return (dto: null, errorCode: ex.Error?.StatusCode.ToString());
		}
		catch (HttpRequestException)
		{
			return (dto: null, errorCode: "no_network");
		}
		catch (OperationCanceledException)
		{
			return (dto: null, errorCode: "time_out");
		}
		catch (Exception ex4)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex4));
		}
		return (dto: null, errorCode: null);
	}

	public static async Task<(AITaskIdDto dto, string errorCode)> AIVoiceTranscriptTask(string attachmentId, string projectId, string taskId)
	{
		try
		{
			return (dto: await ApiClient.PostFormAsync<AITaskIdDto>(BaseUrl.GetAIUrl("/ai/api/v2/voice/task/transcriptions/task"), new(string, string, bool)[3]
			{
				("attId", attachmentId, false),
				("projectId", projectId, false),
				("taskId", taskId, false)
			}), errorCode: null);
		}
		catch (ApiException ex)
		{
			if (!string.IsNullOrEmpty(ex.ErrorCode))
			{
				return (dto: null, errorCode: ex.ErrorCode);
			}
			return (dto: null, errorCode: ex.Error?.StatusCode.ToString());
		}
		catch (HttpRequestException)
		{
			return (dto: null, errorCode: "no_network");
		}
		catch (OperationCanceledException)
		{
			return (dto: null, errorCode: "time_out");
		}
		catch (Exception ex4)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex4));
		}
		return (dto: null, errorCode: null);
	}

	public static async Task<(CommonDto<AIVoiceTranscriptDto> dto, string errorCode)> AIVoiceTranscriptQueryTaskResult(string taskId)
	{
		try
		{
			return (dto: await ApiClient.GetJsonAsync<CommonDto<AIVoiceTranscriptDto>>(BaseUrl.GetAIUrl("/ai/api/v2/voice/task/transcriptions/task?taskId=" + taskId)), errorCode: null);
		}
		catch (ApiException ex)
		{
			if (!string.IsNullOrEmpty(ex.ErrorCode))
			{
				return (dto: null, errorCode: ex.ErrorCode);
			}
			return (dto: null, errorCode: ex.Error?.StatusCode.ToString());
		}
		catch (HttpRequestException)
		{
			return (dto: null, errorCode: "no_network");
		}
		catch (OperationCanceledException)
		{
			return (dto: null, errorCode: "time_out");
		}
		catch (Exception ex4)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex4));
		}
		return (dto: null, errorCode: null);
	}

	public static async Task<(AIVoiceSummaryDto dto, string errorCode)> AIVoiceSummary(string text, string attachmentId)
	{
		try
		{
			var jsonObj = new
			{
				text = text,
				attId = attachmentId
			};
			return (dto: await ApiClient.PostJsonAsync<AIVoiceSummaryDto>(BaseUrl.GetAIUrl("/ai/api/v2/voice/task/summary"), jsonObj, ApiClient.GetUploadClient()), errorCode: null);
		}
		catch (ApiException ex)
		{
			if (!string.IsNullOrEmpty(ex.ErrorCode))
			{
				return (dto: null, errorCode: ex.ErrorCode);
			}
			return (dto: null, errorCode: ex.Error?.StatusCode.ToString());
		}
		catch (HttpRequestException)
		{
			return (dto: null, errorCode: "no_network");
		}
		catch (OperationCanceledException)
		{
			return (dto: null, errorCode: "time_out");
		}
		catch (Exception ex4)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex4));
		}
		return (dto: null, errorCode: null);
	}

	public static async Task<List<AIQueryGenerResultDto>> AIQueryGenerResult(string objId, string genType)
	{
		try
		{
			return await ApiClient.GetJsonAsync<List<AIQueryGenerResultDto>>(BaseUrl.GetAIUrl("/ai/api/v2/generated?objId=" + objId + (string.IsNullOrEmpty(genType) ? null : ("&genType=" + genType))));
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<List<AIQueryGenerResultDto>> AIQueryMultiGenerResult(string genType, params string[] objIds)
	{
		try
		{
			var jsonObj = new { objIds, genType };
			return await ApiClient.PostJsonAsync<List<AIQueryGenerResultDto>>(BaseUrl.GetAIUrl("/ai/api/v2/generated"), jsonObj);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	public static async Task<string> GetUrlTitle(string inputUrl)
	{
		try
		{
			string text = await ApiClient.GetAsync(BaseUrl.GetExtUrl($"/pageTitle?url={inputUrl}"));
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (CheckIfJson(text))
			{
				return null;
			}
			return text;
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		return null;
	}

	private static bool CheckIfJson(string content)
	{
		try
		{
			JToken.Parse(content);
			return true;
		}
		catch (JsonReaderException)
		{
			return false;
		}
	}
}
