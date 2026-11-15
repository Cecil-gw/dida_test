using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.Views;

namespace ticktick_WPF.Views.Calendar;

public class CountdownWindow : IndependentWindow
{
	public new static CountdownWindow Window;

	public new static string Name = DisplayModule.CountDown.ToString();

	private CountDownContainer _countDown;

	public static bool IsShowing => Window != null;

	private CountdownWindow(WindowModel windowModel)
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
		_countDown = new CountDownContainer();
		Container.Children.Add(_countDown);
		base.Title = Utils.GetString("Countdown");
		Container.SetResourceReference(Control.BackgroundProperty, "CardBackBottomColor");
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
			IndependentWindow.AddShowEvent("countdown");
		}
		if (windowModel != null)
		{
			await WindowModelDao.OpenWindow(windowModel);
		}
		else
		{
			windowModel = await IndependentWindow.GetNewWindowModel(Name);
		}
		Window = new CountdownWindow(windowModel);
		Window.Show();
	}

	private void Reload()
	{
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
