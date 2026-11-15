using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public class ListItemProjectLabel : StackPanel, IComponentConnector
{
	private bool _projectNameDown;

	internal ListItemProjectLabel ProjectName;

	internal EmjTextBlock ProjectTitle;

	private bool _contentLoaded;

	public ListItemProjectLabel()
	{
		InitializeComponent();
		base.DataContextChanged += OnDataBind;
		SetValue(ToolTipService.InitialShowDelayProperty, 500);
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { ShowProject: not false } displayItemModel)
		{
			SetTooltip(displayItemModel.ProjectName);
		}
	}

	private void OnProjectNameMouseDown(object sender, MouseButtonEventArgs e)
	{
		_projectNameDown = true;
	}

	private async void OnProjectNameClick(object sender, MouseButtonEventArgs e)
	{
		if (_projectNameDown && base.DataContext is DisplayItemModel displayItemModel && (displayItemModel.IsTaskOrNote || displayItemModel.IsItem))
		{
			App.Window.TryShowMainWindow();
			App.Window.StopSearch();
			App.Window.NavigateNormalProject(displayItemModel.ProjectId);
		}
		_projectNameDown = false;
	}

	private void OnTitleSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (base.DataContext is DisplayItemModel { ShowProject: not false } displayItemModel)
		{
			SetTooltip(displayItemModel.ProjectName);
		}
	}

	private async void SetTooltip(string projectName)
	{
		await Task.Delay(100);
		double num = Utils.MeasureStringWidth(projectName, 12.0);
		base.ToolTip = ((num - 1.0 > ProjectTitle.ActualWidth) ? projectName : null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tasklist/listitemprojectlabel.xaml", UriKind.Relative);
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
			ProjectName = (ListItemProjectLabel)target;
			ProjectName.MouseLeftButtonUp += OnProjectNameClick;
			ProjectName.PreviewMouseLeftButtonDown += OnProjectNameMouseDown;
			break;
		case 2:
			ProjectTitle = (EmjTextBlock)target;
			ProjectTitle.SizeChanged += OnTitleSizeChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
