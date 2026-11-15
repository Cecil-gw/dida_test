using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Config;

public class LinkGoogleCalendarWindow : Window, IComponentConnector
{
	private CalendarConnectInfo _info;

	private CalendarContentProject _calendarProject;

	private bool _isNew = true;

	private bool _saved;

	private static bool _checkConnects;

	internal TextBlock TitleText;

	internal Border HelpCenter;

	internal StackPanel BindOrLinkGrid;

	internal TextBlock SubscribeMessage;

	internal TextBlock IntegrationMessage;

	internal StackPanel IntegrationGrid;

	internal Grid BindGrid;

	internal TextBlock AppName;

	internal TextBlock GoogleName;

	internal Border ChooseListBorder;

	internal EmjTextBlock ChosenList;

	internal Border ChooseCalendarBorder;

	internal EmjTextBlock ChosenCalendar;

	internal EscPopup ChooseListPopup;

	internal EscPopup ChooseCalendarPopup;

	internal TextBlock ListMessage;

	internal Grid DefaultList;

	internal Path ProjectArrow;

	internal EmjTextBlock DefaultAddProjectNameText;

	internal Path ExpiredIcon;

	internal EscPopup DefaultAddProjectPopup;

	internal Grid DefaultTag;

	internal EmjTextBlock DefaultAddTagNameText;

	internal EscPopup DefaultAddTagPopup;

	internal TextBlock TagMessage;

	internal TextBlock DisintegrationText;

	internal Button SaveButton;

	internal Border ToastBorder;

	internal TextBlock ToastText;

	private bool _contentLoaded;

	public CalendarContentProject RelProject => _calendarProject;

	public string ChooseCalendarName => ChosenCalendar.Text;

	public bool IsSaved => _saved;

	public LinkGoogleCalendarWindow(CalendarConnectInfo info = null, CalendarContentProject relProject = null)
	{
		InitializeComponent();
		_calendarProject = new CalendarContentProject();
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		ListMessage.Text = string.Format(Utils.GetString("IntegrationWithGoogleListMessage"), Utils.GetAppName());
		TagMessage.Text = string.Format(Utils.GetString("IntegrationWithGoogleTagMessage"), Utils.GetAppName());
		SubscribeMessage.Text = string.Format(Utils.GetString("SubscribeFromGoogleDesc"), Utils.GetAppName());
		IntegrationMessage.Text = string.Format(Utils.GetString("IntegrationWithGoogleDesc"), Utils.GetAppName());
		AppName.Text = Utils.GetAppName();
		if (info != null)
		{
			SetInfo(info);
			if (relProject != null)
			{
				SetEditMode(relProject);
			}
		}
		else
		{
			UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "show");
		}
		base.Loaded += async delegate
		{
			try
			{
				await Task.Delay(100);
				Activate();
				for (Window owner = base.Owner; owner != null; owner = owner.Owner)
				{
					owner.Activate();
				}
			}
			catch (Exception)
			{
			}
		};
	}

	private void OnDragMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void CloseClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("subscribe_calendar", (_info == null) ? "google" : "integrate_google", "cancel");
		Close();
	}

	private async void OnIntegrationClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "integrate");
		if (ProChecker.CheckPro(ProType.IntegrationWithGoogle) && await CalendarConnectService.CheckConnectCount(this, "google"))
		{
			BindGoogleAccount instance = BindGoogleAccount.GetInstance(isSubscribe: false);
			instance.CalendarConnected += OnCalendarConnected;
			instance.ToastText += OnToastText;
			instance.Start();
		}
	}

	protected override void OnActivated(EventArgs e)
	{
		if (_checkConnects)
		{
			_checkConnects = false;
			CalendarService.PullCalendarConnects();
		}
		base.OnActivated(e);
	}

	private void OnToastText(object sender, string e)
	{
		Toast(e);
	}

	private void OnSubscribeClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "subscribe");
		if (!ProChecker.CheckPro(ProType.SubscribeCalendar) || !LimitCache.CheckCalendarCount("Google"))
		{
			return;
		}
		BindGoogleAccount instance = BindGoogleAccount.GetInstance();
		instance.Start();
		instance.ToastText += delegate(object o, string s)
		{
			if (base.Owner is SettingDialog settingDialog)
			{
				settingDialog.Toast(s);
			}
			else
			{
				Utils.Toast(s);
			}
		};
		Close();
	}

	public async void OnCalendarConnected(object sender, CalendarConnectInfo info)
	{
		SetInfo(info);
		await Task.Delay(3000);
		Activate();
	}

	private void SetInfo(CalendarConnectInfo info)
	{
		if (info != null)
		{
			UserActCollectUtils.AddClickEvent("subscribe_calendar", "integrate_google", "show");
			_info = info;
			BindOrLinkGrid.Visibility = Visibility.Collapsed;
			HelpCenter.Visibility = Visibility.Collapsed;
			TitleText.Text = Utils.GetString("IntegrationWithGoogle");
			IntegrationGrid.Visibility = Visibility.Visible;
			InitProjectAndTag();
		}
	}

	public void SetEditMode(CalendarContentProject relProject)
	{
		_calendarProject = relProject;
		_isNew = false;
		TitleText.Text = Utils.GetString("Edit");
		SaveButton.Content = Utils.GetString("PublicSave");
		SaveButton.IsEnabled = true;
		InitProjectAndTag();
		ChooseListBorder.IsHitTestVisible = false;
		ChooseListBorder.Opacity = 0.4;
		ChooseCalendarBorder.IsHitTestVisible = false;
		ChooseCalendarBorder.Opacity = 0.4;
		DisintegrationText.Visibility = Visibility.Visible;
	}

	private async void InitProjectAndTag()
	{
		SetChosenList();
		SetChosenCalendar();
		ExpiredIcon.Visibility = Visibility.Collapsed;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == _calendarProject.syncProjectId);
		if (projectModel != null)
		{
			DefaultAddProjectNameText.Text = projectModel.name;
			DefaultAddProjectNameText.Tag = projectModel.id;
		}
		else if (string.IsNullOrEmpty(_calendarProject.syncProjectId))
		{
			_calendarProject.syncProjectId = LocalSettings.Settings.InServerBoxId;
			DefaultAddProjectNameText.Text = Utils.GetString("Inbox");
			DefaultAddProjectNameText.Tag = LocalSettings.Settings.InServerBoxId;
		}
		else
		{
			DefaultAddProjectNameText.Text = Utils.GetString("InvalidList");
			DefaultAddProjectNameText.Tag = "";
			ExpiredIcon.Visibility = Visibility.Visible;
			SaveButton.IsEnabled = false;
		}
		List<string> tags = CalendarConnectService.NormalizeDefaultTags(_calendarProject);
		if (!_isNew)
		{
			foreach (string item in tags)
			{
				if (CacheManager.GetTagByName(item.ToLower()) == null)
				{
					await TagDao.TryCreateTag(item);
				}
			}
		}
		if (tags.Count == 0 && _isNew)
		{
			tags.Add(CacheManager.GetTagByName("gcal")?.label ?? "GCal");
			_calendarProject.defaultTags = tags;
		}
		DefaultAddTagNameText.Text = ((tags.Count > 0) ? tags.Join(",") : Utils.GetString("none"));
	}

	private async void OnDefaultAddProjectClick(object sender, MouseButtonEventArgs e)
	{
		string defaultProjectId = "";
		if (DefaultAddProjectNameText.Tag is string text)
		{
			defaultProjectId = text;
		}
		List<string> linkedProjectIds = await CalendarConnectService.GetAllConnectProjectIds();
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(DefaultAddProjectPopup, new ProjectExtra
		{
			ProjectIds = new List<string> { defaultProjectId }
		}, new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			showSmartAll = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			showNoteProject = true,
			showSharedProject = true,
			CanSearch = true,
			LinkedProjectIds = linkedProjectIds
		});
		projectOrGroupPopup.ItemSelect += OnDefaultProjectSelect;
		projectOrGroupPopup.Show();
	}

	private void OnDefaultTagClick(object sender, MouseButtonEventArgs e)
	{
		if (_info != null)
		{
			BatchSetTagControl batchSetTagControl = new BatchSetTagControl();
			batchSetTagControl.Init(new TagSelectData
			{
				OmniSelectTags = (_calendarProject.defaultTags?.ToList() ?? new List<string>())
			}, firstInit: true);
			batchSetTagControl.TagsSelect += delegate(object s, TagSelectData tags)
			{
				_calendarProject.defaultTags = TagSerializer.NormalizeNames(tags.OmniSelectTags);
				DefaultAddTagPopup.IsOpen = false;
				SetTagText(_calendarProject.defaultTags);
			};
			batchSetTagControl.Close += delegate
			{
				DefaultAddTagPopup.IsOpen = false;
			};
			DefaultAddTagPopup.Child = batchSetTagControl;
			DefaultAddTagPopup.IsOpen = true;
		}
	}

	private void SetTagText(List<string> tags)
	{
		tags = TagSerializer.NormalizeNames(tags);
		if (tags == null || tags.Count == 0)
		{
			DefaultAddTagNameText.Text = Utils.GetString("none");
			return;
		}
		tags = (from t in tags
			select CacheManager.GetTags().FirstOrDefault((TagModel ta) => ta.name == t.ToLower())?.label into label
			where !string.IsNullOrEmpty(label)
			select label).ToList();
		DefaultAddTagNameText.Text = tags.Join(",");
	}

	private void OnDefaultProjectSelect(object sender, SelectableItemViewModel e)
	{
		if (e is ProjectGroupViewModel)
		{
			return;
		}
		DefaultAddProjectPopup.IsOpen = false;
		string projectId = e.Id;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null && (!projectModel.IsShareList() || new CustomerDialog(Utils.GetString("SetShareProjectAsDefault"), Utils.GetString("SetShareProjectAsDefaultDesc"), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true))
		{
			DefaultAddProjectNameText.Text = projectModel.name;
			DefaultAddProjectNameText.Tag = projectModel.id;
			_calendarProject.syncProjectId = projectModel.id;
			ExpiredIcon.Visibility = Visibility.Collapsed;
			if (!_isNew)
			{
				SaveButton.IsEnabled = true;
			}
		}
	}

	private async void OnChooseListClick(object sender, MouseButtonEventArgs e)
	{
		List<string> linked = await CalendarConnectService.GetAllConnectProjectIds();
		string defaultProjectId = TaskDefaultDao.GetDefaultSafely().ProjectId;
		if (linked.Contains("all"))
		{
			await SettingsHelper.PullRemoteSettings();
			linked.Add(defaultProjectId);
		}
		string item = ((_calendarProject.syncProjectScope == "all") ? "_special_id_all" : _calendarProject.syncProjectScope);
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(ChooseListPopup, new ProjectExtra
		{
			ProjectIds = new List<string> { item }
		}, new ProjectSelectorExtra
		{
			showSmartAll = true,
			batchMode = false,
			showAll = false,
			canSelectGroup = false,
			onlyShowPermission = true,
			showNoteProject = true,
			showSharedProject = true,
			CanSearch = true,
			LinkedProjectIds = linked
		});
		projectOrGroupPopup.ItemSelect += OnListChoose;
		projectOrGroupPopup.Show();
	}

	private void OnListChoose(object sender, SelectableItemViewModel e)
	{
		string syncProjectScope = ((e.Id == "_special_id_all") ? "all" : e.Id);
		_calendarProject.syncProjectScope = syncProjectScope;
		SetChosenList();
		if (!string.IsNullOrEmpty(_calendarProject.syncProjectScope) && !string.IsNullOrEmpty(_calendarProject.calendarId))
		{
			SaveButton.IsEnabled = true;
		}
	}

	private void SetChosenList()
	{
		if (_calendarProject == null)
		{
			_calendarProject = new CalendarContentProject();
		}
		DefaultList.Visibility = ((!(_calendarProject.syncProjectScope == "all")) ? Visibility.Collapsed : Visibility.Visible);
		if (_calendarProject.syncProjectScope == "all")
		{
			ChosenList.Text = Utils.GetString("All");
		}
		else
		{
			ProjectModel projectById = CacheManager.GetProjectById(_calendarProject.syncProjectScope);
			if (projectById != null)
			{
				ChosenList.Text = projectById.name;
			}
			else
			{
				ChosenList.Text = Utils.GetString("ChooseList");
			}
		}
		SetChosenTextFontsize();
	}

	private void OnChooseCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (_info == null)
		{
			return;
		}
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>();
		foreach (CalendarContentCalendar calendar in _info.calendars)
		{
			if (string.IsNullOrEmpty(calendar.accessRole) || calendar.accessRole.ToLower().Trim() == "writer" || calendar.accessRole.ToLower().Trim() == "owner")
			{
				bool flag = _info.relProjects != null && !_info.relProjects.All((CalendarContentProject p) => p.calendarId != calendar.outId);
				list.Add(new CustomMenuItemViewModel((object)calendar.outId, calendar.name, (Geometry)null, "")
				{
					Selected = (calendar.outId == _calendarProject.calendarId),
					Selectable = !flag,
					ExtraIcon = (flag ? Utils.GetGeometry("Ic.Focus.LinkToTask") : null),
					UnableText = (flag ? Utils.GetString("CalendarIntegrated") : null),
					ExtraIconColor = (flag ? ThemeUtil.GetColor("BaseColorOpacity40") : null),
					ExtraIconSize = 14
				});
			}
		}
		list.Add(new CustomMenuItemViewModel("addNew", Utils.GetString("AddCalendar"), Utils.GetGeometry("Ic.ProjectList.Add"))
		{
			ImageWidth = 14.0,
			Selected = true,
			ShowSelectedIcon = false,
			ImageMargin = new Thickness(6.0, 0.0, 0.0, 0.0)
		});
		CustomMenuList customMenuList = new CustomMenuList(list, ChooseCalendarPopup);
		customMenuList.Operated += delegate(object o, object o1)
		{
			string text = o1 as string;
			if (text == "addNew")
			{
				ShowAddCalendarWindow();
			}
			else
			{
				_calendarProject.calendarId = text;
				SetChosenCalendar();
				if (!string.IsNullOrEmpty(_calendarProject.syncProjectScope) && !string.IsNullOrEmpty(_calendarProject.calendarId))
				{
					SaveButton.IsEnabled = true;
				}
			}
		};
		customMenuList.Show();
	}

	private async void ShowAddCalendarWindow()
	{
		AddCalendarWindow addCalendarWindow = new AddCalendarWindow(_info.id)
		{
			Owner = this
		};
		addCalendarWindow.ShowDialog();
		Activate();
		if (addCalendarWindow.NewCalendar != null)
		{
			UserActCollectUtils.AddClickEvent("subscribe_calendar", "integrate_google", "add_calendar");
			_info.calendars?.Add(addCalendarWindow.NewCalendar);
			await Task.Delay(200);
			OnChooseCalendarClick(null, null);
		}
	}

	private void SetChosenCalendar()
	{
		CalendarContentCalendar calendarContentCalendar = _info?.calendars.FirstOrDefault((CalendarContentCalendar c) => c.outId == _calendarProject.calendarId);
		if (calendarContentCalendar != null)
		{
			ChosenCalendar.Text = calendarContentCalendar.name;
		}
		else
		{
			ChosenCalendar.Text = Utils.GetString("ChooseCalendar");
		}
		SetChosenTextFontsize();
	}

	private void SetChosenTextFontsize()
	{
		double val = Utils.MeasureStringWidth(ChosenList.Text, 16.0, FontWeights.Bold);
		double val2 = Utils.MeasureStringWidth(ChosenCalendar.Text, 16.0, FontWeights.Bold);
		int num = ((Math.Max(val, val2) < 150.0) ? 16 : 14);
		ChosenList.FontSize = num;
		ChosenCalendar.FontSize = num;
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		SaveButton.IsEnabled = false;
		if (_isNew)
		{
			ProjectBindCalendarModel model = new ProjectBindCalendarModel
			{
				calendarId = _calendarProject.calendarId,
				syncProjectScope = _calendarProject.syncProjectScope,
				syncProjectId = ((_calendarProject.syncProjectScope == "all") ? _calendarProject.syncProjectId : null),
				defaultTags = _calendarProject.defaultTags
			};
			_saved = await Communicator.BindProjectCalendar(_info.id, model);
			UserActCollectUtils.AddClickEvent("subscribe_calendar", "integrate_google", "integrate_success");
			Window owner = base.Owner;
			if (!(owner is CalendarConnectInfoWindow calendarConnectInfoWindow))
			{
				if (owner is SettingDialog settingDialog)
				{
					settingDialog.Toast(Utils.GetString("Integrated"));
				}
			}
			else
			{
				calendarConnectInfoWindow.Toast(Utils.GetString("Integrated"));
			}
		}
		else
		{
			ModifyBindProjectCalendarModel model2 = new ModifyBindProjectCalendarModel
			{
				id = _calendarProject.id,
				syncProjectId = ((_calendarProject.syncProjectScope == "all") ? _calendarProject.syncProjectId : null),
				defaultTags = _calendarProject.defaultTags
			};
			_saved = await Communicator.ModifyProjectCalendar(_info.id, model2);
		}
		if (_calendarProject?.defaultTags != null && _calendarProject.defaultTags.Contains("gcal", StringComparer.OrdinalIgnoreCase) && CacheManager.GetTagByName("gcal") == null)
		{
			TagDao.TryCreateTag("GCal");
		}
		if (!_saved)
		{
			SaveButton.IsEnabled = true;
			return;
		}
		await CalendarService.PullCalendarConnects();
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("subscribe_calendar", (_info == null) ? "google" : "integrate_google", "cancel");
		Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!_saved)
		{
			_calendarProject = null;
		}
		_info = null;
		BindGoogleAccount.ClearEvent();
		base.OnClosing(e);
	}

	private async void OnDisintegrationClick(object sender, MouseButtonEventArgs e)
	{
		CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("Disintegration"), Utils.GetString("DisintegrationDesc"), Utils.GetString("OK"), Utils.GetString("Cancel"), this);
		customerDialog.ShowDialog();
		if (customerDialog.DialogResult.HasValue && customerDialog.DialogResult.Value && await Communicator.UnbindProjectCalendar(_info.id, _calendarProject.id))
		{
			await CalendarService.PullCalendarConnects();
			if (base.Owner is CalendarConnectInfoWindow calendarConnectInfoWindow)
			{
				calendarConnectInfoWindow.Toast(Utils.GetString("DisintegrationSucceed"));
			}
			Close();
		}
	}

	private void GotoHelpCenter(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("subscribe_calendar", "google", "guide");
		Utils.TryProcessStartUrl("https://help.dida365.com/articles/6950000554735042560#google%E3%80%81outlook%E3%80%81exchange%E3%80%81icloud-%E6%97%A5%E5%8E%86%E5%90%8C%E6%AD%A5");
	}

	public void Toast(string toastText)
	{
		ToastBorder.Visibility = Visibility.Visible;
		ToastText.Text = toastText;
		Storyboard storyboard = (Storyboard)FindResource("ShowToast");
		ToastBorder.BeginStoryboard(storyboard);
	}

	private void OnToasted(object sender, EventArgs e)
	{
		ToastBorder.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/linkgooglecalendarwindow.xaml", UriKind.Relative);
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
			((Storyboard)target).Completed += OnToasted;
			break;
		case 2:
			((Border)target).MouseLeftButtonDown += OnDragMove;
			break;
		case 3:
			TitleText = (TextBlock)target;
			break;
		case 4:
			HelpCenter = (Border)target;
			HelpCenter.MouseLeftButtonUp += GotoHelpCenter;
			break;
		case 5:
			BindOrLinkGrid = (StackPanel)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnSubscribeClick;
			break;
		case 7:
			SubscribeMessage = (TextBlock)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += OnIntegrationClick;
			break;
		case 9:
			IntegrationMessage = (TextBlock)target;
			break;
		case 10:
			IntegrationGrid = (StackPanel)target;
			break;
		case 11:
			BindGrid = (Grid)target;
			break;
		case 12:
			AppName = (TextBlock)target;
			break;
		case 13:
			GoogleName = (TextBlock)target;
			break;
		case 14:
			ChooseListBorder = (Border)target;
			ChooseListBorder.MouseLeftButtonUp += OnChooseListClick;
			break;
		case 15:
			ChosenList = (EmjTextBlock)target;
			break;
		case 16:
			ChooseCalendarBorder = (Border)target;
			ChooseCalendarBorder.MouseLeftButtonUp += OnChooseCalendarClick;
			break;
		case 17:
			ChosenCalendar = (EmjTextBlock)target;
			break;
		case 18:
			ChooseListPopup = (EscPopup)target;
			break;
		case 19:
			ChooseCalendarPopup = (EscPopup)target;
			break;
		case 20:
			ListMessage = (TextBlock)target;
			break;
		case 21:
			DefaultList = (Grid)target;
			break;
		case 22:
			ProjectArrow = (Path)target;
			break;
		case 23:
			((Border)target).MouseLeftButtonUp += OnDefaultAddProjectClick;
			break;
		case 24:
			DefaultAddProjectNameText = (EmjTextBlock)target;
			break;
		case 25:
			ExpiredIcon = (Path)target;
			break;
		case 26:
			DefaultAddProjectPopup = (EscPopup)target;
			break;
		case 27:
			DefaultTag = (Grid)target;
			break;
		case 28:
			((Border)target).MouseLeftButtonUp += OnDefaultTagClick;
			break;
		case 29:
			DefaultAddTagNameText = (EmjTextBlock)target;
			break;
		case 30:
			DefaultAddTagPopup = (EscPopup)target;
			break;
		case 31:
			TagMessage = (TextBlock)target;
			break;
		case 32:
			DisintegrationText = (TextBlock)target;
			DisintegrationText.MouseLeftButtonUp += OnDisintegrationClick;
			break;
		case 33:
			((Button)target).Click += OnCancelClick;
			break;
		case 34:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 35:
			((Border)target).MouseLeftButtonUp += CloseClick;
			break;
		case 36:
			ToastBorder = (Border)target;
			break;
		case 37:
			ToastText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
