using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels.AI;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.AI;

public class AIAudioTransResultWindow : Window, IComponentConnector
{
	internal AnimateGroupTitle SwitchTitle;

	internal SmoothScrollViewer SummaryScroll;

	internal MarkDownEditor SummaryContent;

	private bool _contentLoaded;

	private AIAudioTransResultViewModel viewModel { get; set; }

	public AIAudioTransResultWindow(AttachmentModel attachModel, string projectId, AIAudioViewType viewType)
	{
		base.DataContext = (viewModel = new AIAudioTransResultViewModel(attachModel, projectId, viewType));
		InitializeComponent();
		AIAudioTransResultViewModel aIAudioTransResultViewModel2 = viewModel;
		aIAudioTransResultViewModel2.OnClose = (Action)Delegate.Combine(aIAudioTransResultViewModel2.OnClose, new Action(OnClose));
		viewModel.PropertyChanged += ViewModel_PropertyChanged;
		Size lastAIAudioWindowSize = LocalSettings.Settings.ExtraSettings.LastAIAudioWindowSize;
		if (lastAIAudioWindowSize != Size.Empty && lastAIAudioWindowSize != default(Size) && lastAIAudioWindowSize.Width >= base.MinWidth && lastAIAudioWindowSize.Height >= base.MinHeight && lastAIAudioWindowSize.Width < SystemParameters.PrimaryScreenWidth && lastAIAudioWindowSize.Height < SystemParameters.PrimaryScreenHeight)
		{
			base.Width = lastAIAudioWindowSize.Width;
			base.Height = lastAIAudioWindowSize.Height;
		}
		SummaryContent.StyleEnabled = false;
	}

	private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "Summary")
		{
			SummaryContent.SetText(viewModel.Summary);
		}
	}

	private void OnClose()
	{
		Close();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void DragGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonDown(e);
		DragMove();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		viewModel.PropertyChanged -= ViewModel_PropertyChanged;
		AIAudioTransResultViewModel aIAudioTransResultViewModel = viewModel;
		aIAudioTransResultViewModel.OnClose = (Action)Delegate.Remove(aIAudioTransResultViewModel.OnClose, new Action(OnClose));
		viewModel.Dispose();
		LocalSettings.Settings.ExtraSettings.LastAIAudioWindowSize = new Size(base.ActualWidth, base.ActualHeight);
		LocalSettings.Settings.Save();
		base.Owner?.Activate();
	}

	private void SummaryContent_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		MouseWheelEventArgs e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
		{
			RoutedEvent = UIElement.MouseWheelEvent,
			Source = sender
		};
		(((Control)sender).Parent as UIElement)?.RaiseEvent(e2);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/aiaudiotransresultwindow.xaml", UriKind.Relative);
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
			((AIAudioTransResultWindow)target).Closing += Window_Closing;
			break;
		case 2:
			((Grid)target).MouseLeftButtonDown += DragGrid_MouseLeftButtonDown;
			break;
		case 3:
			SwitchTitle = (AnimateGroupTitle)target;
			break;
		case 4:
			SummaryScroll = (SmoothScrollViewer)target;
			break;
		case 5:
			SummaryContent = (MarkDownEditor)target;
			break;
		case 6:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
