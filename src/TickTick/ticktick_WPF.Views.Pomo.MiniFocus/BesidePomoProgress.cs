using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class BesidePomoProgress : UserControl, ISideMiniFocus, IComponentConnector
{
	internal Rectangle TopPomoRect;

	internal RectangleGeometry ClipGeo;

	private bool _contentLoaded;

	public BesidePomoProgress()
	{
		InitializeComponent();
		OnStatusChanged();
		base.SizeChanged += OnSizeChanged;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetPercent();
	}

	public void SetPercent()
	{
		ClipGeo.Rect = new Rect(0.0, Math.Max(0.0, (1.0 - TickFocusManager.Config.GetDisplayPercent()) * base.ActualHeight), 4.0, base.ActualHeight);
	}

	public void OnStatusChanged()
	{
		switch (TickFocusManager.Status)
		{
		case PomoStatus.Relaxing:
			TopPomoRect.SetResourceReference(Shape.FillProperty, "PomoGreen");
			break;
		case PomoStatus.Pause:
			TopPomoRect.SetResourceReference(Shape.FillProperty, "TickRedColor");
			break;
		default:
			TopPomoRect.SetResourceReference(Shape.FillProperty, "PrimaryColor");
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/minifocus/besidepomoprogress.xaml", UriKind.Relative);
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
			TopPomoRect = (Rectangle)target;
			break;
		case 2:
			ClipGeo = (RectangleGeometry)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
