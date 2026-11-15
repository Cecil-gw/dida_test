using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class BesideTiming : UserControl, ISideMiniFocus, IComponentConnector
{
	internal Grid Container;

	internal Path BottomPath;

	internal Path TopPath;

	internal RectangleGeometry ClipGeo;

	private bool _contentLoaded;

	public BesideTiming()
	{
		InitializeComponent();
		base.SizeChanged += OnSizeChanged;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetPercent();
		BottomPath.Data = Utils.GetIcon((e.NewSize.Height < 100.0) ? "FocusSideTimingPath01" : ((e.NewSize.Height > 150.0) ? "FocusSideTimingPath03" : "FocusSideTimingPath02"));
	}

	public void SetPercent()
	{
		ClipGeo.Rect = new Rect(0.0, Math.Max(0.0, (1.0 - TickFocusManager.Config.GetDisplayPercent()) * base.ActualHeight), 4.0, base.ActualHeight);
	}

	public void OnStatusChanged()
	{
		if (TickFocusManager.Status == PomoStatus.Pause)
		{
			TopPath.SetResourceReference(Shape.FillProperty, "TickRedColor");
		}
		else
		{
			TopPath.SetResourceReference(Shape.FillProperty, "PrimaryColor");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/besidetiming.xaml", UriKind.Relative);
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
			Container = (Grid)target;
			break;
		case 2:
			BottomPath = (Path)target;
			break;
		case 3:
			TopPath = (Path)target;
			break;
		case 4:
			ClipGeo = (RectangleGeometry)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
