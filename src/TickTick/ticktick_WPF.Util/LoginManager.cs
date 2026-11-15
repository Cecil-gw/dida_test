using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Util;

public class LoginManager
{
	private static TTAsyncLocker<bool> _logoutLocker = new TTAsyncLocker<bool>();

	public static async Task<(UserModel, ApiException)> Login(string name, string pw, string verKey, string verCode)
	{
		_ = 5;
		try
		{
			UserModel user = await Communicator.SignOn(name, pw, verKey, verCode);
			if (user?.userId != null && user.userId != "")
			{
				bool isNew = await UserDao.UpdateOrInsertUserModelListDbAsync(user);
				await LocalSettings.ResetUserSettings(user, "mail_account");
				await UserManager.PullUserInfo();
				await UserActCollectUtils.OnDeviceDataChanged();
				await AppConfigManager.PerformPullAppConfig(login: true);
				UserManager.OnUserDbChange(user, isNew);
				JumpHelper.InitJumpList();
				JumpHelper.InitChangeEvents();
				return (user, null);
			}
			if (user?.authId != null)
			{
				return (user, null);
			}
		}
		catch (ApiException item)
		{
			return (null, item);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
			UserActCollectUtils.SendException(ex, ExceptionType.Task);
		}
		return (null, null);
	}

	public static async Task<(UserModel, ApiException)> VerifyMfa(string oid, string method, string code, string authId)
	{
		_ = 5;
		try
		{
			UserModel user = await Communicator.VerifyMfaAndSignOn(oid, method, code, authId);
			if (user?.userId != null && user.userId != "")
			{
				bool isNew = await UserDao.UpdateOrInsertUserModelListDbAsync(user);
				await LocalSettings.ResetUserSettings(user, "mail_account");
				await UserManager.PullUserInfo();
				await UserActCollectUtils.OnDeviceDataChanged();
				await AppConfigManager.PerformPullAppConfig(login: true);
				UserManager.OnUserDbChange(user, isNew);
				JumpHelper.InitJumpList();
				JumpHelper.InitChangeEvents();
				return (user, null);
			}
			if (user?.authId != null)
			{
				return (user, null);
			}
		}
		catch (ApiException item)
		{
			return (null, item);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
			UserActCollectUtils.SendException(ex, ExceptionType.Task);
		}
		return (null, null);
	}

	public static async Task<(UserModel, string)> SignUp(string name, string userName, string verifyText, string pw, bool isPhone, string verKey, string verCode)
	{
		UserModel user = await Communicator.Signup(name, userName, pw, verifyText, isPhone, verKey, verCode);
		if (user?.userId != null && user.userId != "")
		{
			App.IsRegisterSuccess = true;
			bool isNew = await UserDao.UpdateOrInsertUserModelListDbAsync(user);
			await LocalSettings.ResetUserSettings(user, "mail_account");
			await UserManager.PullUserInfo();
			await UserActCollectUtils.OnDeviceDataChanged();
			UserManager.OnUserDbChange(user, isNew);
			JumpHelper.InitJumpList();
			JumpHelper.InitChangeEvents();
			return (user, string.Empty);
		}
		try
		{
			string item = JObject.Parse(user?.username ?? string.Empty)["errorCode"]?.ToString();
			return (null, item);
		}
		catch (Exception)
		{
		}
		return (null, null);
	}

	public static async Task<bool> Logout()
	{
		return await _logoutLocker.RunAsync(async delegate
		{
			if (string.IsNullOrEmpty(LocalSettings.Settings.LoginUserAuth))
			{
				return false;
			}
			if (!(await TickFocusManager.ExitPomo()))
			{
				return false;
			}
			MainWindowManager.HandleLogout();
			JumpHelper.ClearJumpList();
			await LocalSettings.BeforeLogout();
			Utils.CleanAfterLogout();
			await Communicator.SignOut();
			await UserDao.AdjustCheckPointAfterLogout(Utils.GetCurrentUserIdInt().ToString());
			await Utils.LogotEmptyDb();
			UserManager.LastInitTime = DateTime.Now.AddMinutes(-2.0);
			await UserActCollectUtils.OnDeviceDataChanged();
			AppConfigManager.StopPolling();
			LocalSettings.ClearModel();
			return true;
		});
	}
}
