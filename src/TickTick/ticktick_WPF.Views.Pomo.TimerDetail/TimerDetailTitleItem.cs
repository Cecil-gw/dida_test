using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Emoji.Wpf;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailTitleItem : DockPanel
{
	private FocusObjIcon _focusIcon = new FocusObjIcon(40.0)
	{
		VerticalAlignment = VerticalAlignment.Center,
		Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
	};

	private EmjTextBlock _title = new EmjTextBlock
	{
		FontSize = 18.0,
		Height = 25.0,
		TextWrapping = TextWrapping.Wrap,
		ClipToBounds = true,
		TextTrimming = TextTrimming.CharacterEllipsis,
		Margin = new Thickness(0.0, 10.0, 0.0, 2.0)
	};

	private TextBlock _typeText = new TextBlock
	{
		FontSize = 12.0
	};

	private Border _navigateButton = new Border
	{
		Width = 20.0,
		Height = 20.0,
		VerticalAlignment = VerticalAlignment.Center,
		Cursor = Cursors.Hand
	};

	public TimerDetailTitleItem()
	{
		base.Margin = new Thickness(22.0, 0.0, 22.0, 0.0);
		base.Height = 62.0;
		_focusIcon.SetValue(DockPanel.DockProperty, Dock.Left);
		base.Children.Add(_focusIcon);
		_navigateButton.SetValue(DockPanel.DockProperty, Dock.Right);
		base.Children.Add(_navigateButton);
		_title.SetValue(DockPanel.DockProperty, Dock.Top);
		_title.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity100");
		base.Children.Add(_title);
		_typeText.SetValue(DockPanel.DockProperty, Dock.Right);
		_typeText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity60");
		base.Children.Add(_typeText);
		base.DataContextChanged += OnDataBind;
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is TimerDetailTitleViewModel timerDetailTitleViewModel)
		{
			_focusIcon.SetIconAndColor(timerDetailTitleViewModel.TModel.Icon, timerDetailTitleViewModel.TModel.Color);
			_title.Text = timerDetailTitleViewModel.TModel.Name;
			_typeText.Text = ((timerDetailTitleViewModel.TModel.Type == "pomodoro") ? string.Format("{0} {1} m", Utils.GetString("PomoTimer2"), timerDetailTitleViewModel.TModel.PomodoroTime) : Utils.GetString("Timing"));
		}
	}
}
