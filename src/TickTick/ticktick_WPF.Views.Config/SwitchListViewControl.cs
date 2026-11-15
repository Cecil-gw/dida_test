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

namespace ticktick_WPF.Views.Config;

public class SwitchListViewControl : StackPanel, ITabControl, IComponentConnector
{
	private bool? _selectList = false;

	private bool? _selectKanban = false;

	private bool? _selectTimeline = false;

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(SwitchListViewControl), new PropertyMetadata(-10, null));

	internal SwitchListViewControl Root;

	internal Border ListBorder;

	internal Path ListPath;

	internal Border KanbanBorder;

	internal Path KanbanPath;

	internal Border TimelineBorder;

	internal Path TimelinePath;

	private bool _contentLoaded;

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public event EventHandler<string> ViewSelected;

	public SwitchListViewControl()
	{
		InitializeComponent();
	}

	public void SetButtonStatus(bool? selectList, bool? selectKanban, bool? selectTimeline)
	{
		if (_selectList != selectList)
		{
			_selectList = selectList;
			ListBorder.Cursor = ((!_selectList.HasValue) ? Cursors.No : Cursors.Hand);
			ListPath.SetResourceReference(Shape.FillProperty, (!_selectList.HasValue) ? "BaseColorOpacity10" : (_selectList.Value ? "TextAccentColor" : "BaseColorOpacity60"));
		}
		if (_selectKanban != selectKanban)
		{
			_selectKanban = selectKanban;
			KanbanBorder.Cursor = ((!_selectKanban.HasValue) ? Cursors.No : Cursors.Hand);
			KanbanPath.SetResourceReference(Shape.FillProperty, (!_selectKanban.HasValue) ? "BaseColorOpacity10" : (_selectKanban.Value ? "TextAccentColor" : "BaseColorOpacity60"));
		}
		if (_selectTimeline != selectTimeline)
		{
			_selectTimeline = selectTimeline;
			TimelineBorder.Cursor = ((!_selectTimeline.HasValue) ? Cursors.No : Cursors.Hand);
			TimelinePath.SetResourceReference(Shape.FillProperty, (!_selectTimeline.HasValue) ? "BaseColorOpacity10" : (_selectTimeline.Value ? "TextAccentColor" : "BaseColorOpacity60"));
		}
	}

	private void SwitchViewClick(object sender, MouseButtonEventArgs e)
	{
		string text = (sender as FrameworkElement)?.Tag as string;
		e.Handled = true;
		switch (text)
		{
		case "list":
			if (_selectList.HasValue && !_selectList.Value)
			{
				this.ViewSelected?.Invoke(this, "list");
			}
			break;
		case "kanban":
			if (_selectKanban.HasValue && !_selectKanban.Value)
			{
				this.ViewSelected?.Invoke(this, "kanban");
			}
			break;
		case "timeline":
			if (_selectTimeline.HasValue && !_selectTimeline.Value)
			{
				this.ViewSelected?.Invoke(this, "timeline");
			}
			break;
		}
	}

	public bool HandleTab(bool shift)
	{
		switch (SelectedIndex)
		{
		case 0:
			if (shift)
			{
				SelectedIndex = -1;
				return false;
			}
			SelectedIndex = (_selectKanban.HasValue ? 1 : 2);
			break;
		case 1:
			SelectedIndex = ((!shift) ? 2 : 0);
			break;
		case 2:
			if (!shift)
			{
				SelectedIndex = -1;
				return false;
			}
			SelectedIndex = (_selectKanban.HasValue ? 1 : 0);
			break;
		default:
			SelectedIndex = (shift ? 2 : 0);
			break;
		}
		return true;
	}

	public bool HandleEnter()
	{
		if (SelectedIndex >= 0)
		{
			switch (SelectedIndex)
			{
			case 0:
				if (_selectList.HasValue && !_selectList.Value)
				{
					this.ViewSelected?.Invoke(this, "list");
				}
				break;
			case 1:
				if (_selectKanban.HasValue && !_selectKanban.Value)
				{
					this.ViewSelected?.Invoke(this, "kanban");
				}
				break;
			case 2:
				if (_selectTimeline.HasValue && !_selectTimeline.Value)
				{
					this.ViewSelected?.Invoke(this, "timeline");
				}
				break;
			}
			return true;
		}
		return false;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		if (SelectedIndex < -1)
		{
			if (isUp)
			{
				SelectedIndex = -1;
				return false;
			}
			SelectedIndex = 0;
			return true;
		}
		if (SelectedIndex == -1)
		{
			SelectedIndex = 0;
		}
		else
		{
			SelectedIndex = -1;
		}
		return false;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		switch (SelectedIndex)
		{
		case 0:
			SelectedIndex = ((!isLeft && _selectKanban.HasValue) ? 1 : 2);
			break;
		case 1:
			SelectedIndex = ((!isLeft) ? 2 : 0);
			break;
		case 2:
			SelectedIndex = ((isLeft && _selectKanban.HasValue) ? 1 : 0);
			break;
		default:
			SelectedIndex = (isLeft ? 2 : 0);
			break;
		}
		return true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/switchlistviewcontrol.xaml", UriKind.Relative);
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
			Root = (SwitchListViewControl)target;
			break;
		case 2:
			ListBorder = (Border)target;
			ListBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 3:
			ListPath = (Path)target;
			break;
		case 4:
			KanbanBorder = (Border)target;
			KanbanBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 5:
			KanbanPath = (Path)target;
			break;
		case 6:
			TimelineBorder = (Border)target;
			TimelineBorder.MouseLeftButtonUp += SwitchViewClick;
			break;
		case 7:
			TimelinePath = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
