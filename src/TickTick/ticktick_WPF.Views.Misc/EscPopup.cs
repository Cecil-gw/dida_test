using System;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class EscPopup : Popup
{
	public bool NeedFocus = true;

	public bool RestoreFocus { get; set; } = true;

	public EscPopup()
	{
		base.AllowsTransparency = true;
		base.Opened += OnOpened;
		base.PreviewKeyDown += OnKeyDown;
		base.KeyUp += OnKeyUp;
		base.Closed += OnClosed;
		AppLockWindowManager.TrackPopup(this);
	}

	private void OnClosed(object sender, EventArgs e)
	{
		if (base.PlacementTarget != null && RestoreFocus && !AppLockWindowManager.IsLocked)
		{
			HwndHelper.SetFocus(base.PlacementTarget);
		}
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
		{
			ITabControl tabControl2 = GetTabControl();
			if (tabControl2 != null && tabControl2.HandleEsc())
			{
				e.Handled = true;
				break;
			}
			Close();
			e.Handled = true;
			break;
		}
		case Key.Return:
		{
			ITabControl tabControl = GetTabControl();
			if (tabControl != null)
			{
				e.Handled = tabControl.HandleEnter();
			}
			break;
		}
		}
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			e.Handled = true;
			break;
		case Key.Up:
		case Key.Down:
		{
			ITabControl tabControl = GetTabControl();
			if (tabControl != null)
			{
				e.Handled = tabControl.UpDownSelect(e.Key == Key.Up);
			}
			break;
		}
		case Key.Left:
		case Key.Right:
		{
			ITabControl tabControl2 = GetTabControl();
			if (tabControl2 != null)
			{
				e.Handled = tabControl2.LeftRightSelect(e.Key == Key.Left);
			}
			break;
		}
		case Key.Tab:
			GetTabControl()?.HandleTab(Utils.IfShiftPressed());
			e.Handled = true;
			break;
		}
	}

	private ITabControl GetTabControl()
	{
		if (base.Child == null)
		{
			return null;
		}
		if (base.Child is ITabControl result)
		{
			return result;
		}
		return Utils.FindSingleVisualChildren<ITabControl>(base.Child);
	}

	private async void OnOpened(object sender, EventArgs e)
	{
		await Task.Delay(50);
		if (NeedFocus && base.IsOpen && !AppLockWindowManager.IsLocked)
		{
			HwndHelper.SetFocus(this, !base.StaysOpen);
		}
	}

	public bool HandleTab(bool shift)
	{
		return GetTabControl()?.HandleTab(shift) ?? false;
	}

	public bool HandleUpDown(bool isUp)
	{
		return GetTabControl()?.UpDownSelect(isUp) ?? false;
	}

	public bool HandleLeftRight(bool isLeft)
	{
		return GetTabControl()?.LeftRightSelect(isLeft) ?? false;
	}

	public bool HandleEnter()
	{
		return GetTabControl()?.HandleEnter() ?? false;
	}

	public bool HandleEsc()
	{
		ITabControl tabControl = GetTabControl();
		if (tabControl != null && tabControl.HandleEsc())
		{
			return true;
		}
		Close();
		return false;
	}

	public void Close()
	{
		base.IsOpen = false;
	}

	public async void TryFocus()
	{
		Keyboard.ClearFocus();
		await Task.Delay(10);
		FocusManager.SetFocusedElement(this, this);
		Keyboard.Focus(this);
	}
}
