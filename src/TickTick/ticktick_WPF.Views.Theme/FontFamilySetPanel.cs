using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class FontFamilySetPanel : UserControl, IComponentConnector, IStyleConnector
{
	private List<FontFamilyViewModel> _models;

	private FontFamilyViewModel _currentHover;

	private bool _switching;

	internal TextBlock Text;

	internal ItemsControl FontFamilyItems;

	internal Popup AuthPopup;

	internal Run AuthRun1;

	internal Run AuthRun2;

	private bool _contentLoaded;

	public FontFamilySetPanel(List<FontFamilyViewModel> models)
	{
		InitializeComponent();
		_models = models;
		FontFamilyItems.ItemsSource = models;
		base.Loaded += delegate
		{
			DataChangedNotifier.IsDarkChanged += OnIsDarkChanged;
			OnIsDarkChanged(null, null);
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.IsDarkChanged -= OnIsDarkChanged;
		};
	}

	private void OnIsDarkChanged(object sender, EventArgs e)
	{
		_models.ForEach(delegate(FontFamilyViewModel m)
		{
			m.ChangeImage();
		});
	}

	private async void OnFontFamilySelect(object sender, MouseButtonEventArgs e)
	{
		if (!IsSwitching() && sender is FrameworkElement { DataContext: FontFamilyViewModel dataContext })
		{
			TrySetFontFamily(dataContext);
		}
	}

	private bool IsSwitching()
	{
		bool switching = _switching;
		if (!_switching)
		{
			_switching = true;
		}
		return switching;
	}

	private async Task TrySetFontFamily(FontFamilyViewModel model)
	{
		try
		{
			if (!model.Selected && (!model.NeedPro || ProChecker.CheckPro(ProType.Font)))
			{
				model.Loading = true;
				string text = await FontFamilyUtils.DownLoadFontFile(model.Id);
				if (!string.IsNullOrEmpty(text))
				{
					Utils.FindParent<AppearanceControl>(this)?.Toast(text);
				}
				model.Loading = false;
				LocalSettings.Settings.ExtraSettings.AppFontFamily = model.Id;
				if (!FontFamilyUtils.NewDownloadFont.Contains(model.Id))
				{
					App.Instance.LoadFontResource();
					DataChangedNotifier.NotifyFontFamilyChanged();
				}
				_models.ForEach(delegate(FontFamilyViewModel m)
				{
					m.Selected = false;
				});
				model.Selected = true;
				string fontEventName = FontFamilyUtils.GetFontEventName(model.Id);
				if (!string.IsNullOrEmpty(fontEventName))
				{
					UserActCollectUtils.AddClickEvent("settings", "fonts", fontEventName);
				}
				model.Loading = false;
			}
		}
		finally
		{
			_switching = false;
		}
	}

	private bool CheckFileMd5(string path, string fontId)
	{
		if (File.Exists(path))
		{
			return new FileInfo(path).Length > 10000000;
		}
		return false;
	}

	private void OnAuthClick(object sender, MouseButtonEventArgs e)
	{
		if (_currentHover != null)
		{
			string fontCopyRightLink = FontFamilyUtils.GetFontCopyRightLink(_currentHover.Id);
			if (!string.IsNullOrEmpty(fontCopyRightLink))
			{
				Utils.TryProcessStartUrl(fontCopyRightLink);
			}
		}
	}

	private void OnCopyRightMouseLeave(object sender, MouseEventArgs e)
	{
		if (AuthPopup.IsMouseOver || !AuthPopup.IsOpen)
		{
			return;
		}
		AuthPopup.IsOpen = false;
		if (_currentHover != null)
		{
			if (sender.Equals(AuthPopup))
			{
				_currentHover.ShowCopyRight = false;
			}
			_currentHover = null;
		}
	}

	private void OnCopyRightMouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: FontFamilyViewModel dataContext } frameworkElement)
		{
			string fontAuth = FontFamilyUtils.GetFontAuth(dataContext.Id);
			if (!string.IsNullOrEmpty(fontAuth) && !AuthPopup.IsOpen)
			{
				_currentHover = dataContext;
				AuthRun1.Text = (dataContext.ShowAuthor() ? (Utils.GetString("Author") + ": ") : string.Empty);
				AuthRun2.Text = fontAuth;
				AuthPopup.PlacementTarget = frameworkElement;
				AuthPopup.IsOpen = true;
			}
		}
	}

	private void HoverBorderMouseEnter(object sender, MouseEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: FontFamilyViewModel dataContext })
		{
			dataContext.ShowCopyRight = !string.IsNullOrEmpty(FontFamilyUtils.GetFontAuth(dataContext.Id));
		}
	}

	private void HoverBorderMouseLeave(object sender, MouseEventArgs e)
	{
		if (!AuthPopup.IsOpen && sender is FrameworkElement { DataContext: FontFamilyViewModel dataContext })
		{
			dataContext.ShowCopyRight = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/fontfamilysetpanel.xaml", UriKind.Relative);
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
			Text = (TextBlock)target;
			break;
		case 2:
			FontFamilyItems = (ItemsControl)target;
			break;
		case 5:
			AuthPopup = (Popup)target;
			AuthPopup.MouseLeave += OnCopyRightMouseLeave;
			break;
		case 6:
			AuthRun1 = (Run)target;
			break;
		case 7:
			AuthRun2 = (Run)target;
			AuthRun2.MouseLeftButtonUp += OnAuthClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 3:
			((Border)target).MouseLeftButtonUp += OnFontFamilySelect;
			((Border)target).MouseEnter += HoverBorderMouseEnter;
			((Border)target).MouseLeave += HoverBorderMouseLeave;
			break;
		case 4:
			((Border)target).MouseLeave += OnCopyRightMouseLeave;
			((Border)target).MouseEnter += OnCopyRightMouseEnter;
			break;
		}
	}
}
