using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class FocusObjIcon : Border
{
	private Path _icon;

	private EmjTextBlock _iconText;

	private double _width;

	public FocusObjIcon(double size)
	{
		_width = size;
		base.Height = size;
		base.Width = size;
		base.Background = Brushes.Transparent;
		InitIcon(size);
	}

	private void InitIcon(double size)
	{
		_icon = new Path
		{
			Height = size - 6.0,
			Width = size - 6.0,
			Stretch = Stretch.Uniform,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_iconText = new EmjTextBlock
		{
			FontSize = size - 10.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Center
		};
		Child = _icon;
		SetIcon();
		SetFocusIcon(null, 0);
	}

	public void SetIcon()
	{
		if (_icon != null)
		{
			_icon.SetResourceReference(Shape.FillProperty, (TickFocusManager.Status == PomoStatus.Relaxing) ? "PomoGreen" : "PrimaryColor");
			_icon.Data = Utils.GetIcon(TickFocusManager.IsPomo ? "IcPomo" : "IcPomoTimer");
		}
	}

	public void SetIconAndColor(string icon, string color)
	{
		base.Background = new ImageBrush
		{
			ImageSource = HabitService.GetIcon(icon, color)
		};
		Child = null;
	}

	public async void SetFocusIcon(string id, int type)
	{
		if (!string.IsNullOrWhiteSpace(id))
		{
			bool flag = type == 2;
			if (type == 1 || flag)
			{
				string icon = null;
				string color = null;
				if (flag)
				{
					TimerModel timerModel = await TimerDao.GetTimerById(id);
					if (timerModel != null)
					{
						icon = timerModel.Icon;
						color = timerModel.Color;
					}
				}
				else
				{
					HabitModel habitModel = await HabitDao.GetHabitById(id);
					if (habitModel != null)
					{
						icon = habitModel.IconRes;
						color = habitModel.Color;
					}
				}
				if (!string.IsNullOrEmpty(icon))
				{
					SetIconAndColor(icon, color);
					return;
				}
			}
			else
			{
				ProjectModel projectById = CacheManager.GetProjectById(TaskCache.GetTaskById(id)?.ProjectId);
				if (projectById != null && EmojiHelper.StartWithEmoji(projectById.name))
				{
					Child = _iconText;
					base.Background = Brushes.Transparent;
					_iconText.FontSize = _width - 6.0;
					_iconText.Margin = new Thickness(-3.0, -2.0, 0.0, 0.0);
					_iconText.Text = EmojiHelper.GetEmojiIcon(projectById.name);
					return;
				}
			}
		}
		Child = _icon;
		base.Background = Brushes.Transparent;
	}
}
