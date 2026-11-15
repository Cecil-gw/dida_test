using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Pomo.FocusStatistics;
using ticktick_WPF.Views.Pomo.TimerDetail;

namespace ticktick_WPF.Views.Pomo.FocusDetail;

public sealed class FocusDetailControl : Border
{
	private readonly StackPanel _focusContainer;

	private readonly FocusStatisticsPanel _statistics;

	private EmojiEditor _noteEditor;

	private FocusDetailTitle _focusDetailTitle;

	private FocusTimeline _timeline;

	private TextBlock _noteLengthText;

	private TimerDetailPanel _timerDetail;

	public FocusDetailControl()
	{
		_focusContainer = new StackPanel
		{
			Margin = new Thickness(22.0, 40.0, 22.0, 20.0)
		};
		_statistics = new FocusStatisticsPanel
		{
			Margin = new Thickness(0.0, 25.0, 6.0, 20.0)
		};
		_timerDetail = new TimerDetailPanel
		{
			Margin = new Thickness(0.0, 25.0, 6.0, 20.0)
		};
		InitFocusChildren();
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
		base.SizeChanged += OnSizeChanged;
	}

	private void InitFocusChildren()
	{
		InitTitle();
		InitTimeline();
		InitNote();
	}

	private void InitTitle()
	{
		_focusDetailTitle = new FocusDetailTitle
		{
			Height = 50.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 20.0)
		};
		_focusContainer.Children.Add(_focusDetailTitle);
	}

	private void InitTimeline()
	{
		_timeline = new FocusTimeline
		{
			Margin = new Thickness(-20.0, 0.0, 0.0, 40.0)
		};
		_focusContainer.Children.Add(_timeline);
	}

	private void InitNote()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
		};
		_noteEditor = new EmojiEditor
		{
			FontSize = 13.0,
			MaxLength = 500,
			BorderThickness = new Thickness(1.0),
			WordWrap = true,
			AcceptReturn = true,
			Height = 120.0,
			Background = Brushes.Transparent,
			BorderCorner = new CornerRadius(8.0),
			TextVerticalAlignment = VerticalAlignment.Top,
			Padding = new Thickness(12.0, 8.0, 12.0, 20.0),
			Cursor = Cursors.IBeam,
			Tag = Utils.GetString("FocusNoteHint"),
			VerticalAlignment = VerticalAlignment.Top,
			Text = TickFocusManager.Config.Note
		};
		_noteEditor.SetResourceReference(EmojiEditor.ForegroundProperty, "BaseColorOpacity80");
		_noteEditor.SetResourceReference(EmojiEditor.BorderBackgroundProperty, "BaseColorOpacity5");
		_noteEditor.SetResourceReference(EmojiEditor.BorderBrushProperty, "BaseColorOpacity5");
		_noteEditor.TextChanged += OnNoteChanged;
		_noteEditor.GotFocus += OnNoteGotFocus;
		TextBlock textBlock = new TextBlock
		{
			Text = Utils.GetString("FocusNote"),
			Style = (Style)FindResource("Body01"),
			FontSize = 16.0
		};
		textBlock.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = _noteEditor
		});
		_focusContainer.Children.Add(textBlock);
		grid.Children.Add(_noteEditor);
		_noteLengthText = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 20.0, 6.0),
			Foreground = Brushes.Red
		};
		_noteLengthText.SetResourceReference(TextBlock.ForegroundProperty, "BaseColorOpacity40");
		_noteLengthText.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = _noteEditor
		});
		grid.Children.Add(_noteLengthText);
		_focusContainer.Children.Add(grid);
		_noteEditor.Visibility = ((TickFocusManager.Status == PomoStatus.WaitingWork) ? Visibility.Hidden : Visibility.Visible);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		TickFocusManager.StatusChanged -= OnStatusChanged;
		TickFocusManager.DurationChanged -= OnDurationChanged;
		PomoNotifier.FocusingNoteChanged -= OnFocusingNoteChanged;
		PomoNotifier.Changed -= OnPomoChanged;
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnTitleChanged, "Title");
		PropertyChangedEventManager.RemoveHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ShowStatisticsOrClock();
		TickFocusManager.StatusChanged += OnStatusChanged;
		TickFocusManager.DurationChanged += OnDurationChanged;
		PomoNotifier.FocusingNoteChanged += OnFocusingNoteChanged;
		PomoNotifier.Changed += OnPomoChanged;
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnTitleChanged, "Title");
		PropertyChangedEventManager.AddHandler(TickFocusManager.Config.FocusVModel, OnFocusChanged, "NoTask");
	}

	private void OnFocusingNoteChanged(object sender, EventArgs e)
	{
		if (_noteEditor != null && !_noteEditor.Equals(sender))
		{
			_noteEditor.TextChanged -= OnNoteChanged;
			_noteEditor.EditBox.SetTextAndOffset(TickFocusManager.Config.Note, restoreOffset: false);
			_noteEditor.TextChanged += OnNoteChanged;
		}
	}

	private void OnFocusChanged(object sender, PropertyChangedEventArgs e)
	{
		_focusDetailTitle.SetFocusIcon();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Height > 350.0)
		{
			double num = e.NewSize.Height * 0.5;
			double val = e.NewSize.Height - 340.0;
			_timeline?.SetSize(e.NewSize.Width, Math.Min(val, Math.Max(260.0, num)));
			_noteEditor.Height = Math.Max(120.0, num * 0.25);
		}
	}

	private void OnTitleChanged(object sender, PropertyChangedEventArgs e)
	{
		_focusDetailTitle.SetTitle();
	}

	private void OnNoteChanged(object sender, EventArgs e)
	{
		if (_noteEditor.Text.Length >= 400)
		{
			_noteLengthText.Text = _noteEditor.Text.Length + "/500";
		}
		else
		{
			_noteLengthText.Text = string.Empty;
		}
		TickFocusManager.Config.SetNote(sender, _noteEditor.Text);
	}

	private void OnNoteGotFocus(object sender, RoutedEventArgs e)
	{
		string actCType = TickFocusManager.GetActCType();
		UserActCollectUtils.AddClickEvent("focus", "add_focus_note", actCType);
	}

	private void OnDurationChanged()
	{
		_timeline?.DrawCurrentFocus();
	}

	private void OnStatusChanged()
	{
		base.Dispatcher.Invoke(delegate
		{
			if (!object.Equals(Child, _timerDetail))
			{
				_noteEditor.Visibility = ((TickFocusManager.Status == PomoStatus.WaitingWork) ? Visibility.Hidden : Visibility.Visible);
				if (_noteEditor.Text != TickFocusManager.Config.Note)
				{
					_noteEditor.Text = TickFocusManager.Config.Note;
				}
				ShowStatisticsOrClock();
			}
		});
	}

	private void ShowStatisticsOrClock()
	{
		FocusView focusView = Utils.FindParent<FocusView>(this);
		if (focusView != null && !focusView.GetClockPanelShow())
		{
			if (!object.Equals(Child, _statistics))
			{
				Child = _statistics;
				_statistics?.Reload();
			}
			return;
		}
		if (TickFocusManager.Status == PomoStatus.WaitingWork)
		{
			FocusView focusView2 = Utils.FindParent<FocusView>(this);
			if (focusView2 != null && !focusView2.ExistTimer())
			{
				Child = _statistics;
				_statistics?.Reload();
				return;
			}
		}
		Child = _focusContainer;
		_timeline?.DrawCurrentFocus();
		_focusDetailTitle.SetIcon();
		_focusDetailTitle.SetFocusIcon();
		_focusDetailTitle.SetTitle();
	}

	private void OnPomoChanged(object sender, PomoChangeArgs e)
	{
		if (Child == _statistics)
		{
			_statistics?.Reload();
		}
		if (Child == _focusContainer)
		{
			_timeline?.DrawCurrentFocus();
		}
	}

	public void ReloadStatistics()
	{
		if (Child == _statistics)
		{
			_statistics?.Reload();
		}
	}

	public async Task OnTimerSelect(string timerId)
	{
		TimerModel timerModel = ((!string.IsNullOrEmpty(timerId)) ? (await TimerDao.GetTimerById(timerId)) : null);
		TimerModel timerModel2 = timerModel;
		if (timerModel2 != null)
		{
			Child = _timerDetail;
			UserActCollectUtils.AddClickEvent("timer", "timer_detail", "show");
			_timerDetail.LoadTimer(timerModel2);
		}
		else
		{
			ShowStatisticsOrClock();
		}
	}

	public void SetFoldStyle(bool fold)
	{
		if (fold)
		{
			base.HorizontalAlignment = HorizontalAlignment.Right;
			base.Width = 400.0;
			SetResourceReference(Border.BackgroundProperty, "FoldAreaBackground");
			base.RenderTransform = new TranslateTransform
			{
				X = 400.0
			};
			base.Effect = new DropShadowEffect
			{
				BlurRadius = 10.0,
				Direction = 180.0,
				Opacity = 0.08
			};
			base.Visibility = Visibility.Collapsed;
		}
		else
		{
			base.HorizontalAlignment = HorizontalAlignment.Stretch;
			base.Width = double.NaN;
			base.Background = Brushes.Transparent;
			base.RenderTransform = null;
			base.Visibility = Visibility.Visible;
			base.Effect = null;
		}
	}
}
