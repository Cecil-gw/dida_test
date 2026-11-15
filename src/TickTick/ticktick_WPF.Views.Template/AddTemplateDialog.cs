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
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.Template;

public class AddTemplateDialog : Window, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty SearchKeywordProperty = DependencyProperty.Register("SearchKeyword", typeof(string), typeof(AddTemplateDialog), new PropertyMetadata(""));

	private TemplateKind _kind;

	private List<TaskTemplateModel> _localTaskTemplate = new List<TaskTemplateModel>();

	private ObservableCollection<TemplateViewModel> _templateViewModels = new ObservableCollection<TemplateViewModel>();

	private List<TemplateViewModel> _allTemplateViewModels = new List<TemplateViewModel>();

	private AddTaskViewModel _addModel;

	private ListViewContainer _listView;

	private bool _wasDragging;

	private readonly DelayActionHandler _searchHandler = new DelayActionHandler(200);

	internal StackPanel EmptyPanel;

	internal Grid SearchEmptyGrid;

	internal ContentControl NoSearchEmptyGrid;

	internal ScrollViewer Scroller;

	internal ItemsControl TemplateItems;

	internal TextBox SearchTextBox;

	private bool _contentLoaded;

	public string SearchKeyword
	{
		get
		{
			return (string)GetValue(SearchKeywordProperty);
		}
		set
		{
			SetValue(SearchKeywordProperty, value);
		}
	}

	public event EventHandler<string> TemplateSelected;

	public AddTemplateDialog(TemplateKind kind, AddTaskViewModel addModel = null, ListViewContainer listView = null)
	{
		AddTemplateDialog addTemplateDialog = this;
		InitializeComponent();
		_addModel = addModel;
		_listView = listView;
		base.InputBindings.Add(new KeyBinding(WindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.Title = Utils.GetString((kind == TemplateKind.Note) ? "NoteTemplate" : "TaskTemplate");
		SearchTextBox.TextChanged += OnSearchTextChanged;
		_searchHandler.SetAction(delegate
		{
			addTemplateDialog.Dispatcher.Invoke(addTemplateDialog.FilterTemplates);
		});
		EmptyView emptyView = new EmptyView(160.0);
		emptyView.SetIconsSource(new EmptyIconsSourceModel("SearchResultEmpty"));
		SearchEmptyGrid.Children.Add(emptyView);
		EmptyWithTextView emptyWithTextView = new EmptyWithTextView(148.0);
		emptyWithTextView.SetData(Utils.GetString("YouDoNotHaveAnyTemplates"), Utils.GetString((kind == TemplateKind.Note) ? "TemplateEmptyNoteText" : "TemplateEmptyTaskText"), new EmptyIconsSourceModel("TemplateEmpty", new List<int> { 2, 4, 5 }), setIcon5Color: false);
		NoSearchEmptyGrid.Content = emptyWithTextView;
		base.Loaded += async delegate
		{
			DataChangedNotifier.TemplateChanged += addTemplateDialog.OnTemplateChanged;
			await addTemplateDialog.Init(kind);
		};
		base.Unloaded += delegate
		{
			DataChangedNotifier.TemplateChanged -= addTemplateDialog.OnTemplateChanged;
		};
		DragSortBehavior.SetSortCompleted(TemplateItems, OnSortCompleted);
		UserActCollectUtils.AddClickEvent("add_task", "template", "template_page");
	}

	private async void OnTemplateChanged(object sender, EventArgs e)
	{
		_localTaskTemplate = await TemplateDao.GetLocalTemplate();
		ShowLocalTemplate(_kind);
	}

	private async Task Init(TemplateKind kind)
	{
		_kind = kind;
		_localTaskTemplate = await TemplateDao.GetLocalTemplate();
		ShowLocalTemplate(kind);
		await Task.Delay(10);
		TryPullRemoteTemplate(kind);
	}

	private void ShowLocalTemplate(TemplateKind kind)
	{
		List<TemplateViewModel> list = TemplateViewModel.Build(_localTaskTemplate.Where((TaskTemplateModel t) => t.Kind == kind.ToString() || (kind == TemplateKind.Task && string.IsNullOrEmpty(t.Kind))).ToList());
		list.Sort(TemplateOrderHelper.Compare);
		_allTemplateViewModels = list;
		_templateViewModels = new ObservableCollection<TemplateViewModel>(list);
		TemplateItems.ItemsSource = _templateViewModels;
		UpdateEmptyVisibility();
	}

	private void UpdateEmptyVisibility()
	{
		string value = SearchTextBox.Text?.Trim() ?? "";
		bool flag = _templateViewModels.Count > 0;
		if (string.IsNullOrEmpty(value))
		{
			EmptyPanel.Visibility = Visibility.Collapsed;
			NoSearchEmptyGrid.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			Scroller.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
		else
		{
			NoSearchEmptyGrid.Visibility = Visibility.Collapsed;
			EmptyPanel.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			Scroller.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private async void TryPullRemoteTemplate(TemplateKind kind)
	{
		if (!Utils.IsNetworkAvailable())
		{
			return;
		}
		TemplatesModel templates = await Communicator.PullTemplates();
		if (templates != null)
		{
			List<TaskTemplateModel> defaultTemplate = new List<TaskTemplateModel>();
			if (templates.noteTemplates == null && _localTaskTemplate.Count((TaskTemplateModel t) => t.Kind == TemplateKind.Note.ToString()) == 0)
			{
				List<TaskTemplateModel> defaultTemplates = TaskTemplateModel.GetDefaultNoteTemp();
				await TemplateDao.AddTemplates(defaultTemplates, isNote: true);
				defaultTemplate.AddRange(defaultTemplates);
			}
			if (templates.taskTemplates == null && _localTaskTemplate.Count((TaskTemplateModel t) => t.Kind == TemplateKind.Task.ToString() || string.IsNullOrEmpty(t.Kind)) == 0)
			{
				List<TaskTemplateModel> defaultTemplates = TaskTemplateModel.GetDefaultTaskTemp();
				await TemplateDao.AddTemplates(defaultTemplates);
				defaultTemplate.AddRange(defaultTemplates);
			}
			if (await TemplateDao.MergeTemplates(templates.taskTemplates, templates.noteTemplates, _localTaskTemplate))
			{
				_localTaskTemplate = await TemplateDao.GetLocalTemplate();
				ShowLocalTemplate(kind);
			}
			else if (defaultTemplate.Count > 0)
			{
				_localTaskTemplate.AddRange(defaultTemplate);
				ShowLocalTemplate(kind);
			}
		}
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		_searchHandler.TryDoAction();
	}

	private void FilterTemplates()
	{
		string text = SearchTextBox.Text?.Trim().ToLower() ?? "";
		SearchKeyword = text;
		if (string.IsNullOrEmpty(text))
		{
			_templateViewModels = new ObservableCollection<TemplateViewModel>(_allTemplateViewModels);
		}
		else
		{
			List<TemplateViewModel> list = _allTemplateViewModels.Where(delegate(TemplateViewModel t)
			{
				if (!ContainsSearchText(t.Title, text) && !ContainsSearchText(t.Content, text) && !ContainsSearchText(t.Desc, text))
				{
					List<string> items = t.Items;
					if (items == null || !items.Any((string x) => ContainsSearchText(x, text)))
					{
						return t.Children?.Any((TemplateChildViewModel x) => ContainsSearchText(x.Title, text)) ?? false;
					}
				}
				return true;
			}).ToList();
			_templateViewModels = new ObservableCollection<TemplateViewModel>(list);
		}
		TemplateItems.ItemsSource = _templateViewModels;
		UpdateEmptyVisibility();
	}

	private static bool ContainsSearchText(string value, string searchText)
	{
		string contentMarkdownDisplayText = TaskUtils.GetContentMarkdownDisplayText(value);
		if (!string.IsNullOrEmpty(contentMarkdownDisplayText))
		{
			return contentMarkdownDisplayText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private void OnTemplateClick(object sender, MouseButtonEventArgs e)
	{
		if (_wasDragging)
		{
			_wasDragging = false;
		}
		else if (sender is Grid { DataContext: TemplateViewModel dataContext })
		{
			this.TemplateSelected?.Invoke(this, dataContext.Id);
			UserActCollectUtils.AddClickEvent("add_task", "template", "template_apply");
			Close();
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonDown(e);
		DragMove();
	}

	private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
	{
		SearchTextBox.Text = null;
	}

	private void OnSortCompleted(object dragItem, int oldIndex, int newIndex)
	{
		_wasDragging = true;
		if (TemplateOrderHelper.CountWithoutOrder(_templateViewModels) > 1)
		{
			TemplateOrderHelper.ResetOrders(_templateViewModels);
			TemplateViewModel.SaveSortOrders(_templateViewModels);
		}
		else
		{
			long? num = TemplateOrderHelper.CalculateInsertedOrder(_templateViewModels, newIndex);
			if (!num.HasValue)
			{
				return;
			}
			if (dragItem is TemplateViewModel templateViewModel)
			{
				templateViewModel.SortOrder = num.Value;
				templateViewModel.HasOrder = true;
				templateViewModel.SaveSortOrder();
			}
		}
		_allTemplateViewModels = _templateViewModels.ToList();
	}

	private async void ManageTemplateClick(object sender, MouseButtonEventArgs e)
	{
		Close();
		await Task.Delay(50);
		SettingDialog.ShowTemplateSettingDialog(null, _kind, _addModel);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/template/addtemplatedialog.xaml", UriKind.Relative);
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
		case 2:
			EmptyPanel = (StackPanel)target;
			break;
		case 3:
			SearchEmptyGrid = (Grid)target;
			break;
		case 4:
			NoSearchEmptyGrid = (ContentControl)target;
			break;
		case 5:
			Scroller = (ScrollViewer)target;
			break;
		case 6:
			TemplateItems = (ItemsControl)target;
			break;
		case 7:
			((TextBlock)target).MouseLeftButtonUp += ManageTemplateClick;
			break;
		case 8:
			((Grid)target).MouseLeftButtonDown += Grid_MouseLeftButtonDown;
			break;
		case 9:
			SearchTextBox = (TextBox)target;
			break;
		case 10:
			((Button)target).Click += ClearSearchButton_Click;
			break;
		case 11:
			((Button)target).Click += CloseButton_Click;
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
		if (connectionId == 1)
		{
			((Grid)target).MouseLeftButtonUp += OnTemplateClick;
		}
	}
}
