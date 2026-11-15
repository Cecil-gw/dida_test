using System;

namespace ticktick_WPF.Views.AI;

internal sealed class AIClientHostState
{
	private AIClientPresentation _presentation;

	private bool _isGenerating;

	private bool _isFloatingEntryHidden;

	private bool _isTemporarilySuppressed;

	public AIClientPresentation Presentation => _presentation;

	public bool IsGenerating => _isGenerating;

	public bool IsFloatingEntryHidden => _isFloatingEntryHidden;

	public bool IsTemporarilySuppressed => _isTemporarilySuppressed;

	public bool ShouldShowFloatingEntry
	{
		get
		{
			if (_presentation == AIClientPresentation.Background && !_isFloatingEntryHidden)
			{
				return !_isTemporarilySuppressed;
			}
			return false;
		}
	}

	public bool ShouldShowFloatingWindow
	{
		get
		{
			if (_presentation == AIClientPresentation.Floating && !_isFloatingEntryHidden)
			{
				return !_isTemporarilySuppressed;
			}
			return false;
		}
	}

	public bool ShouldAnimateFloatingEntry
	{
		get
		{
			if (ShouldShowFloatingEntry)
			{
				return _isGenerating;
			}
			return false;
		}
	}

	public event EventHandler Changed;

	public void SetMainModuleIsAI(bool isAI)
	{
		if (isAI)
		{
			SetPresentation(AIClientPresentation.FullScreen);
		}
		else if (_presentation == AIClientPresentation.FullScreen)
		{
			SetPresentation(AIClientPresentation.Background);
		}
	}

	public bool TryOpenFloatingWindow()
	{
		if (_isFloatingEntryHidden)
		{
			return false;
		}
		return SetPresentation(AIClientPresentation.Floating);
	}

	public bool CloseFloatingWindow()
	{
		if (_presentation != AIClientPresentation.Floating)
		{
			return false;
		}
		return SetPresentation(AIClientPresentation.Background);
	}

	public bool SetGenerating(bool isGenerating)
	{
		if (_isGenerating == isGenerating)
		{
			return false;
		}
		_isGenerating = isGenerating;
		OnChanged();
		return true;
	}

	public bool HideFloatingEntry()
	{
		if (_isFloatingEntryHidden)
		{
			return false;
		}
		_isFloatingEntryHidden = true;
		if (_presentation == AIClientPresentation.Floating)
		{
			_presentation = AIClientPresentation.Background;
		}
		OnChanged();
		return true;
	}

	public bool RestoreFloatingEntry()
	{
		if (!_isFloatingEntryHidden)
		{
			return false;
		}
		_isFloatingEntryHidden = false;
		OnChanged();
		return true;
	}

	public bool SetTemporarilySuppressed(bool isSuppressed)
	{
		if (_isTemporarilySuppressed == isSuppressed)
		{
			return false;
		}
		_isTemporarilySuppressed = isSuppressed;
		OnChanged();
		return true;
	}

	private bool SetPresentation(AIClientPresentation presentation)
	{
		if (_presentation == presentation)
		{
			return false;
		}
		_presentation = presentation;
		OnChanged();
		return true;
	}

	private void OnChanged()
	{
		this.Changed?.Invoke(this, EventArgs.Empty);
	}
}
