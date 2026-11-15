using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class SelectSoundControl : UserControl, IComponentConnector, IStyleConnector
{
	private string _lastSelected;

	public Popup Popup;

	public List<PomoSoundViewModel> Models = new List<PomoSoundViewModel>
	{
		new PomoSoundViewModel("none"),
		new PomoSoundViewModel("Clock"),
		new PomoSoundViewModel("Stove"),
		new PomoSoundViewModel("Boiling"),
		new PomoSoundViewModel("TempleBlock"),
		new PomoSoundViewModel("Storm"),
		new PomoSoundViewModel("Rain"),
		new PomoSoundViewModel("Cafe"),
		new PomoSoundViewModel("MusicBox"),
		new PomoSoundViewModel("Morning"),
		new PomoSoundViewModel("Summer"),
		new PomoSoundViewModel("Chirp"),
		new PomoSoundViewModel("Forest"),
		new PomoSoundViewModel("Stream"),
		new PomoSoundViewModel("DeepSea"),
		new PomoSoundViewModel("Wave"),
		new PomoSoundViewModel("Desert"),
		new PomoSoundViewModel("StreetTraffic")
	};

	internal WrapPanel SoundPanel;

	internal DockPanel VolumePanel;

	internal Slider Slider;

	internal Grid ToastGrid;

	internal TextBlock ToastTextBlock;

	private bool _contentLoaded;

	public event EventHandler ItemSelect;

	public SelectSoundControl()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		foreach (PomoSoundViewModel model in Models)
		{
			SoundPanel.Children.Add(new ContentControl
			{
				DataContext = model
			});
		}
		if (Utils.IsWindows7())
		{
			VolumePanel.Visibility = Visibility.Collapsed;
			return;
		}
		Slider.Value = LocalSettings.Settings.ExtraSettings.FocusVolume / 10.0;
		Slider.ToolTip = (int)(Slider.Value * 10.0) + "%";
		Slider.ValueChanged -= OnSlideValueChanged;
		Slider.ValueChanged += OnSlideValueChanged;
	}

	private void OnSlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		LocalSettings.Settings.ExtraSettings.FocusVolume = e.NewValue * 10.0;
		Slider.ToolTip = (int)(e.NewValue * 10.0) + "%";
		PomoSoundPlayer.OnVolumeChanged();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		CheckSelected();
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: PomoSoundViewModel dataContext }))
		{
			return;
		}
		if (dataContext.Key != "none" && dataContext.Key != "Clock" && !UserDao.IsPro())
		{
			ProChecker.ShowUpgradeDialog(ProType.PomoSound);
			return;
		}
		_lastSelected = dataContext.Key;
		if (dataContext.Downloaded)
		{
			LocalSettings.Settings.PomoSound = dataContext.Key;
			PomoSoundPlayer.StartPlaySound(TickFocusManager.Status != PomoStatus.Working);
			SetModelSelected(dataContext);
			dataContext.Selected = true;
		}
		else if (dataContext.NeedDownload)
		{
			if (Utils.IsNetworkAvailable())
			{
				dataContext.TryDownload();
				dataContext.NotifyDownLoadCompleted += OnSoundDownLoaded;
			}
			else
			{
				ToastGrid.Visibility = Visibility.Visible;
				((Storyboard)FindResource("ToastShowAndHide")).Begin();
			}
		}
		this.ItemSelect?.Invoke(this, null);
	}

	private void OnStoryCompleted(object sender, EventArgs e)
	{
		ToastGrid.Visibility = Visibility.Collapsed;
	}

	private void SetModelSelected(PomoSoundViewModel model)
	{
		model.Selected = true;
		Models.ForEach(delegate(PomoSoundViewModel m)
		{
			if (m.Key != model.Key)
			{
				m.Selected = false;
			}
		});
	}

	private void OnSoundDownLoaded(object sender, string e)
	{
		if (e == _lastSelected)
		{
			LocalSettings.Settings.PomoSound = e;
			PomoSoundPlayer.StartPlaySound(TickFocusManager.Status != PomoStatus.Working);
			if (sender is PomoSoundViewModel modelSelected)
			{
				SetModelSelected(modelSelected);
			}
		}
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		if (Popup != null)
		{
			Popup.IsOpen = false;
		}
	}

	public string GetSelectedItem()
	{
		return Models.FirstOrDefault((PomoSoundViewModel m) => m.Selected)?.Key;
	}

	public void CheckSelected()
	{
		if (!(GetSelectedItem() != LocalSettings.Settings.PomoSound))
		{
			return;
		}
		Models.ForEach(delegate(PomoSoundViewModel m)
		{
			m.Selected = false;
		});
		PomoSoundViewModel pomoSoundViewModel = Models.FirstOrDefault((PomoSoundViewModel m) => m.Key == LocalSettings.Settings.PomoSound);
		if (pomoSoundViewModel != null)
		{
			if (pomoSoundViewModel.Downloaded)
			{
				pomoSoundViewModel.Selected = true;
				return;
			}
			Models[0].Selected = true;
			LocalSettings.Settings.PomoSound = "none";
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/selectsoundcontrol.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnStoryCompleted;
			break;
		case 2:
			SoundPanel = (WrapPanel)target;
			break;
		case 6:
			VolumePanel = (DockPanel)target;
			break;
		case 7:
			Slider = (Slider)target;
			break;
		case 8:
			ToastGrid = (Grid)target;
			break;
		case 9:
			ToastTextBlock = (TextBlock)target;
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
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 4:
			((Image)target).MouseLeftButtonUp += OnItemClick;
			break;
		case 5:
			((Border)target).MouseLeftButtonUp += OnItemClick;
			break;
		}
	}
}
