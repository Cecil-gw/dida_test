using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Setting;

public class GeneralSettings : UserControl, IComponentConnector
{
	internal Grid LanguageGrid;

	internal CustomSimpleComboBox LanguageComboBox;

	private bool _contentLoaded;

	private List<string> _languageKeys { get; } = new List<string>();

	public GeneralSettings()
	{
		InitializeComponent();
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "zh-CN", "简体中文" },
			{ "en-US", "English" },
			{ "zh-TW", "繁體中文" },
			{ "ja-JP", "日本語" },
			{ "ko-KR", "한국어" },
			{ "fr-FR", "Français" },
			{ "ru-RU", "Русский" },
			{ "pt-BR", "Português (Brasil)" }
		};
		_languageKeys = dictionary.Keys.ToList();
		LanguageComboBox.ItemsSource = dictionary.Values.ToList();
		InitData();
	}

	private void InitData()
	{
		GetLanguageUse();
	}

	private void GetLanguageUse()
	{
		LanguageComboBox.SelectedIndex = _languageKeys.IndexOf(LocalSettings.Settings.UserChooseLanguage);
	}

	private SettingDialog GetParent()
	{
		return Utils.FindParent<SettingDialog>(this);
	}

	private async void LanguageComboBoxSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		if (LanguageComboBox.SelectedIndex == -1)
		{
			return;
		}
		string userChooseLanguage = LocalSettings.Settings.UserChooseLanguage;
		LocalSettings.Settings.UserChooseLanguage = _languageKeys[LanguageComboBox.SelectedIndex];
		if (!(userChooseLanguage == LocalSettings.Settings.UserChooseLanguage))
		{
			if (new CustomerDialog(Utils.GetString("Language1"), Utils.GetString("ChangeLanguageContent"), Utils.GetString("Restart"), Utils.GetString("Cancel")).ShowDialog() == true)
			{
				await LocalSettings.Settings.Save();
				await UserActCollectUtils.OnDeviceDataChanged();
				GetParent()?.Close();
				App.Instance.Restart();
			}
			else
			{
				LocalSettings.Settings.UserChooseLanguage = userChooseLanguage;
				LanguageComboBox.SelectedIndex = _languageKeys.IndexOf(userChooseLanguage);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/generalsettings.xaml", UriKind.Relative);
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
			LanguageGrid = (Grid)target;
			break;
		case 2:
			LanguageComboBox = (CustomSimpleComboBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
