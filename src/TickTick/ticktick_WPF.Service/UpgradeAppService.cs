using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Update;

namespace ticktick_WPF.Service;

internal class UpgradeAppService
{
	private static bool isCheckingUpdate { get; set; }

	private static bool isShowingNews { get; set; }

	public static async Task CheckAutoUpdate(ImportantVersionModel data, bool fromConfigChange = false)
	{
		if (isCheckingUpdate)
		{
			return;
		}
		try
		{
			isCheckingUpdate = true;
			if (fromConfigChange)
			{
				return;
			}
			ImportantVersionModel.Windows windows = data.windows;
			if (windows == null || !windows.versionCode.HasValue || Utils.VersionNumToDouble() >= data.windows.versionCode.Value || LocalSettings.Settings.ExtraSettings.LastUpdateNotifyImportantVersion == data.windows.versionCode.Value.ToString())
			{
				return;
			}
			Task.Run(async delegate
			{
				for (int i = 0; i < 10; i++)
				{
					if (App.Window != null)
					{
						UpdateModel updateModel = await UpdateHelper.CheckUpdate();
						if (updateModel != null)
						{
							string importantVersion = data.windows.versionCode.Value.ToString();
							MainWindow.ShowNewVersionWindow(updateModel, force: false, App.Window, null, delegate
							{
								if (!(LocalSettings.Settings.ExtraSettings.LastUpdateNotifyImportantVersion == importantVersion))
								{
									LocalSettings.Settings.ExtraSettings.LastUpdateNotifyImportantVersion = importantVersion;
									LocalSettings.Settings.Save(force: true);
								}
							});
							LocalSettings.Settings.ExtraSettings.LastUpdateRedDotVersion = null;
							LocalSettings.Settings.ExtraSettings.LastUpdateRedDotNewVersion = null;
							LocalSettings.Settings.Save();
						}
						break;
					}
					await Task.Delay(1000);
				}
			});
		}
		catch (Exception ex)
		{
			UtilLog.Error("CheckAutoUpdate Exception, " + ex);
		}
		finally
		{
			isCheckingUpdate = false;
		}
	}

	public static async Task ShowVersionNews(ReleaseNoteDetailModel data)
	{
		if (isShowingNews)
		{
			return;
		}
		try
		{
			if (data == null || data.Epic == null)
			{
				return;
			}
			if (!double.TryParse(LocalSettings.Settings.ExtraSettings.LastShowReleaseNoteVersion, out var result) || result <= 0.0)
			{
				LocalSettings.Settings.ExtraSettings.LastShowReleaseNoteVersion = Utils.VersionNumToDouble().ToString();
				LocalSettings.Settings.Save();
			}
			else
			{
				if (!double.TryParse(data.VersionCode, out var result2) || Utils.VersionNumToDouble() < result2 || result >= result2)
				{
					return;
				}
				isShowingNews = true;
				Task.Run(async delegate
				{
					_ = 2;
					try
					{
						for (int i = 0; i < 10; i++)
						{
							if (App.Window != null)
							{
								if (!App.Window.IsInTutorial)
								{
									LocalSettings.Settings.ExtraSettings.LastShowReleaseNoteVersion = Utils.VersionNumToDouble().ToString();
									LocalSettings.Settings.Save();
									Directory.CreateDirectory(AppPaths.ImageDir);
									string lightImagePath = null;
									string darkImagePath = null;
									if (!string.IsNullOrEmpty(data.Epic.ImageUrl))
									{
										string filePath = Path.Combine(AppPaths.ImageDir, "epic_image_temp.png");
										await new FileDownloader(data.Epic.ImageUrl, filePath, null).StartDownloadAsync().ConfigureAwait(continueOnCapturedContext: false);
										lightImagePath = filePath;
									}
									if (!string.IsNullOrEmpty(data.Epic.DarkImageUrl))
									{
										string filePath = Path.Combine(AppPaths.ImageDir, "epic_image_dark_temp.png");
										await new FileDownloader(data.Epic.DarkImageUrl, filePath, null).StartDownloadAsync().ConfigureAwait(continueOnCapturedContext: false);
										darkImagePath = filePath;
									}
									if (ThemeUtil.IsDark() && !string.IsNullOrEmpty(darkImagePath))
									{
										data.Epic.ImageFilePath = darkImagePath;
									}
									else
									{
										data.Epic.ImageFilePath = lightImagePath;
									}
									Application.Current.Dispatcher.Invoke(delegate
									{
										VersionNewsWindow versionNewsWindow = new VersionNewsWindow(data);
										versionNewsWindow.Owner = App.Window;
										versionNewsWindow.Show();
									});
									UserActCollectUtils.AddClickEvent("release_note", "show", Utils.GetVersion());
								}
								break;
							}
							await Task.Delay(1000);
						}
					}
					catch (Exception ex2)
					{
						UtilLog.Error("ShowVersionNews Exception, " + ex2);
					}
					finally
					{
						isShowingNews = false;
					}
				});
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("ShowVersionNews Exception, " + ex);
			isShowingNews = false;
		}
	}

	public static async Task SaveUpdateFrom(AppUpdateFromType fromType)
	{
		LocalSettings.Settings.ExtraSettings.AppUpdateFrom = fromType;
		await LocalSettings.Settings.Save(force: true);
	}

	public static void SendUpdateFrom()
	{
		AppUpdateFromType? appUpdateFrom = LocalSettings.Settings.ExtraSettings.AppUpdateFrom;
		if (appUpdateFrom.HasValue)
		{
			string text = string.Empty;
			switch (appUpdateFrom)
			{
			case AppUpdateFromType.Notification:
				text = "update_notification";
				break;
			case AppUpdateFromType.Tabbar:
				text = "tabbar";
				break;
			case AppUpdateFromType.Setting:
				text = "setting";
				break;
			case AppUpdateFromType.Auto:
				text = "auto";
				break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				UserActCollectUtils.AddClickEvent("update", "update_from", text);
			}
			LocalSettings.Settings.ExtraSettings.AppUpdateFrom = null;
			LocalSettings.Settings.Save();
		}
	}
}
