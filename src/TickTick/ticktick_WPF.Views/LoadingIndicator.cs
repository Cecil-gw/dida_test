using System.Windows;
using System.Windows.Controls;

namespace ticktick_WPF.Views;

[TemplatePart(Name = "Border", Type = typeof(Border))]
public class LoadingIndicator : Control
{
	public static readonly DependencyProperty SpeedRatioProperty = DependencyProperty.Register("SpeedRatio", typeof(double), typeof(LoadingIndicator), new PropertyMetadata(1.0, delegate(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		LoadingIndicator loadingIndicator = (LoadingIndicator)o;
		if (loadingIndicator.PartBorder == null || !loadingIndicator.IsActive)
		{
			return;
		}
		foreach (VisualStateGroup visualStateGroup2 in VisualStateManager.GetVisualStateGroups(loadingIndicator.PartBorder))
		{
			if (visualStateGroup2.Name == "ActiveStates")
			{
				foreach (VisualState state in visualStateGroup2.States)
				{
					if (state.Name == "Active")
					{
						state.Storyboard.SetSpeedRatio(loadingIndicator.PartBorder, (double)e.NewValue);
					}
				}
			}
		}
	}));

	public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(LoadingIndicator), new PropertyMetadata(true, delegate(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		LoadingIndicator loadingIndicator = (LoadingIndicator)o;
		if (loadingIndicator.PartBorder != null)
		{
			if ((bool)e.NewValue)
			{
				VisualStateManager.GoToElementState(loadingIndicator.PartBorder, "Active", useTransitions: false);
				loadingIndicator.PartBorder.Visibility = Visibility.Visible;
				{
					foreach (VisualStateGroup visualStateGroup3 in VisualStateManager.GetVisualStateGroups(loadingIndicator.PartBorder))
					{
						if (visualStateGroup3.Name == "ActiveStates")
						{
							foreach (VisualState state2 in visualStateGroup3.States)
							{
								if (state2.Name == "Active")
								{
									state2.Storyboard.SetSpeedRatio(loadingIndicator.PartBorder, loadingIndicator.SpeedRatio);
								}
							}
						}
					}
					return;
				}
			}
			VisualStateManager.GoToElementState(loadingIndicator.PartBorder, "Inactive", useTransitions: false);
			loadingIndicator.PartBorder.Visibility = Visibility.Collapsed;
		}
	}));

	protected Border PartBorder;

	public double SpeedRatio
	{
		get
		{
			return (double)GetValue(SpeedRatioProperty);
		}
		set
		{
			SetValue(SpeedRatioProperty, value);
		}
	}

	public bool IsActive
	{
		get
		{
			return (bool)GetValue(IsActiveProperty);
		}
		set
		{
			SetValue(IsActiveProperty, value);
		}
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		PartBorder = (Border)GetTemplateChild("PART_Border");
		if (PartBorder == null)
		{
			return;
		}
		VisualStateManager.GoToElementState(PartBorder, IsActive ? "Active" : "Inactive", useTransitions: false);
		foreach (VisualStateGroup visualStateGroup in VisualStateManager.GetVisualStateGroups(PartBorder))
		{
			if (!(visualStateGroup.Name == "ActiveStates"))
			{
				continue;
			}
			foreach (VisualState state in visualStateGroup.States)
			{
				if (state.Name == "Active")
				{
					state.Storyboard.SetSpeedRatio(PartBorder, SpeedRatio);
				}
			}
		}
		PartBorder.Visibility = ((!IsActive) ? Visibility.Collapsed : Visibility.Visible);
	}
}
