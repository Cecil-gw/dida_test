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
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.NewUser;

public class ChooseCommonProjectWindow : Window, IComponentConnector, IStyleConnector
{
	private readonly List<string> _selectedProjects = new List<string>();

	internal ItemsControl ChooseListItems;

	internal Button SaveButton;

	private bool _contentLoaded;

	public ChooseCommonProjectWindow()
	{
		InitializeComponent();
		InitItems();
	}

	private void InitItems()
	{
		List<string> list = NewUserGuide.DefaultProjects.Keys.ToList();
		ObservableCollection<GuideChooseViewModel> observableCollection = new ObservableCollection<GuideChooseViewModel>();
		foreach (string item in list)
		{
			observableCollection.Add(new GuideChooseViewModel(item, NewUserGuide.DefaultProjects[item]));
		}
		ChooseListItems.ItemsSource = observableCollection;
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: GuideChooseViewModel dataContext })
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
			SaveButton.IsEnabled = _selectedProjects.Count > 0;
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (!UserDao.IsPro() && CacheManager.GetProjectsWithoutInbox().Count((ProjectModel p) => !p.delete_status) + _selectedProjects.Count > 9)
		{
			ProChecker.CheckPro(ProType.MoreListsUnlimited);
			return;
		}
		await ProjectDao.InitProjects(_selectedProjects);
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/newuser/choosecommonprojectwindow.xaml", UriKind.Relative);
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
			ChooseListItems = (ItemsControl)target;
			break;
		case 3:
			((Button)target).Click += OnCancelClick;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
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
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
