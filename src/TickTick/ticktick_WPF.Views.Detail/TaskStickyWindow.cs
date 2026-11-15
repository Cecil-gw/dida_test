using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using KotlinModels;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Undo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Detail;

public class TaskStickyWindow : MyWindow, IToastShowWindow, IComponentConnector
{
	[Flags]
	private enum DragSizeMode
	{
		None = 0,
		Top = 1,
		Bottom = 2,
		Left = 4,
		Right = 8
	}

	private struct WindowRect
	{
		public double Left;

		public double Top;

		public double Width;

		public double Height;

		public double Right => Left + Width;

		public double Bottom => Top + Height;

		public WindowRect(double left, double top, double width, double height)
		{
			Left = left;
			Top = top;
			Width = width;
			Height = height;
		}

		public double GetLOrR(bool right)
		{
			if (!right)
			{
				return Left;
			}
			return Right;
		}

		public void SetRight(double right)
		{
			Left = right - Width;
		}

		public void SetBottom(double bottom)
		{
			Top = bottom - Height;
		}

		public double GetSide1(bool isHorizon)
		{
			if (!isHorizon)
			{
				return Top;
			}
			return Left;
		}

		public double GetSide2(bool isHorizon)
		{
			if (!isHorizon)
			{
				return Bottom;
			}
			return Right;
		}

		public bool IsIntersect(WindowRect rect, bool isHorizon, double extra = 0.0)
		{
			if (!MathUtil.Between(GetSide1(isHorizon), rect.GetSide1(isHorizon) - extra, rect.GetSide2(isHorizon) + extra))
			{
				return MathUtil.Between(GetSide2(isHorizon), rect.GetSide1(isHorizon) - extra, rect.GetSide2(isHorizon) + extra);
			}
			return true;
		}

		public void SetSide1(bool isHorizon, double val)
		{
			if (isHorizon)
			{
				Left = val;
			}
			else
			{
				Top = val;
			}
		}

		public void SetSide2(bool isHorizon, double val)
		{
			if (isHorizon)
			{
				SetRight(val);
			}
			else
			{
				SetBottom(val);
			}
		}

		public double GetLength(bool isHorizon)
		{
			if (!isHorizon)
			{
				return Height;
			}
			return Width;
		}

		public void SetLength(bool isHorizon, double val)
		{
			if (isHorizon)
			{
				Width = val;
			}
			else
			{
				Height = val;
			}
		}

		public bool IntersectWith(WindowRect rect)
		{
			if (!(rect.Left < Right) || !(rect.Right > Left) || !(rect.Top < Bottom) || !(rect.Bottom > Top))
			{
				if (MathUtil.Between(rect.Right, Left, Right, checkEqual: false))
				{
					return MathUtil.Between(rect.Top, Top, Bottom, checkEqual: false);
				}
				return false;
			}
			return true;
		}
	}

	private class StickySortModel
	{
		public string Id;

		public WindowRect Rect;

		public double Direction;
	}

	private const double AdsorbWidth = 10.0;

	private const double UnAdsorbWidth = 10.0;

	private const double WMin = 224.0;

	private const double HMin = 212.0;

	private const double DefaultW = 254.0;

	private const double DefaultH = 228.0;

	private static ConcurrentDictionary<string, WindowRect> _windowRects = new ConcurrentDictionary<string, WindowRect>();

	private static ConcurrentDictionary<string, WindowRect> _dragWindowRects = new ConcurrentDictionary<string, WindowRect>();

	private static BlockingList<WindowRect> _screenRects = new BlockingList<WindowRect>();

	private static BlockingList<WindowRect> _dragScreenRects = new BlockingList<WindowRect>();

	private double _adsorbLeft = double.NegativeInfinity;

	private System.Windows.Point _sizeChangeStartPoint;

	private DragSizeMode _sizeMode;

	private System.Drawing.Rectangle _dragWindowBoundsDevice;

	private static bool _aligning;

	private static ConcurrentDictionary<string, TaskStickyWindow> _openedStickies = new ConcurrentDictionary<string, TaskStickyWindow>();

	public readonly TaskStickyViewModel ViewModel;

	private TaskType _taskType;

	private bool _editing;

	private DateTime _headerClickOldTime;

	private double _originHeight;

	private bool _collapsed;

	private bool _dragMouseDown;

	private bool _storyPlaying;

	private string _color;

	private static bool _locking;

	private bool _checkEmpty;

	private static SettingDialog _settingDialog;

	private System.Windows.Point _mouseDownPoint;

	private System.Windows.Point _mouseDownDevicePoint;

	private Vector _dragInnerOffsetDevice;

	private bool _isDraggingWindow;

	private List<(string, KeyBinding)> _keyBindings = new List<(string, KeyBinding)>();

	private static readonly Dictionary<StickyColorKey, string> BorderColorDict = new Dictionary<StickyColorKey, string>
	{
		{
			StickyColorKey.DarkGray,
			"#3D3F45"
		},
		{
			StickyColorKey.Dark,
			"#323232"
		},
		{
			StickyColorKey.DarkBlue,
			"#203972"
		}
	};

	private static readonly Dictionary<StickyColorKey, string> PopupBackColorDict = new Dictionary<StickyColorKey, string>
	{
		{
			StickyColorKey.DarkGray,
			"#383A3E"
		},
		{
			StickyColorKey.Dark,
			"#2F323B"
		},
		{
			StickyColorKey.DarkBlue,
			"#1E2E52"
		}
	};

	internal TaskStickyWindow Root;

	internal Border BackBorder;

	internal Grid Container;

	internal RowDefinition FirstRow;

	internal RowDefinition SecondRow;

	internal Border BottomBackground;

	internal Border TopBackground;

	internal Border TitleGrid;

	internal Path TopStatusIcon;

	internal DetailTextBox TopTitleText;

	internal StackPanel TopGrid;

	internal Border ThemeBorder;

	internal System.Windows.Controls.ToolTip ColorTooltip;

	internal Border PinGrid;

	internal System.Windows.Controls.ToolTip PinTooltip;

	internal Path PinPath;

	internal Border CloseGrid;

	internal TaskDetailStickyView DetailControl;

	internal Grid BottomGrid;

	internal Border DateBtn;

	internal StackPanel DatePanel;

	internal Path MoreIcon;

	internal EscPopup MorePopup;

	internal Grid ToastGrid;

	internal Border SizeChangeBorder;

	private bool _contentLoaded;

	private static double StickySpacing => LocalSettings.Settings.ExtraSettings.StickySpacing;

	private static bool ResetOnAlign => LocalSettings.Settings.ExtraSettings.ResetStickyWhenAlign;

	private static bool changeStateFromShortKey { get; set; }

	private static void RemoveGeoById(string id)
	{
		_windowRects.TryRemove(id, out var value);
		_dragWindowRects.TryRemove(id, out value);
	}

	private static void AddOrUpdateWindowLocation(TaskStickyModel stickyModel)
	{
		_windowRects[stickyModel.TaskId] = new WindowRect(stickyModel.Left + 8.0, stickyModel.Top + 8.0, stickyModel.Width - 16.0, stickyModel.Height - 16.0);
	}

	private void AddOrUpdateDragWindowLocation()
	{
		System.Drawing.Rectangle rectangle = ((_dragWindowBoundsDevice == System.Drawing.Rectangle.Empty) ? GetWindowBoundsDevice() : _dragWindowBoundsDevice);
		_dragWindowRects[ViewModel.Id] = new WindowRect(rectangle.Left + 8, rectangle.Top + 8, rectangle.Width - 16, rectangle.Height - 16);
	}

	private void MovePosition(System.Windows.Point target)
	{
		RemoveAnimation();
		WindowRect dragWindowRect = GetDragWindowRect();
		bool adsorb = IsAdsorb(dragWindowRect, isHorizon: true);
		bool adsorb2 = IsAdsorb(dragWindowRect, isHorizon: false);
		double diff = target.X - dragWindowRect.Left;
		double diff2 = target.Y - dragWindowRect.Top;
		double? num = GetTargetValue(adsorb, diff, dragWindowRect, isHorizon: true);
		if (num.HasValue)
		{
			SetWindowDevicePosition((int)Math.Round(num.Value), null);
		}
		dragWindowRect = GetDragWindowRect();
		double? num2 = GetTargetValue(adsorb2, diff2, dragWindowRect, isHorizon: false);
		if (num2.HasValue)
		{
			SetWindowDevicePosition(null, (int)Math.Round(num2.Value));
		}
		double? GetTargetValue(bool flag, double num3, WindowRect windowRect, bool isHorizon)
		{
			if (!flag || Math.Abs(num3) > 10.0)
			{
				windowRect.SetSide1(isHorizon, windowRect.GetSide1(isHorizon) + num3);
				foreach (KeyValuePair<string, WindowRect> dragWindowRect2 in _dragWindowRects)
				{
					WindowRect value = dragWindowRect2.Value;
					if (!(dragWindowRect2.Key == ViewModel.Id))
					{
						if (Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide1(isHorizon)) < 10.0)
						{
							windowRect.SetSide1(isHorizon, value.GetSide1(isHorizon));
							break;
						}
						if (Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide2(isHorizon)) < 10.0)
						{
							windowRect.SetSide1(isHorizon, value.GetSide2(isHorizon));
							break;
						}
						if (Math.Abs(windowRect.GetSide2(isHorizon) - value.GetSide1(isHorizon)) < 10.0)
						{
							windowRect.SetSide2(isHorizon, value.GetSide1(isHorizon));
							break;
						}
						if (Math.Abs(windowRect.GetSide2(isHorizon) - value.GetSide2(isHorizon)) < 10.0)
						{
							windowRect.SetSide2(isHorizon, value.GetSide2(isHorizon));
							break;
						}
						if (windowRect.IsIntersect(value, !isHorizon))
						{
							if (Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide2(isHorizon) - StickySpacing) < 10.0)
							{
								windowRect.SetSide1(isHorizon, value.GetSide2(isHorizon) + StickySpacing);
								break;
							}
							if (Math.Abs(value.GetSide1(isHorizon) - windowRect.GetSide2(isHorizon) - StickySpacing) < 10.0)
							{
								windowRect.SetSide2(isHorizon, value.GetSide1(isHorizon) - StickySpacing);
								break;
							}
						}
					}
				}
				return windowRect.GetSide1(isHorizon) - 8.0;
			}
			return null;
		}
	}

	private System.Drawing.Rectangle GetWindowBoundsDevice()
	{
		return NativeUtils.GetWindowRect(new WindowInteropHelper(this).EnsureHandle());
	}

	private WindowRect GetWindowRect()
	{
		return new WindowRect(base.Left + 8.0, base.Top + 8.0, base.ActualWidth - 16.0, base.ActualHeight - 16.0);
	}

	private WindowRect GetDragWindowRect()
	{
		System.Drawing.Rectangle windowBoundsDevice = GetWindowBoundsDevice();
		return new WindowRect(windowBoundsDevice.Left + 8, windowBoundsDevice.Top + 8, windowBoundsDevice.Width - 16, windowBoundsDevice.Height - 16);
	}

	private void SetWindowDevicePosition(int? left, int? top)
	{
		System.Drawing.Rectangle rectangle = ((_dragWindowBoundsDevice == System.Drawing.Rectangle.Empty) ? GetWindowBoundsDevice() : _dragWindowBoundsDevice);
		int num = left ?? rectangle.Left;
		int num2 = top ?? rectangle.Top;
		if (num != rectangle.Left || num2 != rectangle.Top)
		{
			NativeUtils.SetWindowPos(new WindowInteropHelper(this).EnsureHandle(), IntPtr.Zero, num, num2, 0, 0, 21u);
			_dragWindowBoundsDevice = new System.Drawing.Rectangle(num, num2, rectangle.Width, rectangle.Height);
			AddOrUpdateDragWindowLocation();
		}
	}

	private bool IsAdsorb(WindowRect windowRect, bool isHorizon)
	{
		foreach (KeyValuePair<string, WindowRect> dragWindowRect in _dragWindowRects)
		{
			if (!(dragWindowRect.Key == ViewModel.Id))
			{
				WindowRect value = dragWindowRect.Value;
				if (Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide1(isHorizon)) < 1.0 || Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide2(isHorizon)) < 1.0 || Math.Abs(windowRect.GetSide2(isHorizon) - value.GetSide1(isHorizon)) < 1.0 || Math.Abs(windowRect.GetSide2(isHorizon) - value.GetSide2(isHorizon)) < 1.0)
				{
					return true;
				}
				if ((Math.Abs(windowRect.GetSide1(isHorizon) - value.GetSide2(isHorizon) - StickySpacing) < 1.0 || Math.Abs(value.GetSide1(isHorizon) - windowRect.GetSide2(isHorizon) - StickySpacing) < 1.0) && windowRect.IsIntersect(value, !isHorizon))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnBorderMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Released)
		{
			_sizeChangeStartPoint = default(System.Windows.Point);
			System.Windows.Point position = e.GetPosition(SizeChangeBorder);
			GetPointMode(position);
		}
	}

	private void OnSizeChangeMouseDown(object sender, MouseButtonEventArgs e)
	{
		_dragWindowBoundsDevice = System.Drawing.Rectangle.Empty;
		_sizeChangeStartPoint = e.GetPosition(SizeChangeBorder);
		_sizeMode = GetPointMode(_sizeChangeStartPoint);
		if ((_sizeMode & DragSizeMode.Right) == DragSizeMode.Right)
		{
			_sizeChangeStartPoint.X -= base.ActualWidth;
		}
		if ((_sizeMode & DragSizeMode.Bottom) == DragSizeMode.Bottom)
		{
			_sizeChangeStartPoint.Y -= base.ActualHeight;
		}
		SizeChangeBorder.CaptureMouse();
		SizeChangeBorder.MouseMove += ChangeSize;
		SizeChangeBorder.MouseLeftButtonUp += ReleaseSizeChange;
	}

	private void ReleaseSizeChange(object sender, MouseButtonEventArgs e)
	{
		_dragWindowBoundsDevice = System.Drawing.Rectangle.Empty;
		SizeChangeBorder.MouseMove -= ChangeSize;
		SizeChangeBorder.MouseLeftButtonUp -= ReleaseSizeChange;
		SizeChangeBorder.ReleaseMouseCapture();
	}

	private void ChangeSize(object sender, System.Windows.Input.MouseEventArgs e)
	{
		System.Windows.Point position = e.GetPosition(SizeChangeBorder);
		bool flag = (_sizeMode & DragSizeMode.Left) == DragSizeMode.Left;
		bool flag2 = (_sizeMode & DragSizeMode.Right) == DragSizeMode.Right;
		bool flag3 = (_sizeMode & DragSizeMode.Top) == DragSizeMode.Top;
		bool flag4 = (_sizeMode & DragSizeMode.Bottom) == DragSizeMode.Bottom;
		if (flag2)
		{
			position.X -= base.ActualWidth;
		}
		if (flag4)
		{
			position.Y -= base.ActualHeight;
		}
		RemoveAnimation();
		if (flag || flag2)
		{
			double num = position.X - _sizeChangeStartPoint.X;
			double num2 = base.Left + base.ActualWidth;
			if (base.ActualWidth + (double)((!flag) ? 1 : (-1)) * num <= base.MinWidth)
			{
				if (base.Width > base.MinWidth)
				{
					base.Left = (flag ? (num2 - base.MinWidth) : base.Left);
					base.Width = base.MinWidth;
				}
			}
			else
			{
				WindowRect windowRect = GetTargetValue(GetWindowRect(), isHorizon: true, flag, num);
				base.Left = windowRect.Left - 8.0;
				base.Width = windowRect.Width + 16.0;
			}
		}
		if (!(flag3 || flag4))
		{
			return;
		}
		double num3 = position.Y - _sizeChangeStartPoint.Y;
		double num4 = base.Top + base.ActualHeight;
		if (base.ActualHeight + (double)((!flag3) ? 1 : (-1)) * num3 <= base.MinHeight)
		{
			if (base.Height > base.MinHeight)
			{
				base.Top = (flag3 ? (num4 - base.MinHeight) : base.Top);
				base.Height = base.MinHeight;
			}
		}
		else
		{
			WindowRect windowRect2 = GetTargetValue(GetWindowRect(), isHorizon: false, flag3, num3);
			base.Top = windowRect2.Top - 8.0;
			base.Height = windowRect2.Height + 16.0;
		}
		WindowRect GetTargetValue(WindowRect windowRect3, bool isHorizon, bool sideDrag, double diff)
		{
			if (!IsAdsorb(windowRect3, isHorizon) || Math.Abs(diff) > 10.0)
			{
				if (sideDrag)
				{
					windowRect3.SetSide1(isHorizon, windowRect3.GetSide1(isHorizon) + diff);
					windowRect3.SetLength(isHorizon, windowRect3.GetLength(isHorizon) - diff);
				}
				else
				{
					windowRect3.SetLength(isHorizon, windowRect3.GetLength(isHorizon) + diff);
				}
				double num5 = windowRect3.GetSide1(isHorizon);
				double num6 = windowRect3.GetSide2(isHorizon);
				foreach (KeyValuePair<string, WindowRect> stickyPair in _windowRects)
				{
					WindowRect value = stickyPair.Value;
					if (!(stickyPair.Key == ViewModel.Id))
					{
						if (sideDrag && Math.Abs(num5 - value.GetSide1(isHorizon)) < 10.0)
						{
							num5 = value.GetSide1(isHorizon);
							break;
						}
						if (sideDrag && Math.Abs(num5 - value.GetSide2(isHorizon)) < 10.0)
						{
							num5 = value.GetSide2(isHorizon);
						}
						if (!sideDrag && Math.Abs(num6 - value.GetSide1(isHorizon)) < 10.0)
						{
							num6 = value.GetSide1(isHorizon);
							break;
						}
						if (!sideDrag && Math.Abs(num6 - value.GetSide2(isHorizon)) < 10.0)
						{
							num6 = value.GetSide2(isHorizon);
							break;
						}
						if (windowRect3.IsIntersect(value, isHorizon))
						{
							if (sideDrag && Math.Abs(num5 - value.GetSide2(isHorizon) - StickySpacing) < 10.0)
							{
								num5 = value.GetSide2(isHorizon) + StickySpacing;
								break;
							}
							if (!sideDrag && Math.Abs(value.GetSide1(isHorizon) - num6 - StickySpacing) < 10.0)
							{
								num6 = value.GetSide1(isHorizon) - StickySpacing;
								break;
							}
						}
					}
				}
				windowRect3.SetSide1(isHorizon, num5);
				windowRect3.SetLength(isHorizon, num6 - num5);
			}
			return windowRect3;
		}
	}

	private DragSizeMode GetPointMode(System.Windows.Point point)
	{
		bool flag = point.Y <= 8.0;
		bool flag2 = point.X <= 8.0;
		bool flag3 = point.Y >= SizeChangeBorder.ActualHeight - 8.0;
		bool flag4 = point.X >= SizeChangeBorder.ActualWidth - 8.0;
		if ((flag && flag2) || (flag3 && flag4))
		{
			SizeChangeBorder.Cursor = System.Windows.Input.Cursors.SizeNWSE;
			if (!flag)
			{
				return DragSizeMode.Bottom | DragSizeMode.Right;
			}
			return DragSizeMode.Top | DragSizeMode.Left;
		}
		if ((flag && flag4) || (flag3 && flag2))
		{
			SizeChangeBorder.Cursor = System.Windows.Input.Cursors.SizeNESW;
			if (!flag)
			{
				return DragSizeMode.Bottom | DragSizeMode.Left;
			}
			return DragSizeMode.Top | DragSizeMode.Right;
		}
		if (flag || flag3)
		{
			SizeChangeBorder.Cursor = System.Windows.Input.Cursors.SizeNS;
			if (!flag)
			{
				return DragSizeMode.Bottom;
			}
			return DragSizeMode.Top;
		}
		if (flag4 || flag2)
		{
			SizeChangeBorder.Cursor = System.Windows.Input.Cursors.SizeWE;
			if (!flag2)
			{
				return DragSizeMode.Right;
			}
			return DragSizeMode.Left;
		}
		return DragSizeMode.None;
	}

	public void Align(bool isVertical, bool isRight)
	{
		UserActCollectUtils.AddClickEvent("sticky_note", "arrange_by", (!isVertical) ? "top" : (isRight ? "right" : "left"));
		System.Drawing.Rectangle monitorFromWindow = WindowSizing.GetMonitorFromWindow(this);
		if (!(monitorFromWindow == default(System.Drawing.Rectangle)))
		{
			double ratio = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.M22 ?? (1.0 / ScreenPositionUtils.GetScalingRatio());
			AlignWindows(isVertical, isRight, monitorFromWindow, ratio);
			Activate();
		}
	}

	private static void AlignWindows(bool isVertical, bool isRight, System.Drawing.Rectangle screen, double ratio)
	{
		if (_aligning)
		{
			return;
		}
		_aligning = true;
		List<StickySortModel> list = _windowRects.Select((KeyValuePair<string, WindowRect> kv) => new StickySortModel
		{
			Id = kv.Key,
			Rect = new WindowRect(kv.Value.Left, kv.Value.Top, ResetOnAlign ? 238.0 : kv.Value.Width, (ResetOnAlign && kv.Value.Height > 100.0) ? 212.0 : kv.Value.Height)
		}).ToList();
		List<StickySortModel> list2 = new List<StickySortModel>();
		foreach (StickySortModel item in list)
		{
			if (_openedStickies.ContainsKey(item.Id) && WindowSizing.GetMonitorFromWindow(_openedStickies[item.Id]) == screen)
			{
				list2.Add(item);
			}
		}
		screen = new System.Drawing.Rectangle((int)((double)screen.Left * ratio), (int)((double)screen.Top * ratio), (int)((double)screen.Width * ratio), (int)((double)screen.Height * ratio));
		SortInScreen(isRight || isVertical, isRight, screen, list2);
		_aligning = false;
	}

	private static void SortInScreen(bool isVertical, bool isRight, System.Drawing.Rectangle screenRect, List<StickySortModel> stickySortModels)
	{
		double num = (isVertical ? 224.0 : 212.0) + StickySpacing;
		int num2 = (isVertical ? screenRect.Height : screenRect.Width);
		int screenSide = (isVertical ? screenRect.Top : screenRect.Left);
		int screenOtherSide = (isVertical ? screenRect.Bottom : screenRect.Right);
		double num3 = (isVertical ? 228.0 : 254.0);
		double num4 = Math.Max(20.0, StickySpacing);
		double sideMargin = (isVertical ? num4 : (num4 + ((double)num2 - 2.0 * num4) % (num3 - 16.0 + StickySpacing) / 2.0));
		int num5 = ((!isRight) ? 1 : (-1));
		System.Windows.Point point = new System.Windows.Point((double)(isRight ? screenRect.Right : screenRect.Left) + (double)num5 * sideMargin, (double)screenRect.Top + num4);
		int count = stickySortModels.Count;
		List<StickySortModel> sorted = new List<StickySortModel>();
		bool flag = false;
		while (sorted.Count < count)
		{
			if (!flag)
			{
				double maxLength = (double)screenOtherSide - sideMargin - (isVertical ? point.Y : point.X) + 20.0;
				if (maxLength < 60.0)
				{
					flag = true;
					continue;
				}
				foreach (StickySortModel stickySortModel6 in stickySortModels)
				{
					stickySortModel6.Direction = Math.Sqrt(Math.Pow(stickySortModel6.Rect.GetLOrR(isRight) - point.X, 2.0) + Math.Pow(stickySortModel6.Rect.Top - point.Y, 2.0));
				}
				stickySortModels.Sort((StickySortModel a, StickySortModel b) => a.Direction.CompareTo(b.Direction));
				StickySortModel stickySortModel = stickySortModels.FirstOrDefault((StickySortModel m) => (isVertical ? m.Rect.Height : m.Rect.Width) <= maxLength);
				if (stickySortModel != null)
				{
					stickySortModels.Remove(stickySortModel);
					if (isRight)
					{
						stickySortModel.Rect.SetRight(point.X);
					}
					else
					{
						stickySortModel.Rect.Left = point.X;
					}
					stickySortModel.Rect.Top = point.Y;
					if (isVertical)
					{
						point.Y = point.Y + stickySortModel.Rect.Height + StickySpacing;
					}
					else
					{
						point.X = point.X + stickySortModel.Rect.Width + StickySpacing;
					}
					sorted.Add(stickySortModel);
					continue;
				}
				flag = true;
			}
			double num6 = ((!isVertical) ? screenRect.Top : (isRight ? screenRect.Right : screenRect.Left));
			sorted.Sort((StickySortModel a, StickySortModel b) => a.Rect.GetSide1(!isVertical).CompareTo(b.Rect.GetSide1(!isVertical)));
			bool flag2 = false;
			int num7 = ((!isVertical) ? screenRect.Bottom : (isRight ? screenRect.Left : screenRect.Right));
			do
			{
				StickySortModel stickySortModel2 = GetMinBottomModel(num6, num7);
				if (stickySortModel2 == null)
				{
					break;
				}
				num6 = (isRight ? stickySortModel2.Rect.Left : stickySortModel2.Rect.GetSide2(isVertical));
				if (Math.Abs((double)num7 - num6) < num)
				{
					break;
				}
				foreach (var item in GetIntersectLines(num6))
				{
					foreach (StickySortModel stickySortModel7 in stickySortModels)
					{
						double num8;
						if (!isVertical)
						{
							(num8, _) = item;
						}
						else
						{
							num8 = num6;
						}
						double num9 = num8;
						double num10;
						if (!isVertical)
						{
							num10 = num6;
						}
						else
						{
							(num10, _) = item;
						}
						double num11 = num10;
						stickySortModel7.Direction = Math.Sqrt(Math.Pow(stickySortModel7.Rect.GetLOrR(isRight) - num9, 2.0) + Math.Pow(stickySortModel7.Rect.Top - num11, 2.0));
					}
					stickySortModels.Sort((StickySortModel a, StickySortModel b) => a.Direction.CompareTo(b.Direction));
					foreach (StickySortModel stickySortModel8 in stickySortModels)
					{
						double num12 = item.Item1;
						double num13 = item.Item2 - num12;
						while (stickySortModel8.Rect.GetLength(!isVertical) <= num13 - StickySpacing + 2.0)
						{
							double left = (isVertical ? (stickySortModel2.Rect.Right + StickySpacing) : num12);
							double top = (isVertical ? num12 : (stickySortModel2.Rect.Bottom + StickySpacing));
							WindowRect rect = new WindowRect(left, top, stickySortModel8.Rect.Width, stickySortModel8.Rect.Height);
							if (isRight)
							{
								rect.SetRight(stickySortModel2.Rect.Left - StickySpacing);
							}
							StickySortModel stickySortModel3 = sorted.FirstOrDefault((StickySortModel s) => rect.IntersectWith(s.Rect));
							if (stickySortModel3 != null)
							{
								num12 = stickySortModel3.Rect.GetSide2(!isVertical) + StickySpacing;
								num13 = item.Item2 - num12;
								continue;
							}
							stickySortModels.Remove(stickySortModel8);
							stickySortModel8.Rect.Left = rect.Left;
							stickySortModel8.Rect.Top = rect.Top;
							sorted.Add(stickySortModel8);
							flag2 = true;
							break;
						}
						if (flag2)
						{
							break;
						}
					}
					if (flag2)
					{
						break;
					}
				}
			}
			while (!flag2);
			if (flag2)
			{
				continue;
			}
			double num14 = 10000.0;
			StickySortModel stickySortModel4 = sorted.FirstOrDefault();
			System.Windows.Point point2 = (isRight ? new System.Windows.Point(screenRect.Left, screenRect.Bottom) : new System.Windows.Point(screenRect.Right, screenRect.Bottom));
			foreach (StickySortModel item2 in sorted)
			{
				double num15 = Math.Sqrt(Math.Pow(item2.Rect.GetLOrR(!isRight) - point2.X, 2.0) + Math.Pow(item2.Rect.Top - point2.Y, 2.0));
				if (num15 < num14)
				{
					stickySortModel4 = item2;
					num14 = num15;
				}
			}
			double num16 = stickySortModel4?.Rect.Top ?? ((double)screenRect.Top + num4);
			double left2 = stickySortModel4?.Rect.Left ?? ((double)screenRect.Left + num4);
			double right = stickySortModel4?.Rect.Right ?? ((double)screenRect.Right + num4);
			foreach (StickySortModel stickySortModel9 in stickySortModels)
			{
				num16 += 12.0;
				if (num16 > (double)screenRect.Bottom - 212.0)
				{
					num16 = (double)screenRect.Bottom - 212.0;
				}
				stickySortModel9.Rect.Top = num16;
				if (isRight)
				{
					stickySortModel9.Rect.SetRight(right);
				}
				else
				{
					stickySortModel9.Rect.Left = left2;
				}
				sorted.Add(stickySortModel9);
			}
		}
		foreach (StickySortModel item3 in sorted)
		{
			if (_openedStickies.TryGetValue(item3.Id, out var value))
			{
				value.Activate();
			}
		}
		foreach (StickySortModel item4 in sorted)
		{
			if (_openedStickies.TryGetValue(item4.Id, out var value2))
			{
				value2.AnimSetPosition(item4.Rect.Left - 8.0, item4.Rect.Top - 8.0);
			}
		}
		List<(double, double)> GetIntersectLines(double offset)
		{
			List<WindowRect> list = new List<WindowRect>();
			foreach (StickySortModel item5 in sorted)
			{
				if (isRight)
				{
					if (MathUtil.Between(offset, item5.Rect.Right + StickySpacing, item5.Rect.Left + 4.0))
					{
						list.Add(item5.Rect);
					}
				}
				else if (MathUtil.Between(offset, item5.Rect.GetSide1(isVertical) - StickySpacing, item5.Rect.GetSide2(isVertical) - 4.0))
				{
					list.Add(item5.Rect);
				}
			}
			list.Sort((WindowRect a, WindowRect b) => a.GetSide1(!isVertical).CompareTo(b.GetSide1(!isVertical)));
			List<(double, double)> list2 = new List<(double, double)>();
			double num17 = (double)screenSide + sideMargin;
			foreach (WindowRect item6 in list)
			{
				if (item6.GetSide1(!isVertical) - num17 >= 60.0)
				{
					list2.Add((num17, item6.GetSide1(!isVertical)));
				}
				num17 = item6.GetSide2(!isVertical) + StickySpacing;
			}
			if ((double)screenOtherSide - num17 >= 60.0)
			{
				list2.Add((num17, screenOtherSide));
			}
			return list2;
		}
		StickySortModel GetMinBottomModel(double min, double max)
		{
			StickySortModel result = null;
			if (isRight)
			{
				min *= -1.0;
				max *= -1.0;
			}
			for (int num17 = sorted.Count - 1; num17 >= 0; num17--)
			{
				StickySortModel stickySortModel5 = sorted[num17];
				double num18 = (isRight ? (-1.0 * stickySortModel5.Rect.Left) : stickySortModel5.Rect.GetSide2(isVertical));
				if (num18 > min && num18 < max)
				{
					result = stickySortModel5;
					max = num18;
				}
			}
			return result;
		}
	}

	public TaskStickyWindow(TaskStickyModel stickyModel)
	{
		InitializeComponent();
		base.SourceInitialized += OnWindowSourceInitialized;
		ViewModel = new TaskStickyViewModel(stickyModel.TaskId);
		InitWindow();
		SetStickyColor(stickyModel.Color, collect: false);
		base.DataContext = ViewModel;
		ViewModel.Window = this;
		InitWindowSetting(stickyModel);
		UtilLog.Info("ShowSticky: " + stickyModel.TaskId);
		base.Loaded += async delegate
		{
			SetOpacity();
			base.LocationChanged += OnLocationChanged;
			base.SizeChanged += OnSizeChanged;
			ResetPosition();
			DetailControl.RefreshEditorVisualLayersAfterLayout();
			Activate();
			if (ViewModel.IsEmptyTask())
			{
				await Task.Delay(50);
				DetailControl.TryFocusTitle();
			}
			UpdateLocationCache();
		};
		BindEvent();
		InitShortCut();
		SetDatePanelOpacity();
		base.ShowInTaskbar = !LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar;
		base.StateChanged += OnStateChanged;
	}

	private void InitWindow()
	{
		DetailControl.SetTitleAndContent(ViewModel.SourceModel);
		DetailControl.SetStickyMode();
		DetailControl.Navigate(ViewModel.Id);
		TopTitleText.SetText(ViewModel.Title);
		TopTitleText.SetBaseColor("StickyTitleTextColor");
		UpdateTopStatusIcon();
		ColorTooltip.Content = Utils.GetString("Color") + (string.IsNullOrEmpty(LocalSettings.Settings.ShortCutModel.StickyColor) ? "" : ("  " + LocalSettings.Settings.ShortCutModel.StickyColor));
	}

	private void SetStickyColor(string color, bool collect = true)
	{
		if (!Enum.TryParse<StickyColorKey>(color, ignoreCase: true, out var result))
		{
			color = StickyColorKey.Default.ToString();
		}
		_color = color;
		bool flag = color.ToLower().StartsWith("dark");
		ThemeUtil.SetTheme(flag ? "dark" : "light", this);
		SetStickyColor(result);
		DetailControl.SetTheme(flag);
		DetailControl.RefreshEditorVisualLayersAfterLayout();
		TopTitleText.SetLightTheme();
		if (collect)
		{
			CollectColorSelect(result);
		}
	}

	private void CollectColorSelect(StickyColorKey colorKey)
	{
		string key;
		switch (colorKey)
		{
		case StickyColorKey.Apricot:
			key = "pink";
			break;
		case StickyColorKey.Watermelon:
			key = "watermelon_red";
			break;
		case StickyColorKey.LakeBlue:
			key = "light_blue";
			break;
		case StickyColorKey.Blue:
			key = "blue";
			break;
		case StickyColorKey.Purple:
			key = "purple";
			break;
		case StickyColorKey.BlueGreen:
			key = "green";
			break;
		case StickyColorKey.Gray:
			key = "grey";
			break;
		default:
			key = "yellow";
			break;
		}
		DelayActionHandlerCenter.TryDoAction("StickyColor" + ViewModel.Id, delegate
		{
			UserActCollectUtils.AddClickEvent("sticky_note", "color", key);
		}, 60000);
	}

	private void OnWindowSourceInitialized(object sender, EventArgs e)
	{
	}

	private async void ResetPosition()
	{
		double left = base.Left;
		double top = base.Top;
		WindowHelper.MoveTo(this, (int)base.Left, (int)base.Top);
		base.Left = left;
		base.Top = top;
		Matrix? transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
		await Task.Delay(1000);
		System.Windows.Point point = WidgetLocationHelper.GetPomoLocationSafely(defaultPoint: new System.Windows.Point(SystemParameters.PrimaryScreenWidth / 2.0, SystemParameters.PrimaryScreenHeight / 2.0), left: base.Left, top: base.Top, width: base.Width, height: base.Height, transform: transform);
		base.Left = point.X;
		base.Top = point.Y;
		UtilLog.Info($"Sticky Location : {base.Left},{base.Top},{ViewModel?.Id}");
	}

	private void InitWindowSetting(TaskStickyModel stickyModel)
	{
		base.Width = stickyModel.Width;
		base.Left = stickyModel.Left;
		base.Top = stickyModel.Top;
		_originHeight = stickyModel.Height;
		if (stickyModel.Expand)
		{
			base.Height = stickyModel.Height;
			TopBackground.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "StickyTopColor");
		}
		else
		{
			_collapsed = true;
			SetResourceReference(FrameworkElement.MinHeightProperty, "StickyHeight42");
			SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight42");
			SetResourceReference(FrameworkElement.MaxHeightProperty, "StickyHeight42");
			FirstRow.Height = new GridLength(22.0);
			SecondRow.Height = new GridLength(0.0);
			BottomGrid.Visibility = Visibility.Collapsed;
			SetTopTooltip();
			Container.VerticalAlignment = VerticalAlignment.Center;
			TitleGrid.Visibility = Visibility.Visible;
		}
		SetTopMost(stickyModel.TopMost);
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (!_isDraggingWindow)
		{
			SaveLocation();
		}
	}

	private async void SaveLocation()
	{
		string id = ViewModel.Id;
		DelayActionHandlerCenter.TryDoAction("StickySizeChange" + id, async delegate
		{
			UpdateLocationCache();
		});
	}

	private void OnLocationChanged(object sender, EventArgs e)
	{
		if (!_isDraggingWindow)
		{
			SaveLocation();
		}
	}

	private void BindEvent()
	{
		BindPropertyChangeEvent();
		DetailControl.NavigateTask += OnNavigateTask;
		KeyBindingManager.ShortCutChanged += OnShortCutChanged;
		GlobalEventManager.StickyOpacityChanged += OnOpacityChanged;
		DataChangedNotifier.ThemeModeChanged += OnThemeChanged;
	}

	private void OnThemeChanged(object sender, EventArgs e)
	{
		SetStickyColor(_color, collect: false);
	}

	private async void OnNavigateTask(object sender, ProjectTask e)
	{
		if (!(await TaskDetailWindows.ShowTaskWindows(e.TaskId)))
		{
			TryToast(Utils.GetString("NoTaskFound"));
		}
	}

	private void OnShortCutChanged(object sender, string e)
	{
		if (e == "StickyColor")
		{
			ColorTooltip.Content = Utils.GetString("Color") + (string.IsNullOrEmpty(LocalSettings.Settings.ShortCutModel.StickyColor) ? "" : ("  " + LocalSettings.Settings.ShortCutModel.StickyColor));
		}
		if (e == "PinSticky")
		{
			PinTooltip.Content = Utils.GetString(base.Topmost ? "Unpin" : "Pin") + (string.IsNullOrEmpty(LocalSettings.Settings.ShortCutModel.PinSticky) ? "" : ("  " + LocalSettings.Settings.ShortCutModel.PinSticky));
		}
	}

	private void UnbindEvent()
	{
		DetailControl.NavigateTask -= OnNavigateTask;
		GlobalEventManager.StickyOpacityChanged -= OnOpacityChanged;
		KeyBindingManager.ShortCutChanged -= OnShortCutChanged;
		DataChangedNotifier.ThemeModeChanged -= OnThemeChanged;
	}

	private void OnToastString(object sender, string e)
	{
		TryToastString(this, e);
	}

	private void BindPropertyChangeEvent()
	{
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			if (!ViewModel.Enable)
			{
				TaskStickyDao.CloseAsync(ViewModel.Id);
				Close();
			}
		}, "Enable");
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			if (ViewModel.SourceModel.Deleted != 0)
			{
				TaskStickyDao.CloseAsync(ViewModel.Id);
				Close();
			}
		}, "Deleted");
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			if (!string.IsNullOrEmpty(ViewModel.ToastString))
			{
				TryToast(ViewModel.ToastString);
			}
		}, "ToastString");
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			SetDatePanelOpacity();
			SetTopTooltip();
		}, "DateText");
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			TopTitleText.SetText(ViewModel.Title);
		}, "Title");
		PropertyChangedEventManager.AddHandler(ViewModel, delegate
		{
			UpdateTopStatusIcon();
		}, "Status");
	}

	private void SetDatePanelOpacity()
	{
		if (Utils.IsEmptyDate(ViewModel.SourceModel.StartDate))
		{
			DatePanel.SetBinding(UIElement.OpacityProperty, new System.Windows.Data.Binding
			{
				ElementName = "MoreIcon",
				Path = new PropertyPath(UIElement.OpacityProperty)
			});
		}
		else
		{
			DatePanel.Opacity = 1.0;
		}
	}

	private void SetTopTooltip()
	{
		if (Utils.IsEmptyDate(ViewModel.SourceModel.StartDate) || !_collapsed)
		{
			TopBackground.ToolTip = null;
		}
		else
		{
			TopBackground.SetBinding(FrameworkElement.ToolTipProperty, "DateText");
		}
	}

	private void UpdateTopStatusIcon()
	{
		int status = ViewModel.Status;
		TopTitleText.TextStatus = status;
		if (status == 0)
		{
			TopStatusIcon.Visibility = Visibility.Collapsed;
			return;
		}
		TopStatusIcon.BeginAnimation(FrameworkElement.WidthProperty, null);
		TopStatusIcon.Width = 14.0;
		TopStatusIcon.Visibility = Visibility.Visible;
		if (status == -1)
		{
			TopStatusIcon.Data = Utils.GetGeometry("Ic.TaskType.Abandon");
		}
		else
		{
			TopStatusIcon.Data = Utils.GetGeometry("Ic.TaskType.Completed");
		}
	}

	private async void OnTopStatusIconClick(object sender, MouseButtonEventArgs e)
	{
		if (ViewModel.Status != 0)
		{
			e.Handled = true;
			DoubleAnimation animation = new DoubleAnimation(TopStatusIcon.ActualWidth, 0.0, TimeSpan.FromMilliseconds(200.0))
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			TopStatusIcon.BeginAnimation(FrameworkElement.WidthProperty, animation);
			await Task.Delay(200);
			await TaskService.SetTaskStatus(ViewModel.Id, 0);
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (_checkEmpty && ViewModel.IsEmptyTask())
		{
			TaskService.DeleteTask(ViewModel.Id, 2);
		}
		RemoveGeoById(ViewModel.SourceModel.Id);
		_openedStickies.TryRemove(ViewModel.SourceModel.Id, out var _);
		RemoveKeyBinding();
		UnbindEvent();
		if (!WindowManager.AppLockOrExit)
		{
			UtilLog.Info("ManualCloseSticky: " + ViewModel.Id);
			TaskStickyDao.CloseAsync(ViewModel.Id);
		}
		base.OnClosing(e);
	}

	public static async Task<TaskStickyModel> GetTaskStickyModel(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return null;
		}
		if (!_openedStickies.TryGetValue(taskId, out var value))
		{
			TaskStickyModel sticky = await TaskStickyDao.GetModelByTaskIdAsync(taskId, LocalSettings.Settings.LoginUserId);
			if (sticky != null)
			{
				if (sticky.Closed)
				{
					sticky.Closed = false;
					if (sticky.LastCloseDate != 0L && ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Now.AddDays(-7.0)) >= sticky.LastCloseDate)
					{
						sticky.ResetValue();
						sticky.TopMost = LocalSettings.Settings.ExtraSettings.StickyDefaultPin;
					}
					await BaseDao<TaskStickyModel>.UpdateAsync(sticky);
				}
			}
			else
			{
				if (!TaskCache.ExistTask(taskId))
				{
					return null;
				}
				sticky = new TaskStickyModel(taskId, LocalSettings.Settings.LoginUserId)
				{
					Color = GetDefaultColor(),
					TopMost = LocalSettings.Settings.ExtraSettings.StickyDefaultPin
				};
				await TaskStickyDao.AddStickyAsync(sticky);
			}
			return sticky;
		}
		try
		{
			value.WindowState = WindowState.Normal;
			value.Activate();
		}
		catch
		{
		}
		return null;
	}

	private static string GetDefaultColor()
	{
		string text = LocalSettings.Settings.ExtraSettings.StickyDefaultColor;
		if (text == StickyColorKey.Random.ToString())
		{
			text = ((StickyColorKey)new Random().Next(0, Enum.GetValues(typeof(StickyColorKey)).Cast<StickyColorKey>().Count() - 2)/*cast due to .constrained prefix*/).ToString();
		}
		return text;
	}

	private void OnPinClick(object sender, MouseButtonEventArgs e)
	{
		Pin();
		UserActCollectUtils.AddClickEvent("sticky_note", "sticky_note_ui", base.Topmost ? "pin" : "unpin");
	}

	private void SetTopMost(bool topMost)
	{
		base.Topmost = topMost;
		PinPath.RenderTransform = new RotateTransform((!topMost) ? 45 : 0);
		PinTooltip.Content = Utils.GetString(topMost ? "Unpin" : "Pin") + (string.IsNullOrEmpty(LocalSettings.Settings.ShortCutModel.PinSticky) ? "" : ("  " + LocalSettings.Settings.ShortCutModel.PinSticky));
	}

	private void OnCloseClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("sticky_note", "sticky_note_ui", "close");
		Close();
	}

	public void TryClose()
	{
		Close();
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		TopBackground.ReleaseMouseCapture();
		_mouseDownPoint = default(System.Windows.Point);
		_mouseDownDevicePoint = default(System.Windows.Point);
		_dragInnerOffsetDevice = default(Vector);
		bool isDraggingWindow = _isDraggingWindow;
		_isDraggingWindow = false;
		_dragWindowBoundsDevice = System.Drawing.Rectangle.Empty;
		foreach (string item in _dragWindowRects.Keys.Where((string k) => k.StartsWith("screen")))
		{
			_dragWindowRects.TryRemove(item, out var _);
		}
		_dragScreenRects.Clear();
		if (isDraggingWindow)
		{
			UpdateLocationCache();
		}
		if ((DateTime.Now - _headerClickOldTime).TotalMilliseconds <= 300.0 && !_storyPlaying)
		{
			CollapseWindowWithStory();
			_headerClickOldTime = DateTime.Now.AddSeconds(-1.0);
		}
		else
		{
			_headerClickOldTime = DateTime.Now;
		}
	}

	private void TryDragWindow(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (_mouseDownDevicePoint != default(System.Windows.Point) && Mouse.LeftButton == MouseButtonState.Pressed)
		{
			_isDraggingWindow = true;
			if (_dragScreenRects.Count == 0 && !Utils.IsWindows7())
			{
				Screen[] allScreens = Screen.AllScreens;
				foreach (Screen screen in allScreens)
				{
					int left = screen.WorkingArea.Left;
					int top = screen.WorkingArea.Top;
					int width = screen.WorkingArea.Width;
					int height = screen.WorkingArea.Height;
					int num = screen.WorkingArea.Left + screen.WorkingArea.Width;
					if (screen.WorkingArea.Height == screen.Bounds.Height)
					{
						int bottom = screen.WorkingArea.Bottom;
						_dragScreenRects.Add(new WindowRect(left, bottom, width, 1.0));
					}
					_dragScreenRects.Add(new WindowRect(left, top, 1.0, height));
					_dragScreenRects.Add(new WindowRect(left, top, width, 1.0));
					_dragScreenRects.Add(new WindowRect(num, top, 1.0, height));
				}
				List<WindowRect> value = _dragScreenRects.Value;
				for (int j = 0; j < value.Count; j++)
				{
					WindowRect value2 = value[j];
					_dragWindowRects.TryAdd("screen" + j, value2);
				}
			}
			System.Windows.Point point = new System.Windows.Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y);
			System.Windows.Point target = new System.Windows.Point(point.X - _dragInnerOffsetDevice.X, point.Y - _dragInnerOffsetDevice.Y);
			MovePosition(target);
		}
		else
		{
			_mouseDownPoint = default(System.Windows.Point);
			_mouseDownDevicePoint = default(System.Windows.Point);
			_dragInnerOffsetDevice = default(Vector);
		}
	}

	private void TopGridMouseDown(object sender, MouseButtonEventArgs e)
	{
		TopBackground.CaptureMouse();
		_mouseDownPoint = e.GetPosition(this);
		_mouseDownDevicePoint = new System.Windows.Point(System.Windows.Forms.Control.MousePosition.X, System.Windows.Forms.Control.MousePosition.Y);
		System.Drawing.Rectangle windowRect = NativeUtils.GetWindowRect(new WindowInteropHelper(this).EnsureHandle());
		_dragInnerOffsetDevice = new Vector(_mouseDownDevicePoint.X - (double)(windowRect.Left + 8), _mouseDownDevicePoint.Y - (double)(windowRect.Top + 8));
		_dragWindowBoundsDevice = windowRect;
		_isDraggingWindow = false;
	}

	private void OnMoreClick(object sender, MouseButtonEventArgs e)
	{
		SetEditing(editing: true);
		UserActCollectUtils.AddClickEvent("sticky_note", "sticky_note_ui", "om");
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"AddTask", Utils.GetString("NewAdd"), (Geometry)null, "")
			{
				RightText = LocalSettings.Settings.ShortCutModel.AddTask,
				FontSize = 12,
				TextLineHeight = 16
			},
			new CustomMenuItemViewModel((object)"Alignment", Utils.GetString("Alignment"), (Geometry)null, "")
			{
				FontSize = 12,
				TextLineHeight = 16,
				SubActions = new List<CustomMenuItemViewModel>
				{
					new CustomMenuItemViewModel((object)"TopAlignment", Utils.GetString("ToTop"), (Geometry)null, "")
					{
						RightText = HotkeyModel.HandleUpDownText(LocalSettings.Settings.ShortCutModel.StickyAlignTop),
						FontSize = 12,
						TextLineHeight = 16
					},
					new CustomMenuItemViewModel((object)"LeftAlignment", Utils.GetString("ToLeft"), (Geometry)null, "")
					{
						RightText = HotkeyModel.HandleUpDownText(LocalSettings.Settings.ShortCutModel.StickyAlignLeft),
						FontSize = 12,
						TextLineHeight = 16
					},
					new CustomMenuItemViewModel((object)"RightAlignment", Utils.GetString("ToRight"), (Geometry)null, "")
					{
						RightText = HotkeyModel.HandleUpDownText(LocalSettings.Settings.ShortCutModel.StickyAlignRight),
						FontSize = 12,
						TextLineHeight = 16
					}
				},
				ShowSelected = false
			},
			new CustomMenuItemViewModel((object)"OpenMainWindow", Utils.GetString("OpenMainWindow"), (Geometry)null, "")
			{
				FontSize = 12,
				TextLineHeight = 16
			},
			new CustomMenuItemViewModel((object)"Settings", Utils.GetString("Settings"), (Geometry)null, "")
			{
				FontSize = 12,
				TextLineHeight = 16
			},
			new CustomMenuItemViewModel(null),
			new CustomMenuItemViewModel((object)"Delete", Utils.GetString("Delete"), (Geometry)null, "")
			{
				FontSize = 12,
				IsDelete = true,
				TextLineHeight = 16
			}
		};
		CustomMenuList customMenuList = new CustomMenuList(list, MorePopup);
		if (ViewModel.Kind != "NOTE")
		{
			list.Insert(1, new CustomMenuItemViewModel((object)"Complete", Utils.GetString((ViewModel.Status == 0) ? "Done" : "TaskReopen"), (Geometry)null, "")
			{
				RightText = LocalSettings.Settings.ShortCutModel.CompleteTask,
				FontSize = 12,
				TextLineHeight = 16
			});
			TaskPomoSetDialog taskPomoSetDialog = new TaskPomoSetDialog(inSticky: true);
			taskPomoSetDialog.InitData(ViewModel.Id, force: true, "task_detail", "task_detail_pomo");
			taskPomoSetDialog.Closed += customMenuList.CloseSubPopup;
			list.Insert(3, new CustomMenuItemViewModel((object)"Focus", Utils.GetString("BeginFocus"), (Geometry)null, "")
			{
				FontSize = 12,
				TextLineHeight = 16,
				SubControl = taskPomoSetDialog
			});
		}
		MorePopup.VerticalOffset = ((ViewModel.Kind != "NOTE") ? (-128) : (-88));
		customMenuList.Operated += OnSelected;
		customMenuList.Show();
	}

	private void OnSelected(object sender, object e)
	{
		MorePopup.IsOpen = false;
		if (!(e is string text) || text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 8:
			switch (text[0])
			{
			case 'C':
				if (text == "Complete")
				{
					ToggleTask();
				}
				break;
			case 'S':
				if (text == "Settings")
				{
					OpenSettings();
				}
				break;
			}
			break;
		case 14:
			switch (text[0])
			{
			case 'R':
				if (text == "RightAlignment")
				{
					Align(isVertical: true, isRight: true);
				}
				break;
			case 'O':
				if (text == "OpenMainWindow")
				{
					OpenInApp();
				}
				break;
			}
			break;
		case 7:
			if (text == "AddTask")
			{
				CreateNewSticky();
			}
			break;
		case 12:
			if (text == "TopAlignment")
			{
				Align(isVertical: false, isRight: false);
			}
			break;
		case 13:
			if (text == "LeftAlignment")
			{
				Align(isVertical: true, isRight: false);
			}
			break;
		case 6:
			if (text == "Delete")
			{
				DeleteTask();
			}
			break;
		case 9:
		case 10:
		case 11:
			break;
		}
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		DetailControl.InvalidContentMargin();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		DetailControl.InvalidContentMargin();
		base.OnDeactivated(e);
	}

	private async void CollapseWindow()
	{
		_collapsed = !_collapsed;
		if (!_collapsed)
		{
			DetailControl.ScrollToTop();
			TitleGrid.Visibility = Visibility.Collapsed;
			SecondRow.Height = new GridLength(1.0, GridUnitType.Star);
			base.MaxHeight = double.PositiveInfinity;
			base.MinHeight = 80.0;
			BeginAnimation(FrameworkElement.HeightProperty, null);
			base.Height = _originHeight;
			StartExpand();
			DetailControl.BeginAnimation(UIElement.OpacityProperty, null);
			TitleGrid.BeginAnimation(UIElement.OpacityProperty, null);
			DetailControl.Opacity = 1.0;
			TitleGrid.Opacity = 0.0;
		}
		else
		{
			Keyboard.ClearFocus();
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
			_originHeight = base.ActualHeight;
			TopBackground.Background = System.Windows.Media.Brushes.Transparent;
			FirstRow.Height = new GridLength(22.0);
			Container.VerticalAlignment = VerticalAlignment.Center;
			TitleGrid.Visibility = Visibility.Visible;
			TitleGrid.BeginAnimation(UIElement.OpacityProperty, null);
			DetailControl.BeginAnimation(UIElement.OpacityProperty, null);
			TitleGrid.Opacity = 1.0;
			DetailControl.Opacity = 0.0;
			SecondRow.Height = new GridLength(0.0);
			BottomGrid.Visibility = Visibility.Collapsed;
			SetResourceReference(FrameworkElement.MinHeightProperty, "StickyHeight42");
			SetResourceReference(FrameworkElement.MaxHeightProperty, "StickyHeight42");
			BeginAnimation(FrameworkElement.HeightProperty, null);
			SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight42");
		}
		UpdateLocationCache();
		SetTopTooltip();
	}

	private async void CollapseWindowWithStory()
	{
		_storyPlaying = true;
		_collapsed = !_collapsed;
		if (!_collapsed)
		{
			DetailControl.ScrollToTop();
			SecondRow.Height = new GridLength(1.0, GridUnitType.Star);
			base.MaxHeight = double.PositiveInfinity;
			Storyboard obj = (Storyboard)FindResource("ExpandStory");
			if (obj.Children[0] is DoubleAnimation doubleAnimation)
			{
				doubleAnimation.To = _originHeight;
			}
			obj.Begin();
			await Task.Delay(100);
			StartExpand();
		}
		else
		{
			Keyboard.ClearFocus();
			FocusManager.SetFocusedElement(this, this);
			Keyboard.Focus(this);
			_originHeight = base.ActualHeight;
			TopBackground.Background = System.Windows.Media.Brushes.Transparent;
			SetResourceReference(FrameworkElement.MinHeightProperty, "StickyHeight42");
			double value = (double)FindResource("StickyHeight42");
			Storyboard obj2 = (Storyboard)FindResource("CollapseStory");
			if (obj2.Children[0] is DoubleAnimation doubleAnimation2)
			{
				doubleAnimation2.From = base.ActualHeight;
				doubleAnimation2.To = value;
			}
			obj2.Begin();
			FirstRow.Height = new GridLength(22.0);
			Container.VerticalAlignment = VerticalAlignment.Center;
			TitleGrid.Visibility = Visibility.Visible;
			await Task.Delay(180);
			StartCollapse();
		}
		SetTopTooltip();
	}

	private void StartExpand()
	{
		BottomGrid.Visibility = Visibility.Visible;
		FirstRow.Height = new GridLength(14.0);
		TopBackground.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "StickyTopColor");
		Container.VerticalAlignment = VerticalAlignment.Stretch;
		UpdateLocationCache();
	}

	private void StartCollapse()
	{
		((Storyboard)FindResource("ShowTitleStory")).Begin();
	}

	private void OnExpand(object sender, EventArgs e)
	{
		base.MinHeight = 80.0;
		BeginAnimation(FrameworkElement.HeightProperty, null);
		TitleGrid.Visibility = Visibility.Collapsed;
		_storyPlaying = false;
	}

	private void OnCollapsed(object sender, EventArgs e)
	{
		SecondRow.Height = new GridLength(0.0);
		BottomGrid.Visibility = Visibility.Collapsed;
		SetResourceReference(FrameworkElement.MaxHeightProperty, "StickyHeight42");
		BeginAnimation(FrameworkElement.HeightProperty, null);
		SetResourceReference(FrameworkElement.HeightProperty, "StickyHeight42");
		_storyPlaying = false;
		UpdateLocationCache();
	}

	private void UpdateLocationCache()
	{
		base.Dispatcher.Invoke(delegate
		{
			AddOrUpdateWindowLocation(new TaskStickyModel
			{
				TaskId = ViewModel.Id,
				Top = base.Top,
				Left = base.Left,
				Width = base.Width,
				Height = base.Height
			});
			AddOrUpdateDragWindowLocation();
			SaveSticky();
		});
	}

	private async Task SaveSticky()
	{
		await TaskStickyDao.SaveStickyLocation(ViewModel.Id, base.Left, base.Top, base.Width, _collapsed ? _originHeight : base.Height, !_collapsed);
	}

	private void OnWindowMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		((Storyboard)FindResource("ShowOptionStory")).Begin();
	}

	private void OnWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (!_editing)
		{
			((Storyboard)FindResource("HideOptionStory")).Begin();
		}
	}

	private void OnDateClick(object sender, MouseButtonEventArgs e)
	{
		UserActCollectUtils.AddClickEvent("sticky_note", "sticky_note_ui", "set_time");
		ViewModel.ShowDateSelector(this);
	}

	public async void SetEditing(bool editing)
	{
		_editing = editing;
		if (editing)
		{
			OnWindowMouseEnter(null, null);
			return;
		}
		await Task.Delay(100);
		if (!base.IsMouseOver)
		{
			OnWindowMouseLeave(null, null);
		}
	}

	private void TryToast(string toast)
	{
		TryToastString(null, toast);
	}

	public static async Task TryLoadSavedStickies()
	{
		List<TaskStickyModel> obj = await TaskStickyDao.GetAllAsync();
		UtilLog.Info("TryLoadSavedStickies");
		foreach (TaskStickyModel sticky in obj)
		{
			if (sticky.Closed)
			{
				continue;
			}
			if (!TaskCache.ExistTask(sticky.TaskId))
			{
				BaseDao<TaskStickyModel>.DeleteAsync(sticky);
				continue;
			}
			System.Windows.Application.Current?.Dispatcher.Invoke(delegate
			{
				if (!string.IsNullOrEmpty(TaskUtils.ToastOnOpenSticky(sticky.TaskId)))
				{
					sticky.Closed = true;
					BaseDao<TaskStickyModel>.UpdateAsync(sticky);
				}
				else
				{
					TaskStickyWindow taskStickyWindow = new TaskStickyWindow(sticky);
					_openedStickies.TryAdd(sticky.TaskId, taskStickyWindow);
					taskStickyWindow.Show();
				}
			});
		}
	}

	public static async Task CloseAllStickies()
	{
		try
		{
			UtilLog.Info("CloseAllStickies");
			foreach (KeyValuePair<string, TaskStickyWindow> openedSticky in _openedStickies)
			{
				openedSticky.Value.Close();
			}
		}
		catch (Exception)
		{
		}
		_openedStickies.Clear();
	}

	private async void DeleteTask()
	{
		TaskModel task = await TaskDao.GetThinTaskById(ViewModel.Id);
		if (task == null)
		{
			return;
		}
		if (ViewModel.IsEmptyTask())
		{
			TaskService.DeleteTask(ViewModel.Id, 2);
			return;
		}
		List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(task.id, task.projectId);
		if (list == null || list.Count <= 0)
		{
			await TaskService.DeleteTask(task.id);
			return;
		}
		list.Add(task);
		await TaskService.BatchDeleteTasks(list, showUndo: false);
	}

	public void TaskDeleted(string taskId)
	{
	}

	public void TryToastString(object sender, string e)
	{
		ToastGrid.Margin = new Thickness(8.0, 0.0, 8.0, (base.ActualHeight > 150.0) ? 30 : 10);
		WindowToastHelper.ToastString(ToastGrid, e, 600.0, 30.0, 10.0);
	}

	public async Task<bool> BatchDeleteTask(List<TaskModel> tasks)
	{
		return false;
	}

	public void TaskComplete(CloseUndoToast undo)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, undo);
	}

	public void TryHideToast()
	{
	}

	public void ToastDeleteRecUndo(List<TaskDeleteRecurrenceUndoEntity> undoModels)
	{
	}

	public void ToastMoveProjectControl(string projectProjectId, string taskName = null, MoveToastType moveType = MoveToastType.Move)
	{
	}

	public void Toast(FrameworkElement uiElement)
	{
		WindowToastHelper.ShowAndHideToast(ToastGrid, uiElement);
	}

	public static bool TryCloseInProject(string projectId)
	{
		bool result = false;
		KeyValuePair<string, TaskStickyWindow>[] array = _openedStickies.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, TaskStickyWindow> keyValuePair = array[i];
			TaskStickyWindow value = keyValuePair.Value;
			if (value.ViewModel.SourceModel.ProjectId == projectId)
			{
				value.Close();
				TaskStickyDao.CloseAsync(keyValuePair.Key);
				result = true;
			}
		}
		return result;
	}

	public static bool ExistSticky(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			return _openedStickies.ContainsKey(taskId);
		}
		return false;
	}

	private void OnColorClick(object sender, MouseButtonEventArgs e)
	{
		ShowColorSelector();
		UserActCollectUtils.AddClickEvent("sticky_note", "sticky_note_ui", "color");
	}

	public void ShowColorSelector()
	{
		SetEditing(editing: true);
		StickyColorSelector stickyColorSelector = new StickyColorSelector(_color);
		stickyColorSelector.SetPlacementTarget(ThemeBorder);
		stickyColorSelector.ColorSelect += OnColorSelect;
		stickyColorSelector.Closed += OnPopupClosed;
		stickyColorSelector.IsOpen = true;
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
		SetEditing(editing: false);
	}

	public void QuickChangeColor()
	{
		if (ProChecker.CheckPro(ProType.StickyColor))
		{
			if (!Enum.TryParse<StickyColorKey>(_color, ignoreCase: true, out var result))
			{
				result = StickyColorKey.Default;
			}
			result++;
			if ((int)result >= Enum.GetValues(typeof(StickyColorKey)).Cast<StickyColorKey>().Count() - 1)
			{
				result = StickyColorKey.Default;
			}
			SetStickyColor(result.ToString());
			TaskStickyDao.SaveStickyColor(ViewModel.Id, result.ToString());
		}
	}

	private void OnColorSelect(object sender, string e)
	{
		if (!(_color == e))
		{
			SetStickyColor(e);
			TaskStickyDao.SaveStickyColor(ViewModel.Id, e);
		}
	}

	private async void CreateNewSticky()
	{
		await Task.Delay(50);
		UserActCollectUtils.AddClickEvent("sticky_note", "om", "add");
		CreateNew();
	}

	public async Task CreateNew()
	{
		TaskModel task = await TaskService.CreateNewTask();
		if (task == null)
		{
			return;
		}
		TaskStickyModel stickyModel = new TaskStickyModel(task.id, LocalSettings.Settings.LoginUserId);
		stickyModel.Color = GetDefaultColor();
		stickyModel.TopMost = LocalSettings.Settings.ExtraSettings.StickyDefaultPin;
		stickyModel.Left = base.Left;
		double top = base.Top + base.Height - 15.0;
		System.Drawing.Rectangle rect = ScreenPositionUtils.GetWindowPositionRect(this);
		if (top >= (double)(rect.Top + rect.Height - 100))
		{
			top = rect.Top;
			int topExtra = 20;
			List<TaskStickyModel> source = (await TaskStickyDao.GetOpenedAsync()) ?? new List<TaskStickyModel>();
			while (source.Any((TaskStickyModel s) => Math.Abs(s.Left - stickyModel.Left) < 4.0 && Math.Abs(s.Top - top) < 4.0))
			{
				top = rect.Top + topExtra;
				topExtra += 20;
				if (topExtra == 500)
				{
					break;
				}
			}
		}
		else if (top >= (double)(rect.Top + rect.Height) - stickyModel.Height)
		{
			top = (double)(rect.Top + rect.Height) - stickyModel.Height;
		}
		stickyModel.Top = top;
		await TaskStickyDao.AddStickyAsync(stickyModel);
		TaskStickyWindow window = new TaskStickyWindow(await GetTaskStickyModel(task.id));
		window._checkEmpty = true;
		window.Show();
		_openedStickies[task.id] = window;
		await Task.Delay(50);
		window.Activate();
	}

	private async void ToggleTask()
	{
		UserActCollectUtils.AddClickEvent("sticky_note", "om", "done");
		ViewModel?.CompleteTaskCommand();
	}

	private async void OpenInApp()
	{
		UserActCollectUtils.AddClickEvent("sticky_note", "om", "open_main_window");
		App.ShowMainWindow(ViewModel.SourceModel.Id, string.Empty);
	}

	public static async Task<List<TaskStickyWindow>> ShowTaskSticky(List<string> taskIds)
	{
		if (taskIds == null)
		{
			return null;
		}
		await Task.Delay(50);
		System.Drawing.Rectangle rect = ScreenPositionUtils.GetMousePositionRect();
		List<TaskStickyModel> stickies = (await TaskStickyDao.GetAllAsync()) ?? new List<TaskStickyModel>();
		stickies = stickies.Where((TaskStickyModel s) => !s.Closed || taskIds.Contains(s.TaskId)).ToList();
		List<TaskStickyModel> models = new List<TaskStickyModel>();
		int topExtra = 0;
		foreach (string id in taskIds)
		{
			TaskStickyModel model = await GetTaskStickyModel(id);
			if (model == null)
			{
				if (_openedStickies.ContainsKey(id))
				{
					_openedStickies[id].Show();
					_openedStickies[id].Activate();
				}
				continue;
			}
			if (Math.Abs(model.Top + 1.0) < 0.01 && Math.Abs(model.Left + 1.0) < 0.01)
			{
				model.Left = (double)rect.Left + ((double)rect.Width - model.Width) / 2.0;
				model.Top = rect.Top + rect.Height / 4;
				if (topExtra >= 500)
				{
					model.Top = rect.Top + topExtra;
				}
				else
				{
					int num = topExtra;
					while (true)
					{
						TaskStickyModel taskStickyModel = stickies.FirstOrDefault((TaskStickyModel sticky) => Math.Abs(sticky.Left - model.Left) < 4.0 && Math.Abs(sticky.Top - model.Top) < 4.0);
						if (taskStickyModel == null)
						{
							break;
						}
						model.Top += taskStickyModel.Height - 15.0;
						if (model.Top >= (double)(rect.Top + rect.Height - 100))
						{
							model.Top = rect.Top + num;
							num += 20;
							if (num >= 520)
							{
								break;
							}
						}
						else if (model.Top >= (double)(rect.Top + rect.Height) - model.Height)
						{
							model.Top = (double)(rect.Top + rect.Height) - model.Height;
						}
					}
					if (num != topExtra)
					{
						topExtra = num - 20;
					}
				}
				await BaseDao<TaskStickyModel>.UpdateAsync(model);
				stickies.Add(model);
			}
			models.Add(model);
		}
		List<TaskStickyWindow> list = new List<TaskStickyWindow>();
		foreach (TaskStickyModel item in models)
		{
			if (item != null)
			{
				TaskStickyWindow taskStickyWindow = new TaskStickyWindow(item);
				taskStickyWindow.ShowAndActive();
				list.Add(taskStickyWindow);
				_openedStickies[item.TaskId] = taskStickyWindow;
			}
		}
		return list;
	}

	private async Task ShowAndActive()
	{
		Show();
		await Task.Delay(50);
		Activate();
	}

	public static async Task CreateNewStickyStatic()
	{
		TaskModel taskModel = await TaskService.CreateNewTask();
		if (taskModel != null)
		{
			List<TaskStickyWindow> list = await ShowTaskSticky(new List<string> { taskModel.id });
			if (list != null && list.Count > 0)
			{
				list[0]._checkEmpty = true;
			}
		}
	}

	private void InitShortCut()
	{
		_keyBindings.Add(("StickyColor", GetKeyBinding(TaskStickyCommands.SetColorCommand)));
		_keyBindings.Add(("AddTask", GetKeyBinding(TaskStickyCommands.CreatNewCommand)));
		_keyBindings.Add(("PinSticky", GetKeyBinding(TaskStickyCommands.PinCommand)));
		_keyBindings.Add(("CompleteTask", GetKeyBinding(TaskStickyCommands.CompleteCommand)));
		_keyBindings.Add(("StickyCollapse", GetKeyBinding(TaskStickyCommands.StickyCollapseCommand)));
		_keyBindings.Add(("StickyAlignTop", GetKeyBinding(TaskStickyCommands.StickyAlignTopCommand)));
		_keyBindings.Add(("StickyAlignLeft", GetKeyBinding(TaskStickyCommands.StickyAlignLeftCommand)));
		_keyBindings.Add(("StickyAlignRight", GetKeyBinding(TaskStickyCommands.StickyAlignRightCommand)));
		foreach (var (key, kb) in _keyBindings)
		{
			KeyBindingManager.TryAddKeyBinding(key, kb);
		}
		KeyBinding GetKeyBinding(ICommand command)
		{
			if (command != null)
			{
				KeyBinding keyBinding = new KeyBinding(command, new KeyGesture(Key.None))
				{
					CommandParameter = this
				};
				base.InputBindings.Add(keyBinding);
				return keyBinding;
			}
			return null;
		}
	}

	public void RemoveKeyBinding()
	{
		foreach (var (key, keyBinding) in _keyBindings)
		{
			KeyBindingManager.RemoveKeyBinding(key, keyBinding);
		}
	}

	public static void OnAppExit()
	{
		WindowManager.AppLockOrExit = true;
	}

	public void Pin()
	{
		bool flag = !base.Topmost;
		TaskStickyDao.SaveStickyTopMost(ViewModel.Id, flag);
		SetTopMost(flag);
		TryToast(Utils.GetString(flag ? "Pinned" : "Unpinned"));
	}

	public void CompleteTaskCommand()
	{
		ViewModel?.CompleteTaskCommand();
	}

	public async void Reload()
	{
		DetailControl.Reload();
		await Task.Delay(100);
		ViewModel.ResetDateText();
	}

	public static void ReloadStickies()
	{
		foreach (TaskStickyWindow item in _openedStickies.Values.ToList())
		{
			item?.Reload();
		}
	}

	public static void SetShowInTaskBar()
	{
		foreach (TaskStickyWindow item in _openedStickies.Values.ToList())
		{
			if (item != null)
			{
				double left = item.Left;
				item.ShowInTaskbar = !LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar;
				item.Left -= 2.0;
				item.Left = left;
			}
		}
	}

	private void OpenSettings()
	{
		SettingsHelper.PullRemoteSettings();
		UserActCollectUtils.AddClickEvent("sticky_note", "om", "settings");
		SettingsHelper.PullRemoteSettings();
		SettingDialog.ShowSettingDialog(SettingsType.StickyNote, this);
	}

	private void AnimSetPosition(double left, double top)
	{
		RemoveAnimation();
		Storyboard obj = (Storyboard)FindResource("MoveStory");
		DoubleAnimation doubleAnimation = (DoubleAnimation)obj.Children[0];
		DoubleAnimation doubleAnimation2 = (DoubleAnimation)obj.Children[1];
		DoubleAnimation doubleAnimation3 = (DoubleAnimation)obj.Children[2];
		DoubleAnimation obj2 = (DoubleAnimation)obj.Children[3];
		doubleAnimation.To = left;
		doubleAnimation2.To = top;
		doubleAnimation3.To = (ResetOnAlign ? 254.0 : base.Width);
		obj2.To = ((!_collapsed && ResetOnAlign) ? 228.0 : base.Height);
		if (_collapsed && ResetOnAlign)
		{
			base.Width = 254.0;
			_originHeight = 228.0;
		}
		obj.Begin();
	}

	private void RemoveAnimation()
	{
		BeginAnimation(Window.LeftProperty, null);
		BeginAnimation(Window.TopProperty, null);
		BeginAnimation(FrameworkElement.WidthProperty, null);
		BeginAnimation(FrameworkElement.HeightProperty, null);
	}

	public void CollapseOrExpandAll()
	{
		System.Drawing.Rectangle rectangle = WindowSizing.GetMonitorFromWindow(this);
		if (rectangle == default(System.Drawing.Rectangle))
		{
			rectangle = ScreenPositionUtils.GetWindowPositionRect(this);
		}
		List<TaskStickyWindow> list = new List<TaskStickyWindow>();
		foreach (TaskStickyWindow item in _openedStickies.Values.ToList())
		{
			System.Drawing.Rectangle rectangle2 = WindowSizing.GetMonitorFromWindow(item);
			if (rectangle2 == default(System.Drawing.Rectangle))
			{
				rectangle2 = ScreenPositionUtils.GetWindowPositionRect(item);
			}
			if (rectangle == rectangle2)
			{
				list.Add(item);
			}
		}
		bool flag = list.Any((TaskStickyWindow w) => !w._collapsed);
		if (list.Any((TaskStickyWindow window) => window._storyPlaying))
		{
			return;
		}
		foreach (TaskStickyWindow item2 in list)
		{
			if (item2._collapsed != flag)
			{
				item2.CollapseWindow();
			}
		}
	}

	public void OnOpacityChanged(object sender, EventArgs e)
	{
		SetOpacity();
	}

	private void SetOpacity()
	{
		BottomBackground.Opacity = Math.Max(0.01, LocalSettings.Settings.ExtraSettings.StickyOpacity / 100.0);
		((DropShadowEffect)BackBorder.Effect).Opacity = ((LocalSettings.Settings.ExtraSettings.StickyOpacity < 80.0) ? 0.0 : 0.24);
	}

	public static void ShowHideAllStickyStatic()
	{
		List<TaskStickyWindow> list = _openedStickies.Values.ToList();
		if (list.Count == 0)
		{
			return;
		}
		try
		{
			changeStateFromShortKey = true;
			bool flag = list.Any((TaskStickyWindow w) => w.WindowState != WindowState.Minimized);
			foreach (TaskStickyWindow item in list)
			{
				if (flag && item.WindowState != WindowState.Minimized)
				{
					if (LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar)
					{
						item.ShowInTaskbar = true;
					}
					item.WindowState = WindowState.Minimized;
				}
				if (!flag && item.WindowState == WindowState.Minimized)
				{
					if (LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar)
					{
						item.ShowInTaskbar = false;
					}
					item.WindowState = WindowState.Normal;
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("快捷键切换标签状态" + ex);
		}
		changeStateFromShortKey = false;
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		if (base.WindowState == WindowState.Normal && base.ShowInTaskbar && LocalSettings.Settings.ExtraSettings.StickyHideInTaskBar)
		{
			base.ShowInTaskbar = false;
		}
		if (base.Topmost && base.WindowState != WindowState.Normal && !changeStateFromShortKey)
		{
			base.WindowState = WindowState.Normal;
		}
	}

	private void SetStickyColor(StickyColorKey colorKey)
	{
		base.Resources["StickyBackColor"] = StickyColorViewModel.GetBackColor(colorKey);
		base.Resources["StickyTopColor"] = StickyColorViewModel.GetTopColor(colorKey);
		SolidColorBrush topColor = StickyColorViewModel.GetTopColor(colorKey);
		topColor.Opacity = ((colorKey == StickyColorKey.Default || colorKey == StickyColorKey.DarkBlue || colorKey == StickyColorKey.Dark) ? 0.4 : 0.6);
		base.Resources["StickyNotionBackColor"] = topColor;
		if (BorderColorDict.ContainsKey(colorKey))
		{
			base.Resources["StickyBorderColor"] = ThemeUtil.GetColorInString(BorderColorDict[colorKey]);
		}
		else
		{
			base.Resources["StickyBorderColor"] = System.Windows.Media.Brushes.Transparent;
		}
		if (PopupBackColorDict.ContainsKey(colorKey))
		{
			base.Resources["PopupBackground"] = ThemeUtil.GetColorInString(PopupBackColorDict[colorKey]);
		}
		else
		{
			base.Resources["PopupBackground"] = System.Windows.Media.Brushes.White;
		}
		base.Resources["StickyTextColor20"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#33FFFFFF" : "#33191919");
		base.Resources["StickyTextColor40"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#66FFFFFF" : "#66191919");
		base.Resources["StickyTextColor60"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#99FFFFFF" : "#99191919");
		base.Resources["StickyContentTextColor"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#82FFFFFF" : "#CC191919");
		base.Resources["StickyTitleTextColor"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#B3FFFFFF" : "#E8191919");
		base.Resources["StickyCompletedTextColor"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#33FFFFFF" : "#66191919");
		base.Resources["StickyTextColor100"] = ThemeUtil.GetColorInString(IsDarkTheme(colorKey) ? "#FFFFFF" : "#191919");
		base.Resources["StickyBorderThickness"] = new Thickness(IsDarkTheme(colorKey) ? 1 : 0);
	}

	private static bool IsDarkTheme(StickyColorKey colorKey)
	{
		return colorKey.ToString().StartsWith("Dark");
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/taskstickywindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (TaskStickyWindow)target;
			break;
		case 2:
			((DoubleAnimation)target).Completed += OnCollapsed;
			break;
		case 3:
			((DoubleAnimation)target).Completed += OnExpand;
			break;
		case 4:
			BackBorder = (Border)target;
			break;
		case 5:
			Container = (Grid)target;
			break;
		case 6:
			FirstRow = (RowDefinition)target;
			break;
		case 7:
			SecondRow = (RowDefinition)target;
			break;
		case 8:
			BottomBackground = (Border)target;
			break;
		case 9:
			TopBackground = (Border)target;
			TopBackground.PreviewMouseLeftButtonDown += TopGridMouseDown;
			TopBackground.MouseMove += TryDragWindow;
			TopBackground.PreviewMouseLeftButtonUp += OnMouseUp;
			break;
		case 10:
			TitleGrid = (Border)target;
			break;
		case 11:
			TopStatusIcon = (Path)target;
			TopStatusIcon.MouseLeftButtonUp += OnTopStatusIconClick;
			break;
		case 12:
			TopTitleText = (DetailTextBox)target;
			break;
		case 13:
			TopGrid = (StackPanel)target;
			break;
		case 14:
			ThemeBorder = (Border)target;
			ThemeBorder.MouseLeftButtonUp += OnColorClick;
			break;
		case 15:
			ColorTooltip = (System.Windows.Controls.ToolTip)target;
			break;
		case 16:
			PinGrid = (Border)target;
			PinGrid.MouseLeftButtonUp += OnPinClick;
			break;
		case 17:
			PinTooltip = (System.Windows.Controls.ToolTip)target;
			break;
		case 18:
			PinPath = (Path)target;
			break;
		case 19:
			CloseGrid = (Border)target;
			CloseGrid.MouseLeftButtonUp += OnCloseClick;
			break;
		case 20:
			DetailControl = (TaskDetailStickyView)target;
			break;
		case 21:
			BottomGrid = (Grid)target;
			break;
		case 22:
			DateBtn = (Border)target;
			DateBtn.MouseLeftButtonUp += OnDateClick;
			break;
		case 23:
			DatePanel = (StackPanel)target;
			break;
		case 24:
			((Border)target).MouseLeftButtonUp += OnMoreClick;
			break;
		case 25:
			MoreIcon = (Path)target;
			break;
		case 26:
			MorePopup = (EscPopup)target;
			break;
		case 27:
			ToastGrid = (Grid)target;
			break;
		case 28:
			SizeChangeBorder = (Border)target;
			SizeChangeBorder.MouseLeftButtonDown += OnSizeChangeMouseDown;
			SizeChangeBorder.MouseMove += OnBorderMouseMove;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
