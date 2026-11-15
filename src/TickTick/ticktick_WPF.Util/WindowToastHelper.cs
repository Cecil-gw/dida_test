using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TickTickUtils;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Pomo.MainFocus;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Util;

public static class WindowToastHelper
{
	public static async Task ToastString(Grid grid, string text, double maxWidth = 600.0, double height = 40.0, double fontSize = 13.0)
	{
		Application.Current?.Dispatcher?.Invoke((Func<Task>)async delegate
		{
			Border border = new Border
			{
				Background = ThemeUtil.GetColor("ToastBackground"),
				CornerRadius = new CornerRadius(6.0),
				Height = height
			};
			Border border2 = new Border
			{
				BorderBrush = ThemeUtil.GetColor("BaseColorOpacity0_10"),
				CornerRadius = new CornerRadius(6.0),
				BorderThickness = new Thickness(1.0),
				Height = height
			};
			TextBlock child = new TextBlock
			{
				Text = text,
				TextAlignment = TextAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				Padding = new Thickness(fontSize, 0.0, fontSize, 0.0),
				Foreground = Brushes.White,
				MaxWidth = maxWidth,
				FontSize = fontSize,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			border.Child = border2;
			border2.Child = child;
			await ShowAndHideToast(grid, border);
		});
	}

	public static async Task ShowAndHideToast(Grid grid, FrameworkElement uiElement)
	{
		Application.Current?.Dispatcher?.Invoke((Func<Task>)async delegate
		{
			if (grid == null)
			{
				grid = App.Window.ToastGrid;
			}
			try
			{
				uiElement.VerticalAlignment = VerticalAlignment.Bottom;
				if (grid.Children.Count > 0)
				{
					foreach (object child in grid.Children)
					{
						if (child is UndoToast undoToast)
						{
							undoToast.OnFinished();
						}
					}
				}
				grid.Children.Clear();
				grid.Children.Add(uiElement);
				GetShowStory().Begin(uiElement);
				Storyboard hideStory = new Storyboard();
				DoubleAnimation doubleAnimation = new DoubleAnimation
				{
					BeginTime = TimeSpan.FromSeconds(3.0),
					Duration = new Duration(TimeSpan.FromMilliseconds(200.0)),
					From = 1.0,
					To = 0.0
				};
				hideStory.Children.Add(doubleAnimation);
				Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
				Storyboard.SetTarget(doubleAnimation, uiElement);
				uiElement.MouseEnter += MouseEnter;
				hideStory.Completed += async delegate
				{
					uiElement.MouseEnter -= MouseEnter;
					uiElement.MouseLeave -= MouseLeave;
					if (grid.Children.Contains(uiElement))
					{
						grid.Children.Remove(uiElement);
					}
					await Task.Delay(300);
					if (uiElement is UndoToast undoToast2)
					{
						undoToast2.OnFinished();
					}
					if (uiElement is MoveToastControl)
					{
						TaskDragUndoModel.TryFinishAll();
					}
					if (uiElement is CustomUndoToast customUndoToast)
					{
						customUndoToast.TryFinish();
					}
				};
				hideStory.Begin();
				void MouseEnter(object sender, MouseEventArgs e)
				{
					hideStory.Pause();
					uiElement.MouseEnter -= MouseEnter;
					uiElement.MouseLeave += MouseLeave;
				}
				void MouseLeave(object sender, MouseEventArgs e)
				{
					hideStory.Resume();
					uiElement.MouseEnter += MouseEnter;
					uiElement.MouseLeave -= MouseLeave;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
			}
		});
	}

	private static Storyboard GetShowStory()
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation doubleAnimation = new DoubleAnimation
		{
			BeginTime = new TimeSpan(0L),
			Duration = new Duration(TimeSpan.FromMilliseconds(200.0)),
			From = 0.0,
			To = 1.0
		};
		storyboard.Children.Add(doubleAnimation);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(UIElement.OpacityProperty));
		return storyboard;
	}
}
