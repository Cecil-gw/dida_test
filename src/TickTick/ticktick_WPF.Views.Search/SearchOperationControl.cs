using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.Search;

public class SearchOperationControl : UserControl, IComponentConnector
{
	private List<SearchOperationViewModel> _models;

	private ObservableCollection<SearchOperationViewModel> _items = new ObservableCollection<SearchOperationViewModel>();

	private Point _position;

	private bool _canEnter;

	private int _searchVersion;

	internal StackPanel Container;

	internal TextBox InputText;

	internal UpDownSelectListView ItemsView;

	internal StackPanel EmptyGrid;

	private bool _contentLoaded;

	public SearchOperationControl()
	{
		InitializeComponent();
		GetOperationModels();
		SearchProjectHelper.UpdateSortOrder();
	}

	private void GetOperationModels()
	{
		_models = new List<SearchOperationViewModel>
		{
			new SearchOperationViewModel(SearchOperationType.AddTask)
			{
				HoverSelected = true
			},
			new SearchOperationViewModel(Utils.GetString("GlobalOperate")),
			new SearchOperationViewModel(SearchOperationType.GlobalAddTask),
			new SearchOperationViewModel(SearchOperationType.ShowOrHideApp),
			new SearchOperationViewModel(SearchOperationType.OpenOrCloseFocusWindow),
			new SearchOperationViewModel(SearchOperationType.NewSticky),
			new SearchOperationViewModel(Utils.GetString("Navigation")),
			new SearchOperationViewModel(SearchOperationType.Task)
		};
		if (LocalSettings.Settings.EnableCalendar)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.Calendar));
		}
		if (LocalSettings.Settings.ShowMatrix)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.Matrix));
		}
		if (LocalSettings.Settings.ShowHabit)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.Habit));
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.Focus));
		}
		if (LocalSettings.Settings.EnableCountDownDays)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.Anniversary));
		}
		if (LocalSettings.Settings.EnableAI)
		{
			_models.Add(new SearchOperationViewModel(SearchOperationType.AI));
		}
		_models.AddRange(new SearchOperationViewModel[15]
		{
			new SearchOperationViewModel(SearchOperationType.SearchTask),
			new SearchOperationViewModel(SearchOperationType.GoSettings),
			new SearchOperationViewModel(SearchOperationType.GoAll),
			new SearchOperationViewModel(SearchOperationType.GoToday),
			new SearchOperationViewModel(SearchOperationType.GoTomorrow),
			new SearchOperationViewModel(SearchOperationType.GoNext7Day),
			new SearchOperationViewModel(SearchOperationType.GoAssignToMe),
			new SearchOperationViewModel(SearchOperationType.GoInbox),
			new SearchOperationViewModel(SearchOperationType.GoCompleted),
			new SearchOperationViewModel(SearchOperationType.GoAbandoned),
			new SearchOperationViewModel(SearchOperationType.GoSummary),
			new SearchOperationViewModel(SearchOperationType.GoTrash),
			new SearchOperationViewModel(Utils.GetString("Help")),
			new SearchOperationViewModel(SearchOperationType.HelpCenter),
			new SearchOperationViewModel(SearchOperationType.ShowShortCut)
		});
		foreach (SearchOperationViewModel model in _models)
		{
			_items.Add(model);
		}
		ItemsView.ItemsSource = _items;
	}

	public void FocusInput()
	{
		InputText.Focus();
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		InputText.Focus();
	}

	private async void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		int searchVersion = ++_searchVersion;
		_models.ForEach(delegate(SearchOperationViewModel m)
		{
			m.HoverSelected = false;
		});
		string text = InputText.Text.Trim().ToLower();
		if (string.IsNullOrEmpty(text))
		{
			ItemsView.Visibility = Visibility.Visible;
			ItemsSourceHelper.CopyTo(_models, _items);
			return;
		}
		List<SearchOperationViewModel> models = new List<SearchOperationViewModel>();
		List<SearchTagAndProjectModel> matchedProject = SearchProjectHelper.GetModelsMatched(text);
		List<SearchTagAndProjectModel> collection = await SearchProjectHelper.GetTaskGroupsMatched(text, withInbox: true);
		if (searchVersion != _searchVersion)
		{
			return;
		}
		matchedProject.AddRange(collection);
		List<SearchTagAndProjectModel> list = (from it in matchedProject
			orderby it.IsTag descending, it.IsGroup descending, it.IsProject descending, it.IsTaskGroup descending, it.SortOrder
			select it).ToList();
		for (int num = 0; num <= _models.Count - 1; num++)
		{
			SearchOperationViewModel searchOperationViewModel = _models[num];
			if (searchOperationViewModel.Type == SearchOperationType.None)
			{
				if (searchOperationViewModel.Title == Utils.GetString("Help"))
				{
					foreach (SearchTagAndProjectModel item in list)
					{
						models.Add(new SearchOperationViewModel
						{
							Type = (item.IsTag ? SearchOperationType.GoTag : (item.IsProject ? SearchOperationType.GoProject : (item.IsGroup ? SearchOperationType.GoGroup : (item.IsTaskGroup ? SearchOperationType.GoTaskGroup : SearchOperationType.GoFilter)))),
							Title = item.Name,
							Icon = item.IconData,
							PtfId = (item.IsTaskGroup ? item.ProjectId : item.Id),
							ColumnId = (item.IsTaskGroup ? item.Id : null),
							ProjectName = item.ProjectName,
							Emoji = item.Emoji
						});
					}
				}
				SearchOperationViewModel searchOperationViewModel2 = models.LastOrDefault();
				if (searchOperationViewModel2 != null && searchOperationViewModel2.Type == SearchOperationType.None)
				{
					models.Remove(searchOperationViewModel2);
				}
				models.Add(searchOperationViewModel);
			}
			else if (searchOperationViewModel.Pinyin.Contains(text) || searchOperationViewModel.Title.ToLower().Contains(text) || (searchOperationViewModel.Keyword != null && searchOperationViewModel.Keyword.ToLower().Contains(text)))
			{
				models.Add(searchOperationViewModel);
			}
		}
		SearchOperationViewModel searchOperationViewModel3 = models.LastOrDefault();
		if (searchOperationViewModel3 != null && searchOperationViewModel3.Type == SearchOperationType.None)
		{
			models.Remove(searchOperationViewModel3);
		}
		if (models.Count == 0)
		{
			models.Add(new SearchOperationViewModel(Utils.GetString("SearchCommandNoData")));
		}
		models.Add(new SearchOperationViewModel
		{
			Type = SearchOperationType.GoSearch,
			Title = Utils.GetString("Search") + " \"" + text + "\"",
			Shortcut = "",
			SearchText = text,
			Icon = Utils.GetGeometry("Ic.TaskOM.Search")
		});
		ItemsView.Visibility = ((models.Count == 0) ? Visibility.Collapsed : Visibility.Visible);
		ItemsSourceHelper.CopyTo(models, _items);
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			if (_canEnter)
			{
				SearchOperationViewModel searchOperationViewModel = _items.FirstOrDefault((SearchOperationViewModel i) => i.HoverSelected && i.Type != SearchOperationType.None);
				string text = InputText.Text.Trim();
				if (searchOperationViewModel != null)
				{
					MainWindowManager.DoOperation(searchOperationViewModel);
					Window.GetWindow(this)?.Close();
				}
				else if (!string.IsNullOrEmpty(text))
				{
					MainWindowManager.DoOperation(new SearchOperationViewModel
					{
						Type = SearchOperationType.GoSearch,
						SearchText = text
					});
					Window.GetWindow(this)?.Close();
				}
			}
			e.Handled = true;
		}
		else
		{
			_canEnter = true;
		}
	}

	private void OnTextKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.ImeProcessed:
			_canEnter = false;
			break;
		case Key.Up:
		case Key.Down:
			ItemsView.UpDownSelect(e.Key == Key.Up);
			break;
		case Key.Tab:
			e.Handled = true;
			break;
		}
	}

	private void OnItemSelected(bool onenter, UpDownSelectViewModel e)
	{
		if (e is SearchOperationViewModel { Type: not SearchOperationType.None } searchOperationViewModel)
		{
			MainWindowManager.DoOperation(searchOperationViewModel);
			Window.GetWindow(this)?.Close();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchoperationcontrol.xaml", UriKind.Relative);
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
			Container = (StackPanel)target;
			Container.PreviewMouseLeftButtonDown += OnMouseDown;
			break;
		case 2:
			InputText = (TextBox)target;
			InputText.TextChanged += OnTextChanged;
			InputText.PreviewKeyDown += OnTextKeyDown;
			InputText.PreviewKeyUp += OnKeyUp;
			break;
		case 3:
			ItemsView = (UpDownSelectListView)target;
			break;
		case 4:
			EmptyGrid = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
