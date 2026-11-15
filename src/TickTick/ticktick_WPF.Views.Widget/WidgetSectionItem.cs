using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Widget;

public class WidgetSectionItem : UserControl, IComponentConnector
{
	private bool _mouseDown;

	internal Path OpenIndicator;

	internal TextBlock PostPoneText;

	private bool _contentLoaded;

	protected WidgetSectionItem(string themeId)
	{
		ThemeUtil.SetTheme(themeId, this);
		InitializeComponent();
	}

	public WidgetSectionItem()
	{
		InitializeComponent();
	}

	private void OnPostPoneMouseUp(object sender, MouseButtonEventArgs e)
	{
		ProjectWidget projectWidget = Utils.FindParent<ProjectWidget>(this);
		if ((projectWidget == null || !projectWidget.IsLocked) && base.DataContext is DisplayItemModel)
		{
			Utils.FindParent<ProjectWidget>(this)?.PostPoneAll();
		}
	}

	private void OnSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		ProjectWidget projectWidget = Utils.FindParent<ProjectWidget>(this);
		if ((projectWidget == null || !projectWidget.IsLocked) && !PostPoneText.IsMouseOver)
		{
			DisplayItemModel displayItemModel = base.DataContext as DisplayItemModel;
			if (displayItemModel?.Section?.Children != null)
			{
				displayItemModel.IsOpen = !displayItemModel.IsOpen;
				projectWidget?.OpenOrCloseSection(new SectionStatus
				{
					SectionId = displayItemModel.Section.SectionId,
					IsOpen = displayItemModel.IsOpen
				});
			}
		}
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/widget/widgetsectionitem.xaml", UriKind.Relative);
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
			((Grid)target).PreviewMouseLeftButtonDown += OnMouseDown;
			((Grid)target).PreviewMouseLeftButtonUp += OnSectionClick;
			break;
		case 2:
			OpenIndicator = (Path)target;
			break;
		case 3:
			PostPoneText = (TextBlock)target;
			PostPoneText.MouseLeftButtonUp += OnPostPoneMouseUp;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
