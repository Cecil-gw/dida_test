using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Setting;

public class AIFeaturesConfig : UserControl, IComponentConnector
{
	private const string DidaMcpUrl = "https://help.dida365.com/articles/7438132116019216384";

	private const string DidaCliUrl = "https://help.dida365.com/articles/7464976698707017728";

	private const string TickMcpUrl = "https://help.ticktick.com/articles/7438129581631995904";

	private const string TickCliUrl = "https://help.ticktick.com/articles/7465251130025443328";

	private int _loadVersion;

	private bool _isUpdatingPrivacy;

	internal StackPanel UsagePanel;

	internal TextBlock UsageDescriptionText;

	internal TextBlock PrivacyTitle;

	internal Border ImprovePrivacyCard;

	internal CheckBox ImprovePrivacyCheckBox;

	internal TextBlock McpDescText;

	internal TextBlock CliTitleText;

	internal TextBlock CliDescText;

	private bool _contentLoaded;

	public AIFeaturesConfig()
	{
		InitializeComponent();
		InitData();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	private void InitData()
	{
		string text = Utils.GetAppName();
		if (string.IsNullOrEmpty(text))
		{
			text = (Utils.IsDida() ? "DIDA" : "TickTick");
		}
		McpDescText.Text = FormatDescription("MCPServersDesc", text);
		CliTitleText.Text = (Utils.IsDida() ? "DIDA CLI" : "TickTick CLI");
		CliDescText.Text = FormatDescription("CliDesc", text);
		UsagePanel.Visibility = ((!UserDao.IsPro()) ? Visibility.Collapsed : Visibility.Visible);
		ImprovePrivacyCheckBox.IsChecked = true;
		UpdatePrivacyVisibility();
	}

	private void UpdatePrivacyVisibility()
	{
		Visibility visibility = ((!LocalSettings.Settings.EnableAI) ? Visibility.Collapsed : Visibility.Visible);
		PrivacyTitle.Visibility = visibility;
		ImprovePrivacyCard.Visibility = visibility;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		int loadVersion = ++_loadVersion;
		bool flag = UserDao.IsPro();
		UsagePanel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		UpdatePrivacyVisibility();
		Task task = (flag ? LoadUsageAsync(loadVersion) : Task.CompletedTask);
		Task task2 = (LocalSettings.Settings.EnableAI ? LoadPrivacyAsync(loadVersion) : Task.CompletedTask);
		await Task.WhenAll(task, task2);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_loadVersion++;
	}

	private async Task LoadUsageAsync(int loadVersion)
	{
		UsageDescriptionText.Text = "—";
		try
		{
			AIAgentTokenUsageDto usage = await Communicator.GetAIAgentTokenUsage();
			if (base.IsLoaded && loadVersion == _loadVersion)
			{
				UsageDescriptionText.Text = FormatUsageDescription(usage);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("AI usage settings request failed: " + ex.Message);
			if (base.IsLoaded && loadVersion == _loadVersion)
			{
				UsageDescriptionText.Text = Utils.GetString("LoadFailed");
			}
		}
	}

	private async Task LoadPrivacyAsync(int loadVersion)
	{
		try
		{
			AIAgentPrivacySettingDto aIAgentPrivacySettingDto = await Communicator.GetAIAgentPrivacySetting();
			if (base.IsLoaded && loadVersion == _loadVersion && !_isUpdatingPrivacy)
			{
				ImprovePrivacyCheckBox.IsChecked = aIAgentPrivacySettingDto.IsEnabled;
			}
		}
		catch (Exception ex)
		{
			UtilLog.Info("AI privacy settings request failed: " + ex.Message);
		}
	}

	private static string FormatUsageDescription(AIAgentTokenUsageDto usage)
	{
		AIAgentTokenUsageDto aIAgentTokenUsageDto = usage ?? new AIAgentTokenUsageDto();
		string text = string.Format(Utils.GetString("AIUsageUsed"), aIAgentTokenUsageDto.DisplayPercentage);
		if (!aIAgentTokenUsageDto.UpdatedAt.HasValue)
		{
			return text;
		}
		DateTime localDateTime = aIAgentTokenUsageDto.UpdatedAt.Value.LocalDateTime;
		string text2 = DateUtils.FormatMonthDay(localDateTime, DateFormatLen.Middle);
		string hourMinuteText = DateUtils.GetHourMinuteText(localDateTime);
		string text3 = string.Format(Utils.GetString("AIUpdateOn"), text2 + ", " + hourMinuteText);
		return text + " (" + text3 + ")";
	}

	private async void OnImprovePrivacyClick(object sender, RoutedEventArgs e)
	{
		if (_isUpdatingPrivacy)
		{
			return;
		}
		bool requestedValue = ImprovePrivacyCheckBox.IsChecked == true;
		_isUpdatingPrivacy = true;
		ImprovePrivacyCheckBox.IsEnabled = false;
		try
		{
			AIAgentPrivacySettingDto aIAgentPrivacySettingDto = await Communicator.UpdateAIAgentPrivacySetting(requestedValue);
			ImprovePrivacyCheckBox.IsChecked = aIAgentPrivacySettingDto.IsEnabled;
		}
		catch (Exception ex)
		{
			UtilLog.Info("AI privacy settings update failed: " + ex);
			ImprovePrivacyCheckBox.IsChecked = !requestedValue;
			Utils.FindParent<SettingDialog>(this)?.Toast(Utils.GetString("SaveFailed"));
		}
		finally
		{
			_isUpdatingPrivacy = false;
			ImprovePrivacyCheckBox.IsEnabled = true;
		}
	}

	private static string FormatDescription(string key, string productName)
	{
		string text = Utils.GetString(key);
		if (!string.IsNullOrEmpty(text))
		{
			return string.Format(text, productName);
		}
		return string.Empty;
	}

	private void OnMcpClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/articles/7438132116019216384" : "https://help.ticktick.com/articles/7438129581631995904");
	}

	private void OnCliClick(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl(Utils.IsDida() ? "https://help.dida365.com/articles/7464976698707017728" : "https://help.ticktick.com/articles/7465251130025443328");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/aifeaturesconfig.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			UsagePanel = (StackPanel)target;
			break;
		case 2:
			UsageDescriptionText = (TextBlock)target;
			break;
		case 3:
			PrivacyTitle = (TextBlock)target;
			break;
		case 4:
			ImprovePrivacyCard = (Border)target;
			break;
		case 5:
			ImprovePrivacyCheckBox = (CheckBox)target;
			ImprovePrivacyCheckBox.Click += OnImprovePrivacyClick;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnMcpClick;
			break;
		case 7:
			McpDescText = (TextBlock)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += OnCliClick;
			break;
		case 9:
			CliTitleText = (TextBlock)target;
			break;
		case 10:
			CliDescText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
