using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Views.Calendar;

public class HabitWindow : IndependentWindow
{
	public new static HabitWindow Window;

	public new static string Name = DisplayModule.Habit.ToString();

	private HabitContainer _habit;

	public static bool IsShowing => Window != null;

	private HabitWindow(WindowModel windowModel)
		: base(windowModel)
	{
		if (double.TryParse(windowModel.Data, out var result))
		{
			Column1.Width = new GridLength(1.0, GridUnitType.Star);
			Column2.Width = new GridLength((result > 0.0) ? result : 1.0, GridUnitType.Star);
		}
		base.MinWidth = 500.0;
		base.MinHeight = 500.0;
		Id = Name;
		_habit = new HabitContainer(isMainHabit: false);
		Container.Children.Add(_habit);
		base.Title = Utils.GetString("Habit");
		Container.SetResourceReference(Control.BackgroundProperty, "CardBackBottomColor");
		_habit.LoadHabits();
		base.MouseLeftButtonUp += OnWindowMouseUp;
		base.MouseLeftButtonDown += OnWindowMouseDown;
	}

	private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
	{
		_mouseDown = true;
	}

	private async void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!_mouseDown)
		{
			return;
		}
		_mouseDown = false;
		if (Utils.GetMousePointVisibleItem<TextBox>(e, this) == null)
		{
			await Task.Delay(100);
			if (base.IsActive && !e.Handled)
			{
				TryFocus();
			}
		}
	}

	public async Task TryFocus()
	{
		await Task.Delay(10);
		Keyboard.ClearFocus();
		if (base.IsActive)
		{
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
		}
	}

	public static async Task ShowWindow(bool force = true)
	{
		WindowModel windowModel = await WindowModelDao.GetModelByIdAsync(Name);
		if (!force && (windowModel == null || windowModel.Closed))
		{
			return;
		}
		if (Window != null)
		{
			Window.Show();
			Window.Activate();
			Window.Reload();
			return;
		}
		if (windowModel == null || windowModel.Closed)
		{
			IndependentWindow.AddShowEvent("habit");
		}
		if (windowModel != null)
		{
			await WindowModelDao.OpenWindow(windowModel);
		}
		else
		{
			windowModel = await IndependentWindow.GetNewWindowModel(Name);
		}
		Window = new HabitWindow(windowModel);
		Window.Show();
	}

	private void Reload()
	{
		_habit.LoadHabits();
	}

	protected override void OnClosing()
	{
		Container.Children.Clear();
		Window = null;
	}

	public static void OpenOrCloseWindow(Window window)
	{
		if (Window != null)
		{
			Window.Close();
		}
		else if (IndependentWindow.CheckCount(window))
		{
			ShowWindow();
		}
	}
}
