using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class ProjectListColorVisSetPanel : UserControl, IComponentConnector, IStyleConnector
{
	public class ProjectListColorVisViewModel : BaseViewModel
	{
		private bool _selected;

		public string Title { get; set; }

		public bool Value { get; set; }

		public bool Selected
		{
			get
			{
				return _selected;
			}
			set
			{
				if (_selected != value)
				{
					_selected = value;
					OnPropertyChanged("Selected");
				}
			}
		}

		public ProjectListColorVisViewModel(bool value, string title)
		{
			Title = title;
			Value = value;
			Selected = LocalSettings.Settings.ExtraSettings.IsShowProjectListColor == value;
		}
	}

	internal ItemsControl MainItems;

	private bool _contentLoaded;

	private ExtObservableCollection<ProjectListColorVisViewModel> items { get; set; } = new ExtObservableCollection<ProjectListColorVisViewModel>();

	public ProjectListColorVisSetPanel()
	{
		InitializeComponent();
		items.Add(new ProjectListColorVisViewModel(value: true, Utils.GetString("Show")));
		items.Add(new ProjectListColorVisViewModel(value: false, Utils.GetString("Hide")));
		MainItems.ItemsSource = items;
	}

	private void OnVisSelect(object sender, MouseButtonEventArgs e)
	{
		if (!((sender as FrameworkElement)?.DataContext is ProjectListColorVisViewModel projectListColorVisViewModel))
		{
			return;
		}
		foreach (ProjectListColorVisViewModel item in items)
		{
			item.Selected = item == projectListColorVisViewModel;
		}
		LocalSettings.Settings.ExtraSettings.IsShowProjectListColor = projectListColorVisViewModel.Value;
		DataChangedNotifier.NotifyProjectListColorVisChanged();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/theme/projectlistcolorvissetpanel.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			MainItems = (ItemsControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnVisSelect;
		}
	}
}
