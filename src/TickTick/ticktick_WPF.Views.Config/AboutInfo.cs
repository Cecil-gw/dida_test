using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Update;

namespace ticktick_WPF.Views.Config;

public class AboutInfo : UserControl, IComponentConnector
{
	private ContextMenu _aiCompatibilityMenu;

	private bool _savingAICompatibility;

	internal Image LogoImage;

	internal Button UpdateButton;

	private bool _contentLoaded;

	private AboutInfoViewModel viewModel { get; set; }

	private bool isForceUpdate { get; set; }

	private AppUpdateFromType _appUpdateFromType { get; set; }

	internal void ShowAICompatibilityMenu()
	{
		if (_savingAICompatibility)
		{
			return;
		}
		ContextMenu aiCompatibilityMenu = _aiCompatibilityMenu;
		SettingDialog settingsWindow;
		ContextMenu menu;
		string restartHint;
		string backgroundHint;
		if (aiCompatibilityMenu == null || !aiCompatibilityMenu.IsOpen)
		{
			settingsWindow = Window.GetWindow(this) as SettingDialog;
			ExtraSettings settings = LocalSettings.Settings.ExtraSettings;
			menu = new ContextMenu
			{
				PlacementTarget = LogoImage,
				Placement = PlacementMode.Bottom
			};
			_aiCompatibilityMenu = menu;
			restartHint = (Utils.IsCn() ? "仅本机生效，修改后请重启应用。" : "Applies on this device. Restart the app after changing this setting.");
			backgroundHint = (Utils.IsCn() ? "仅本机生效，立即应用；取消勾选恢复透明背景。" : "Applies immediately on this device. Uncheck to restore transparency.");
			AddCompatibilityItem(Utils.IsCn() ? "AI 渲染兼容模式" : "AI rendering compatibility", () => settings.AIUseStandardWebView, delegate(bool value)
			{
				settings.AIUseStandardWebView = value;
			}, "standard_webview", requiresRestart: true);
			AddCompatibilityItem(Utils.IsCn() ? "AI 背景兼容模式（不透明）" : "AI background compatibility (opaque)", () => settings.AIOpaqueBrowserBackground, delegate(bool value)
			{
				settings.AIOpaqueBrowserBackground = value;
			}, "opaque_background", requiresRestart: false);
			menu.IsOpen = true;
		}
		void AddCompatibilityItem(string title, Func<bool> read, Action<bool> write, string logName, bool requiresRestart)
		{
			MenuItem item = new MenuItem
			{
				Header = title,
				IsCheckable = true,
				IsChecked = read(),
				ToolTip = (requiresRestart ? restartHint : backgroundHint)
			};
			item.Click += async delegate
			{
				if (_savingAICompatibility)
				{
					return;
				}
				_savingAICompatibility = true;
				bool previous = read();
				bool value = item.IsChecked;
				try
				{
					write(value);
					if (!requiresRestart)
					{
						AIWebAppView.NotifyBackgroundCompatibilityChanged();
					}
					await LocalSettings.Settings.Save(force: true);
					UtilLog.Info("AI compatibility setting: " + logName + "=" + value);
					settingsWindow?.Toast((!requiresRestart) ? (Utils.IsCn() ? "背景设置已应用。" : "Background setting applied.") : (Utils.IsCn() ? "设置已保存，请重启应用后使用 AI。" : "Setting saved. Restart the app before using AI."));
				}
				catch (Exception ex)
				{
					write(previous);
					item.IsChecked = previous;
					if (!requiresRestart)
					{
						try
						{
							AIWebAppView.NotifyBackgroundCompatibilityChanged();
						}
						catch (Exception ex2)
						{
							UtilLog.Info("Failed to restore AI background: " + ex2);
						}
					}
					UtilLog.Info("Failed to save AI compatibility setting: " + ex);
					CustomerDialog customerDialog = new CustomerDialog(Utils.IsCn() ? "AI 兼容设置" : "AI compatibility settings", Utils.IsCn() ? "设置应用或保存失败，请重试。" : "Could not apply or save the setting. Please try again.", MessageBoxButton.OK);
					customerDialog.Owner = Window.GetWindow(this);
					customerDialog.ShowDialog();
				}
				finally
				{
					_savingAICompatibility = false;
				}
			};
			menu.Items.Add(item);
		}
	}

	public AboutInfo(bool bySwitchClick)
	{
		InitializeComponent();
		base.DataContext = (viewModel = new AboutInfoViewModel());
		_appUpdateFromType = ((!bySwitchClick) ? AppUpdateFromType.Tabbar : AppUpdateFromType.Setting);
		InitData();
	}

	private async Task InitData()
	{
		UpdateModel updateModel = await UpdateHelper.CheckUpdate();
		if (updateModel != null)
		{
			viewModel.IsUpdateEnable = updateModel.versionNum > Utils.VersionNumToDouble();
			viewModel.NewVersionNum = Utils.ToVersion(updateModel.versionNum);
			ReleaseNoteModel releaseNoteModel = updateModel.releaseNote;
			if (releaseNoteModel == null)
			{
				releaseNoteModel = await Utils.GetReleaseNote();
			}
			ReleaseNoteModel releaseNoteModel2 = releaseNoteModel;
			string key = ((App.Ci.ToString() == "zh-CN") ? "zh_cn" : "en");
			NoteItemModel obj = releaseNoteModel2?.data.FirstOrDefault((NoteItemModel item) => item.lang == key);
			viewModel.NewVersionDescription = obj?.content;
			viewModel.ArcSize = releaseNoteModel2?.size;
			viewModel.DownloadUrl = updateModel.downLoadUri;
			isForceUpdate = updateModel.forceVersions != null && updateModel.forceVersions.Contains(Utils.GetVersion());
			LocalSettings.Settings.ExtraSettings.LastUpdateRedDotNewVersion = updateModel.versionNum.ToString();
		}
		LocalSettings.Settings.ExtraSettings.LastUpdateRedDotVersion = Utils.GetVersion();
		LocalSettings.Settings.Save();
	}

	private async void UpdateButton_Click(object sender, RoutedEventArgs e)
	{
		string package = AppPaths.PackageDir + viewModel.DownloadUrl.Split('/').Last();
		try
		{
			long valueOrDefault = ((!viewModel.DownloadUrl.Contains("x64")) ? viewModel.ArcSize?.x86 : viewModel.ArcSize?.x64).GetValueOrDefault();
			if (File.Exists(package) && new FileInfo(package).Length >= valueOrDefault)
			{
				await UpgradeAppService.SaveUpdateFrom(_appUpdateFromType);
				Process.Start(package);
				if (isForceUpdate)
				{
					App.Instance.Shutdown();
				}
			}
			else
			{
				DownloadWindow downloadWindow = new DownloadWindow(viewModel.DownloadUrl, valueOrDefault, _appUpdateFromType);
				if (isForceUpdate)
				{
					downloadWindow.SetForceUpdate();
					downloadWindow.ShowDialog();
				}
				else
				{
					downloadWindow.Show();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/aboutinfo.xaml", UriKind.Relative);
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
			LogoImage = (Image)target;
			break;
		case 2:
			UpdateButton = (Button)target;
			UpdateButton.Click += UpdateButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
