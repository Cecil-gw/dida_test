using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Calendar;

public class FocusWindow : IndependentWindow
{
	public new static FocusWindow Window;

	public new static string Name = DisplayModule.Pomo.ToString();

	private FocusView _focus;

	public static bool IsShowing => Window != null;

	private FocusWindow(WindowModel windowModel)
		: base(windowModel)
	{
		Id = Name;
		base.MinWidth = 400.0;
		base.MinHeight = 600.0;
		_focus = new FocusView
		{
			MinWidth = 400.0
		};
		Container.Children.Add(_focus);
		base.Title = Utils.GetString("PomoFocus");
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
			IndependentWindow.AddShowEvent("focus");
		}
		if (windowModel != null)
		{
			await WindowModelDao.OpenWindow(windowModel);
		}
		else
		{
			windowModel = await IndependentWindow.GetNewWindowModel(Name);
		}
		Window = new FocusWindow(windowModel);
		Window.Show();
		Window.SetBackBrush();
	}

	private void Reload()
	{
		_focus?.Reload();
	}

	protected override void OnClosing()
	{
		Container.Children.Clear();
		Window = null;
	}

	public override void Print()
	{
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
