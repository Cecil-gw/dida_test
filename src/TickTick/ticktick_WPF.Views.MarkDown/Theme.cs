using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public class Theme : INotifyPropertyChanged
{
	private double _codeHeight = 1.0;

	private double _header1Height = 1.29;

	private double _header2Height = 1.14;

	private double _header3Height = 1.0;

	private double _header4Height = 1.0;

	private double _header5Height = 1.0;

	private double _header6Height = 1.0;

	private Highlight _highlightBlockCode = new Highlight
	{
		Name = "BlockCode"
	};

	private Highlight _highlightBlockQuote = new Highlight
	{
		Name = "BlockQuote"
	};

	private Highlight _highlightCheckedItem = new Highlight
	{
		Name = "CheckedItem",
		Strikethrough = true
	};

	private Highlight _highlightEmphasis = new Highlight
	{
		Name = "Emphasis",
		FontStyle = "italic"
	};

	private Highlight _highlightFlag = new Highlight
	{
		Name = "Flag"
	};

	private Highlight _highlightFlagExtra = new Highlight
	{
		Name = "FlagExtra"
	};

	private Highlight _highlightHeading = new Highlight
	{
		Name = "transparent",
		FontWeight = "bold"
	};

	private Highlight _highlightHighlight = new Highlight
	{
		Name = "Highlight"
	};

	private Highlight _highlightInlineCode = new Highlight
	{
		Name = "InlineCode"
	};

	private Highlight _highlightLink = new Highlight
	{
		Name = "Link"
	};

	private Highlight _highlightNormal = new Highlight
	{
		Name = "Normal"
	};

	private Highlight _highlightStrikethrough = new Highlight
	{
		Name = "Strikethrough",
		Strikethrough = true
	};

	private Highlight _highlightStrongEmphasis = new Highlight
	{
		Name = "StrongEmphasis",
		FontWeight = "bold"
	};

	private Highlight _highlightTransparent = new Highlight
	{
		Name = "Highlight"
	};

	private Highlight _highlightUnderLine = new Highlight
	{
		Name = "UnderLine",
		Underline = true
	};

	private string _name = "Zenburn";

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			Set(ref _name, value, "Name");
		}
	}

	public double Header1Height
	{
		get
		{
			return _header1Height;
		}
		set
		{
			Set(ref _header1Height, value, "Header1Height");
		}
	}

	public double Header2Height
	{
		get
		{
			return _header2Height;
		}
		set
		{
			Set(ref _header2Height, value, "Header2Height");
		}
	}

	public double Header3Height
	{
		get
		{
			return _header3Height;
		}
		set
		{
			Set(ref _header3Height, value, "Header3Height");
		}
	}

	public double Header4Height
	{
		get
		{
			return _header4Height;
		}
		set
		{
			Set(ref _header4Height, value, "Header4Height");
		}
	}

	public double Header5Height
	{
		get
		{
			return _header5Height;
		}
		set
		{
			Set(ref _header5Height, value, "Header5Height");
		}
	}

	public double Header6Height
	{
		get
		{
			return _header6Height;
		}
		set
		{
			Set(ref _header6Height, value, "Header6Height");
		}
	}

	public Highlight HighlightHeading
	{
		get
		{
			return _highlightHeading;
		}
		set
		{
			Set(ref _highlightHeading, value, "HighlightHeading");
		}
	}

	public Highlight HighlightEmphasis
	{
		get
		{
			return _highlightEmphasis;
		}
		set
		{
			Set(ref _highlightEmphasis, value, "HighlightEmphasis");
		}
	}

	public Highlight HighlightStrongEmphasis
	{
		get
		{
			return _highlightStrongEmphasis;
		}
		set
		{
			Set(ref _highlightStrongEmphasis, value, "HighlightStrongEmphasis");
		}
	}

	public Highlight HighlightInlineCode
	{
		get
		{
			return _highlightInlineCode;
		}
		set
		{
			Set(ref _highlightInlineCode, value, "HighlightInlineCode");
		}
	}

	public Highlight HighlightBlockCode
	{
		get
		{
			return _highlightBlockCode;
		}
		set
		{
			Set(ref _highlightBlockCode, value, "HighlightBlockCode");
		}
	}

	public Highlight HighlightBlockQuote
	{
		get
		{
			return _highlightBlockQuote;
		}
		set
		{
			Set(ref _highlightBlockQuote, value, "HighlightBlockQuote");
		}
	}

	public Highlight HighlightLink
	{
		get
		{
			return _highlightLink;
		}
		set
		{
			Set(ref _highlightLink, value, "HighlightLink");
		}
	}

	public Highlight HighlightFlagExtra
	{
		get
		{
			return _highlightFlagExtra;
		}
		set
		{
			Set(ref _highlightFlagExtra, value, "HighlightFlagExtra");
		}
	}

	public Highlight HighlightFlag
	{
		get
		{
			return _highlightFlag;
		}
		set
		{
			Set(ref _highlightFlag, value, "HighlightFlag");
		}
	}

	public Highlight HighlightStrikethrough
	{
		get
		{
			return _highlightStrikethrough;
		}
		set
		{
			Set(ref _highlightStrikethrough, value, "HighlightStrikethrough");
		}
	}

	public Highlight HighlightUnderLine
	{
		get
		{
			return _highlightUnderLine;
		}
		set
		{
			Set(ref _highlightUnderLine, value, "HighlightUnderLine");
		}
	}

	public Highlight HighlightHighlight
	{
		get
		{
			return _highlightHighlight;
		}
		set
		{
			Set(ref _highlightHighlight, value, "HighlightHighlight");
		}
	}

	public Highlight HighlightTransparent
	{
		get
		{
			return _highlightTransparent;
		}
		set
		{
			Set(ref _highlightTransparent, value, "HighlightTransparent");
		}
	}

	public Highlight HighlightNormal
	{
		get
		{
			return _highlightNormal;
		}
		set
		{
			Set(ref _highlightNormal, value, "HighlightNormal");
		}
	}

	public Highlight HighlightCheckedItem
	{
		get
		{
			return _highlightCheckedItem;
		}
		set
		{
			Set(ref _highlightCheckedItem, value, "HighlightCheckedItem");
		}
	}

	public double CodeHeight
	{
		get
		{
			return _codeHeight;
		}
		set
		{
			Set(ref _codeHeight, value, "CodeHeight");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public Theme(FrameworkElement element = null)
	{
		_highlightFlag.Foreground = ThemeUtil.GetColor("BaseColorOpacity20", element);
		_highlightFlagExtra.Foreground = ThemeUtil.GetColor("BaseColorOpacity20", element);
		_highlightFlagExtra.Background = new SolidColorBrush(Colors.Transparent);
		_highlightHighlight.Foreground = null;
		_highlightHighlight.Background = new SolidColorBrush(Colors.Transparent);
		_highlightTransparent.Foreground = new SolidColorBrush(Colors.Transparent);
		_highlightLink.Foreground = ThemeUtil.GetPrimaryColor(1.0);
		_highlightInlineCode.Foreground = ThemeUtil.GetColor("BaseColorOpacity80", element);
		_highlightInlineCode.Background = ThemeUtil.GetColor("InlineCodeBackground", element);
		_highlightInlineCode.FontType = Constants.DefaultMonoFont;
		_highlightBlockCode.Foreground = ThemeUtil.GetColor("BaseColorOpacity100", element);
		_highlightBlockCode.Background = ThemeUtil.GetColor("CodeBackground", element);
		_highlightBlockCode.FontType = Constants.DefaultMonoFont;
		_highlightBlockQuote.Foreground = ThemeUtil.GetColor("BaseSolidColorOpacity60", element);
		_highlightStrikethrough.Foreground = ThemeUtil.GetColor("BaseColorOpacity40", element);
		_highlightStrikethrough.TextDecorations = new TextDecorationCollection
		{
			new TextDecoration
			{
				Location = TextDecorationLocation.Strikethrough,
				Pen = new Pen(ThemeUtil.GetColor("BaseColorOpacity40", element), 1.0)
			}
		};
		_highlightCheckedItem.Foreground = ThemeUtil.GetColor("BaseColorOpacity40", element);
	}

	private void Set<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
	{
		if (!EqualityComparer<T>.Default.Equals(property, value))
		{
			property = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
