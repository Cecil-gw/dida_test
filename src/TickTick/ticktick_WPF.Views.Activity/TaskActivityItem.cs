using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Activity;

public class TaskActivityItem : UserControl, IComponentConnector
{
	internal Grid ContentGrid;

	internal EmojiEditor ContentTxt;

	internal StackPanel ExpandPanel;

	internal TextBlock ExpandOrCollapseBtn;

	internal Path ExpandPath;

	internal RotateTransform ExpandPathRotate;

	private bool _contentLoaded;

	public TaskActivityItem()
	{
		InitializeComponent();
		ContentTxt.TextChanged += OnTextChanged;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
	}

	private async void OnTextChanged(object sender, EventArgs e)
	{
		UpdateLayout();
		await Task.Delay(10);
		Point visualPosition = ContentTxt.EditBox.TextArea.TextView.GetVisualPosition(new TextViewPosition(ContentTxt.EditBox.Document.GetLocation(ContentTxt.Text.Length)), VisualYPosition.LineBottom);
		double num = (double)FindResource("Height68");
		if (visualPosition.Y > num)
		{
			ExpandPanel.Visibility = Visibility.Visible;
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is TaskActivityViewModel taskActivityViewModel && !string.IsNullOrEmpty(taskActivityViewModel.Description))
		{
			ExpandPanel.Visibility = Visibility.Collapsed;
			ExpandPathRotate.Angle = ((!taskActivityViewModel.Fold) ? 180 : 0);
			ExpandOrCollapseBtn.Text = Utils.GetString(taskActivityViewModel.Fold ? "expand" : "Collapse");
			ContentTxt.Text = ((taskActivityViewModel.Fold && taskActivityViewModel.Description.Length > taskActivityViewModel.MaxDescLength) ? taskActivityViewModel.Description.Substring(0, taskActivityViewModel.MaxDescLength) : taskActivityViewModel.Description);
			ContentTxt.Visibility = Visibility.Visible;
		}
		else
		{
			ExpandPanel.Visibility = Visibility.Collapsed;
			ContentTxt.Visibility = Visibility.Collapsed;
		}
	}

	private void ExpandClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is TaskActivityViewModel taskActivityViewModel)
		{
			if (ContentTxt.Text != taskActivityViewModel.Description)
			{
				ContentTxt.TextChanged -= OnTextChanged;
				ContentTxt.Text = taskActivityViewModel.Description;
				ContentTxt.TextChanged += OnTextChanged;
			}
			if (taskActivityViewModel.Fold)
			{
				taskActivityViewModel.Fold = false;
				ExpandOrCollapseBtn.Text = Utils.GetString("Collapse");
				ExpandPathRotate.Angle = 180.0;
			}
			else
			{
				taskActivityViewModel.Fold = true;
				ExpandOrCollapseBtn.Text = Utils.GetString("expand");
				ExpandPathRotate.Angle = 0.0;
			}
		}
	}

	private void OnBringIntoView(object sender, RequestBringIntoViewEventArgs e)
	{
		e.Handled = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/activity/taskactivityitem.xaml", UriKind.Relative);
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
			((TaskActivityItem)target).DataContextChanged += OnDataBind;
			break;
		case 2:
			ContentGrid = (Grid)target;
			break;
		case 3:
			ContentTxt = (EmojiEditor)target;
			break;
		case 4:
			ExpandPanel = (StackPanel)target;
			ExpandPanel.MouseLeftButtonUp += ExpandClick;
			break;
		case 5:
			ExpandOrCollapseBtn = (TextBlock)target;
			break;
		case 6:
			ExpandPath = (Path)target;
			break;
		case 7:
			ExpandPathRotate = (RotateTransform)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
