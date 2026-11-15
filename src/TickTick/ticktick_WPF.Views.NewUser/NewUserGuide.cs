using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.NewUser;

public class NewUserGuide : UserControl, IComponentConnector, IStyleConnector
{
	private readonly Dictionary<string, Geometry> GuideFeatures = new Dictionary<string, Geometry>
	{
		{
			Utils.GetString("Task"),
			Utils.GetGeometry("Ic.Tabbar.List")
		},
		{
			Utils.GetString("Calendar"),
			Utils.GetGeometry("Ic.Tabbar.Calendar")
		},
		{
			Utils.GetString("Matrix"),
			Utils.GetGeometry("Ic.Tabbar.Martix")
		},
		{
			Utils.GetString("PomoFocus"),
			Utils.GetGeometry("Ic.Tabbar.Focus")
		},
		{
			Utils.GetString("LoginTitleHabit"),
			Utils.GetGeometry("Ic.Tabbar.Habit")
		},
		{
			Utils.GetString("CountdownTitle"),
			Utils.GetGeometry("Ic.Tabbar.Countdown")
		},
		{
			Utils.GetString("AI"),
			Utils.GetGeometry("Ic.Tabbar.AI")
		}
	};

	public static readonly Dictionary<string, string> DefaultProjects = new Dictionary<string, string>
	{
		{
			Utils.GetString("PresetListWork"),
			"\ud83d\udcbc"
		},
		{
			Utils.GetString("PresetListMemo"),
			"\ud83c\udfe1"
		},
		{
			Utils.GetString("PresetListShopping"),
			"\ud83d\udce6"
		},
		{
			Utils.GetString("PresetListList"),
			"\ud83e\udd84"
		},
		{
			Utils.GetString("PresetListStudy"),
			"\ud83d\udcd6"
		},
		{
			Utils.GetString("PresetListExercise"),
			"\ud83c\udfc3"
		}
	};

	private int _pageIndex;

	private readonly List<string> _selectedFeatures = new List<string>
	{
		Utils.GetString("Task"),
		Utils.GetString("Calendar")
	};

	private readonly List<string> _selectedProjects = new List<string>();

	private int _showMessageRange;

	internal Image FeatureImage;

	internal ItemsControl SelectedFeatureItems;

	internal Grid ProjectGrid;

	internal Path TodayIcon;

	internal Path WeekIcon;

	internal Border EmptyList;

	internal ItemsControl SelectedProjectItems;

	internal Grid ChooseFeatureGrid;

	internal StackPanel BackIcon;

	internal EmjTextBlock PageTitle;

	internal ItemsControl ChooseFeatureItems;

	internal TextBlock MessageText;

	internal Rectangle BotRect;

	internal Ellipse BotEll;

	internal Button Continue;

	private bool _contentLoaded;

	public NewUserGuide()
	{
		if (Utils.IsWindows7())
		{
			GuideFeatures.Remove(Utils.GetString("AI"));
		}
		InitializeComponent();
		SetPage(0);
		TodayIcon.Data = Utils.GetGeometry("Ic.ProjectList.Date" + DateTime.Today.Day);
		WeekIcon.Data = Utils.GetGeometry("Ic.ProjectList." + DateTime.Today.DayOfWeek);
	}

	private void SetPage(int i)
	{
		MessageText.Visibility = Visibility.Collapsed;
		_pageIndex = i;
		if (_pageIndex == 0)
		{
			BackIcon.Visibility = Visibility.Collapsed;
			ProjectGrid.Visibility = Visibility.Collapsed;
			PageTitle.Text = "\ud83d\udc4b" + Utils.GetString("NewUserChooseFeature");
			InitChooseFeatures();
			Continue.Content = Utils.GetString("Continue");
			SwitchFeature(_selectedFeatures[0]);
			BotRect.SetValue(Grid.ColumnProperty, 0);
			BotEll.SetValue(Grid.ColumnProperty, 1);
		}
		else
		{
			ProjectGrid.Visibility = Visibility.Visible;
			BackIcon.Visibility = Visibility.Visible;
			PageTitle.Text = "\ud83d\udc4b" + Utils.GetString("NewUserChooseList");
			InitPresetProject();
			FeatureImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ImageSource/NewUserList.png"));
			Continue.Content = Utils.GetString("GetStartedApp");
			SetSelectedProject(_selectedProjects.LastOrDefault());
			BotRect.SetValue(Grid.ColumnProperty, 1);
			BotEll.SetValue(Grid.ColumnProperty, 0);
		}
	}

	private void InitChooseFeatures()
	{
		List<string> list = GuideFeatures.Keys.ToList();
		ObservableCollection<GuideChooseViewModel> observableCollection = new ObservableCollection<GuideChooseViewModel>();
		foreach (string item in list)
		{
			observableCollection.Add(new GuideChooseViewModel(item, GuideFeatures[item])
			{
				Selected = _selectedFeatures.Contains(item)
			});
		}
		ChooseFeatureItems.ItemsSource = observableCollection;
	}

	private void InitPresetProject()
	{
		List<string> list = DefaultProjects.Keys.ToList();
		ObservableCollection<GuideChooseViewModel> observableCollection = new ObservableCollection<GuideChooseViewModel>();
		foreach (string item in list)
		{
			observableCollection.Add(new GuideChooseViewModel(item, DefaultProjects[item])
			{
				Selected = _selectedProjects.Contains(item)
			});
		}
		ChooseFeatureItems.ItemsSource = observableCollection;
	}

	private void SetSelectedProject(string selected)
	{
		if (string.IsNullOrEmpty(selected))
		{
			EmptyList.Visibility = Visibility.Visible;
			SelectedProjectItems.ItemsSource = null;
		}
		else
		{
			EmptyList.Visibility = Visibility.Collapsed;
			ObservableCollection<GuideChooseViewModel> observableCollection = new ObservableCollection<GuideChooseViewModel>();
			foreach (string selectedProject in _selectedProjects)
			{
				observableCollection.Add(new GuideChooseViewModel(selectedProject, DefaultProjects[selectedProject])
				{
					Selected = (selected == selectedProject)
				});
			}
			SelectedProjectItems.ItemsSource = observableCollection;
		}
		SwitchFeature(_selectedFeatures[0], setImage: false);
	}

	private void OnSkipClick(object sender, MouseButtonEventArgs e)
	{
		App.Window.RemoveNewUserGuide(this);
	}

	private async void OnContinueClick(object sender, RoutedEventArgs e)
	{
		if (_pageIndex == 0)
		{
			SetPage(1);
			return;
		}
		await ProjectDao.InitProjects(_selectedProjects);
		List<string> list = GuideFeatures.Keys.ToList();
		DesktopTabBar desktopTabBar = new DesktopTabBar
		{
			bars = new List<TabBarModel>
			{
				new TabBarModel
				{
					name = "TASK",
					sortOrder = 0L,
					status = "active"
				},
				new TabBarModel
				{
					name = "CALENDAR",
					sortOrder = 1L,
					status = "active"
				},
				new TabBarModel
				{
					name = "SEARCH",
					sortOrder = _selectedFeatures.Count,
					status = "active"
				}
			},
			mtime = Utils.GetNowTimeStampInMills()
		};
		UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "task");
		UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "calendar");
		for (int i = 2; i < _selectedFeatures.Count; i++)
		{
			if (_selectedFeatures[i] == list[2])
			{
				desktopTabBar.bars.Add(new TabBarModel
				{
					name = "MATRIX",
					sortOrder = i,
					status = "active"
				});
				UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "enable_matrix");
			}
			if (_selectedFeatures[i] == list[3])
			{
				desktopTabBar.bars.Add(new TabBarModel
				{
					name = "POMO",
					sortOrder = i,
					status = "active"
				});
				UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "enable_pomo");
			}
			if (_selectedFeatures[i] == list[4])
			{
				desktopTabBar.bars.Add(new TabBarModel
				{
					name = "HABIT",
					sortOrder = i,
					status = "active"
				});
				UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "habit");
			}
			if (_selectedFeatures[i] == list[5])
			{
				desktopTabBar.bars.Add(new TabBarModel
				{
					name = "COUNTDOWN",
					sortOrder = i,
					status = "active"
				});
				UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "enable_countdown");
			}
			if (_selectedFeatures[i] == Utils.GetString("AI"))
			{
				desktopTabBar.bars.Add(new TabBarModel
				{
					name = "AI_ASSISTANT",
					sortOrder = i,
					status = "active"
				});
				UserActCollectUtils.AddClickEvent("newuser_guide", "tab_bar_enable", "enable_ai");
			}
		}
		foreach (string selectedProject in _selectedProjects)
		{
			string value;
			string data = (DefaultProjects.TryGetValue(selectedProject, out value) ? value : "") + selectedProject;
			UserActCollectUtils.AddClickEvent("newuser_guide", "preset_list_data", data);
		}
		UserActCollectUtils.AddClickEvent("newuser_guide", "enable_tab_count", _selectedFeatures.Count.ToString() ?? "");
		UserActCollectUtils.AddClickEvent("newuser_guide", "preset_list_count", _selectedProjects.Count.ToString() ?? "");
		LocalSettings.Settings.UserPreference.desktopTabBars = desktopTabBar;
		SettingsHelper.PushLocalPreference();
		LocalSettings.Settings.Save(force: true);
		App.Window.RemoveNewUserGuide(this);
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: GuideChooseViewModel dataContext }))
		{
			return;
		}
		MessageText.Visibility = Visibility.Collapsed;
		if (_pageIndex == 0)
		{
			if (_selectedFeatures.Contains(dataContext.Name))
			{
				if (_selectedFeatures.IndexOf(dataContext.Name) <= 1)
				{
					ShowMessageText();
					SwitchFeature(dataContext.Name);
					return;
				}
				_selectedFeatures.Remove(dataContext.Name);
				dataContext.Selected = false;
			}
			else
			{
				_selectedFeatures.Add(dataContext.Name);
				dataContext.Selected = true;
			}
			SwitchFeature(_selectedFeatures.LastOrDefault());
		}
		else
		{
			if (_selectedProjects.Contains(dataContext.Name))
			{
				_selectedProjects.Remove(dataContext.Name);
				dataContext.Selected = false;
			}
			else
			{
				_selectedProjects.Add(dataContext.Name);
				dataContext.Selected = true;
			}
			SetSelectedProject(_selectedProjects.LastOrDefault());
		}
	}

	private async void ShowMessageText()
	{
		int range = (_showMessageRange = new Random().Next(0, 10000));
		MessageText.Visibility = Visibility.Visible;
		await Task.Delay(3000);
		if (range == _showMessageRange)
		{
			MessageText.Visibility = Visibility.Collapsed;
		}
	}

	private void SwitchFeature(string current, bool setImage = true)
	{
		ObservableCollection<GuideChooseViewModel> observableCollection = new ObservableCollection<GuideChooseViewModel>();
		foreach (string selectedFeature in _selectedFeatures)
		{
			observableCollection.Add(new GuideChooseViewModel(selectedFeature, GuideFeatures[selectedFeature])
			{
				Selected = (current == selectedFeature)
			});
		}
		SelectedFeatureItems.ItemsSource = observableCollection;
		int num = GuideFeatures.Keys.ToList().IndexOf(current);
		if (setImage && num >= 0)
		{
			FeatureImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/ImageSource/NewUserFeature{num}.png"));
		}
	}

	private void PreviousClick(object sender, MouseButtonEventArgs e)
	{
		SetPage(0);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/newuser/newuserguide.xaml", UriKind.Relative);
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
			FeatureImage = (Image)target;
			break;
		case 2:
			SelectedFeatureItems = (ItemsControl)target;
			break;
		case 3:
			ProjectGrid = (Grid)target;
			break;
		case 4:
			TodayIcon = (Path)target;
			break;
		case 5:
			WeekIcon = (Path)target;
			break;
		case 6:
			EmptyList = (Border)target;
			break;
		case 7:
			SelectedProjectItems = (ItemsControl)target;
			break;
		case 8:
			ChooseFeatureGrid = (Grid)target;
			break;
		case 9:
			BackIcon = (StackPanel)target;
			BackIcon.MouseLeftButtonUp += PreviousClick;
			break;
		case 10:
			PageTitle = (EmjTextBlock)target;
			break;
		case 11:
			ChooseFeatureItems = (ItemsControl)target;
			break;
		case 13:
			MessageText = (TextBlock)target;
			break;
		case 14:
			BotRect = (Rectangle)target;
			break;
		case 15:
			BotEll = (Ellipse)target;
			break;
		case 16:
			((TextBlock)target).MouseLeftButtonUp += OnSkipClick;
			break;
		case 17:
			Continue = (Button)target;
			Continue.Click += OnContinueClick;
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
		if (connectionId == 12)
		{
			((Border)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
