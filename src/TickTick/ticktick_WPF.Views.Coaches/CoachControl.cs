using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using XamlAnimatedGif;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Coaches;

public class CoachControl : UserControl, IComponentConnector
{
	private int _count;

	private int _index;

	internal Image Image;

	internal TextBlock IndexText;

	private bool _contentLoaded;

	public CoachControl(CoachModel linkedCoach)
	{
		InitializeComponent();
		base.DataContext = linkedCoach;
		_index = 0;
		_count = 1;
		SetImage();
		while (linkedCoach.Next != null)
		{
			_count++;
			linkedCoach = linkedCoach.Next;
		}
		UpdateIndexText();
	}

	private void SetImage()
	{
		if (base.DataContext is CoachModel coachModel)
		{
			Image.SetValue(AnimationBehavior.SourceUriProperty, new Uri(coachModel.GifPath));
		}
	}

	private void UpdateIndexText()
	{
		UserActCollectUtils.AddClickEvent("timeline", "user_guide", "tip" + (_index + 1));
		IndexText.Text = $"{_index + 1} / {_count}";
	}

	private void OnNextClicked(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is CoachModel coachModel)
		{
			base.DataContext = coachModel.Next;
			_index++;
			SetImage();
			UpdateIndexText();
		}
	}

	private void OnPreClicked(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is CoachModel coachModel)
		{
			base.DataContext = coachModel.Pre;
			_index--;
			SetImage();
			UpdateIndexText();
		}
	}

	private void OnCloseMouseUp(object sender, MouseButtonEventArgs e)
	{
		App.Window.EndCoach();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/coaches/coachcontrol.xaml", UriKind.Relative);
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
			((Border)target).MouseLeftButtonUp += OnCloseMouseUp;
			break;
		case 2:
			Image = (Image)target;
			break;
		case 3:
			IndexText = (TextBlock)target;
			break;
		case 4:
			((Button)target).Click += OnPreClicked;
			break;
		case 5:
			((Button)target).Click += OnNextClicked;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
