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
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class GroupTitle2 : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty CountProperty = DependencyProperty.Register("Count", typeof(int), typeof(GroupTitle2), new PropertyMetadata(2));

	private List<GroupTitleViewModel> _models = new List<GroupTitleViewModel>();

	public string _titles = string.Empty;

	private int _tabIndex = -1;

	internal GroupTitle2 Root;

	internal Border BackBorder;

	internal ItemsControl TitlesListView;

	private bool _contentLoaded;

	public int Count
	{
		get
		{
			return (int)GetValue(CountProperty);
		}
		set
		{
			SetValue(CountProperty, value);
		}
	}

	public string Titles
	{
		get
		{
			return _titles;
		}
		set
		{
			_titles = value;
			UpdateTitles();
		}
	}

	public event EventHandler<GroupTitleViewModel> SelectedTitleChanged;

	public void SetSelectedIndex(int index)
	{
		if (index < _models.Count && index >= 0)
		{
			for (int i = 0; i < _models.Count; i++)
			{
				_models[i].IsSelected = index == i;
			}
		}
	}

	public int GetSelectedIndex()
	{
		return _models.FirstOrDefault((GroupTitleViewModel m) => m.IsSelected)?.Index ?? 0;
	}

	public GroupTitle2()
	{
		InitializeComponent();
	}

	public void UpdateTitles()
	{
		string[] array = _titles.Split('|');
		if (array.Any())
		{
			_models.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				_models.Add(new GroupTitleViewModel
				{
					Content = Utils.GetString(text),
					Title = text,
					Index = i
				});
			}
			if (_models.Count > 0)
			{
				_models[0].IsSelected = true;
			}
		}
		Count = Math.Max(1, array.Length);
		TitlesListView.ItemsSource = new ObservableCollection<GroupTitleViewModel>(_models);
	}

	public void SetTitleContent(string title, string content)
	{
		foreach (GroupTitleViewModel model in _models)
		{
			if (model.Title.Equals(title))
			{
				model.Content = content;
			}
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: GroupTitleViewModel { IsSelected: false } dataContext })
		{
			_models.ForEach(delegate(GroupTitleViewModel m)
			{
				m.IsSelected = false;
			});
			dataContext.IsSelected = true;
			this.SelectedTitleChanged?.Invoke(this, dataContext);
		}
	}

	public void TabSelectFirst()
	{
		_tabIndex = 0;
		_models.ForEach(delegate(GroupTitleViewModel m)
		{
			m.IsTabSelected = false;
		});
		_models[_tabIndex].IsTabSelected = true;
	}

	public void TabSelectLast()
	{
		_tabIndex = _models.Count - 1;
		_models.ForEach(delegate(GroupTitleViewModel m)
		{
			m.IsTabSelected = false;
		});
		_models[_tabIndex].IsTabSelected = true;
	}

	public void TabSelect(int index)
	{
		_models.ForEach(delegate(GroupTitleViewModel m)
		{
			m.IsTabSelected = false;
		});
		if (index < _models.Count && index >= 0)
		{
			_tabIndex = index;
			_models[_tabIndex].IsTabSelected = true;
		}
		else
		{
			_tabIndex = -1;
		}
	}

	public bool TabSelectNext(bool next)
	{
		if (_models.All((GroupTitleViewModel m) => !m.IsTabSelected))
		{
			if (InTab())
			{
				_models[_tabIndex].IsTabSelected = true;
			}
			return false;
		}
		_tabIndex += (next ? 1 : (-1));
		if (Math.Max(0, Math.Min(_tabIndex, _models.Count - 1)) != _tabIndex)
		{
			return false;
		}
		_models.ForEach(delegate(GroupTitleViewModel m)
		{
			m.IsTabSelected = false;
		});
		_models[++_tabIndex].IsTabSelected = true;
		return true;
	}

	public bool InTab()
	{
		if (_tabIndex >= 0)
		{
			return _tabIndex < _models.Count;
		}
		return false;
	}

	public void SelectTabItem()
	{
		if (!InTab())
		{
			return;
		}
		GroupTitleViewModel groupTitleViewModel = _models[_tabIndex];
		if (!groupTitleViewModel.IsSelected)
		{
			_models.ForEach(delegate(GroupTitleViewModel m)
			{
				m.IsSelected = false;
			});
			groupTitleViewModel.IsSelected = true;
			this.SelectedTitleChanged?.Invoke(this, groupTitleViewModel);
		}
	}

	public void ClearTabSelected()
	{
		_models.ForEach(delegate(GroupTitleViewModel m)
		{
			m.IsTabSelected = false;
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/grouptitle2.xaml", UriKind.Relative);
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
			Root = (GroupTitle2)target;
			break;
		case 2:
			BackBorder = (Border)target;
			break;
		case 3:
			TitlesListView = (ItemsControl)target;
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
		if (connectionId == 4)
		{
			((Border)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
