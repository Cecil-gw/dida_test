using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Eisenhower;

namespace ticktick_WPF.Views.Calendar;

public class MatrixWindow : IndependentWindow
{
	public new static MatrixWindow Window;

	public new static string Name = DisplayModule.Matrix.ToString();

	private MatrixContainer _matrix;

	public static bool IsShowing => Window != null;

	private MatrixWindow(WindowModel windowModel)
		: base(windowModel)
	{
		base.MinWidth = 580.0;
		base.MinHeight = 462.0;
		Id = Name;
		_matrix = new MatrixContainer();
		Container.SetResourceReference(Control.BackgroundProperty, "CardBackBottomColor");
		Container.Children.Add(_matrix);
		base.Title = Utils.GetString("Matrix");
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
		}
		else
		{
			if (windowModel == null || windowModel.Closed)
			{
				IndependentWindow.AddShowEvent("matrix");
			}
			if (windowModel != null)
			{
				await WindowModelDao.OpenWindow(windowModel);
			}
			else
			{
				windowModel = await IndependentWindow.GetNewWindowModel(Name);
			}
			Window = new MatrixWindow(windowModel);
			Window.Show();
		}
		Window.LoadTask();
	}

	private void LoadTask()
	{
		_matrix.LoadTask(restore: false);
	}

	protected override void OnClosing()
	{
		Container.Children.Clear();
		Window = null;
	}

	public override void BatchSetPriorityCommand(int priority)
	{
		_matrix?.TryBatchSetPriority(priority);
	}

	public override void BatchSetDateCommand(DateTime? date)
	{
		_matrix?.TryBatchSetDate(date);
	}

	public override void BatchPinTaskCommand()
	{
		_matrix?.BatchPinTask();
	}

	public override void BatchOpenStickyCommand()
	{
		_matrix?.BatchOpenSticky();
	}

	public override void BatchDeleteCommand()
	{
		_matrix?.BatchDeleteTask();
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
