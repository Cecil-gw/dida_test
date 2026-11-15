using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ticktick_WPF.Views.Misc;

internal class QuickSetProjectPopupState
{
	private int _closeVersion;

	private string _suppressedTriggerKey;

	public bool IsHidden { get; private set; }

	public async void HideUntil(EscPopup popup, Func<bool> shouldDeferClose, Action onHidden = null)
	{
		if (popup != null)
		{
			int version = ++_closeVersion;
			Hide(popup);
			onHidden?.Invoke();
			while (version == _closeVersion && shouldDeferClose())
			{
				await Task.Delay(50);
			}
			if (version == _closeVersion)
			{
				popup.IsOpen = false;
			}
		}
	}

	public void Restore(Popup popup)
	{
		_closeVersion++;
		IsHidden = false;
		if (popup != null)
		{
			popup.IsHitTestVisible = true;
			UIElement child = popup.Child;
			if (child != null)
			{
				child.Opacity = 1.0;
				child.IsHitTestVisible = true;
			}
		}
	}

	public void SuppressTrigger(int markIndex, string text, string filterContent)
	{
		_suppressedTriggerKey = ((markIndex >= 0 && markIndex < (text?.Length ?? 0) && !string.IsNullOrEmpty(filterContent)) ? GetTriggerKey(markIndex, text[markIndex].ToString(), filterContent) : null);
	}

	public void ClearSuppressedTrigger()
	{
		_suppressedTriggerKey = null;
	}

	public bool IsTriggerSuppressed(int markIndex, string mark, string filterContent)
	{
		if (!string.IsNullOrEmpty(filterContent) && _suppressedTriggerKey == GetTriggerKey(markIndex, mark, filterContent))
		{
			return true;
		}
		ClearSuppressedTrigger();
		return false;
	}

	private static string GetTriggerKey(int markIndex, string mark, string filterContent)
	{
		if (markIndex < 0 || string.IsNullOrEmpty(mark))
		{
			return null;
		}
		return markIndex + ":" + mark + ":" + filterContent;
	}

	private void Hide(Popup popup)
	{
		IsHidden = true;
		popup.IsHitTestVisible = false;
		UIElement child = popup.Child;
		if (child != null)
		{
			child.Opacity = 0.0;
			child.IsHitTestVisible = false;
		}
	}
}
