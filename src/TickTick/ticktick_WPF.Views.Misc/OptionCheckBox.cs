using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Emoji.Wpf;

namespace ticktick_WPF.Views.Misc;

public class OptionCheckBox : UserControl, IComponentConnector
{
	public static readonly DependencyProperty ShowCheckIconProperty = DependencyProperty.Register("ShowCheckIcon", typeof(bool), typeof(OptionCheckBox), new PropertyMetadata(true));

	public static readonly DependencyProperty HoverSelectedProperty = DependencyProperty.Register("HoverSelected", typeof(bool), typeof(OptionCheckBox), new PropertyMetadata(false));

	public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(bool), typeof(OptionCheckBox), new PropertyMetadata(false));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty Text2Property = DependencyProperty.Register("Text2", typeof(string), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty TextPaddingProperty = DependencyProperty.Register("TextPadding", typeof(Thickness), typeof(OptionCheckBox), new PropertyMetadata(new Thickness(8.0, 0.0, 4.0, 0.0)));

	public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(OptionCheckBox), new PropertyMetadata(new Thickness(16.0, 0.0, 0.0, 0.0)));

	public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty IconFillProperty = DependencyProperty.Register("IconFill", typeof(Brush), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty CanMultiSelectedProperty = DependencyProperty.Register("CanMultiSelected", typeof(bool), typeof(OptionCheckBox), new PropertyMetadata(false));

	public static readonly DependencyProperty GroupProperty = DependencyProperty.Register("Group", typeof(string), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty MemberIdProperty = DependencyProperty.Register("MemberId", typeof(string), typeof(OptionCheckBox), new PropertyMetadata((object)null));

	public static readonly DependencyProperty IsAIProperty = DependencyProperty.Register("IsAI", typeof(bool), typeof(OptionCheckBox), new PropertyMetadata(false));

	internal OptionCheckBox Root;

	internal EmjTextBlock TitleText;

	private bool _contentLoaded;

	public bool ShowCheckIcon
	{
		get
		{
			return (bool)GetValue(ShowCheckIconProperty);
		}
		set
		{
			SetValue(ShowCheckIconProperty, value);
		}
	}

	public bool HoverSelected
	{
		get
		{
			return (bool)GetValue(HoverSelectedProperty);
		}
		set
		{
			SetValue(HoverSelectedProperty, value);
		}
	}

	public bool Selected
	{
		get
		{
			return (bool)GetValue(SelectedProperty);
		}
		set
		{
			SetValue(SelectedProperty, value);
			OnSelectedChanged(new OptionCheckArgs(Group, MemberId, Selected));
		}
	}

	public string Text
	{
		get
		{
			return (string)GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public string Text2
	{
		get
		{
			return (string)GetValue(Text2Property);
		}
		set
		{
			SetValue(Text2Property, value);
		}
	}

	public Thickness TextPadding
	{
		get
		{
			return (Thickness)GetValue(TextPaddingProperty);
		}
		set
		{
			SetValue(TextPaddingProperty, value);
		}
	}

	public Thickness IconMargin
	{
		get
		{
			return (Thickness)GetValue(IconMarginProperty);
		}
		set
		{
			SetValue(IconMarginProperty, value);
		}
	}

	public Geometry Icon
	{
		get
		{
			return (Geometry)GetValue(IconProperty);
		}
		set
		{
			SetValue(IconProperty, value);
		}
	}

	public Brush IconFill
	{
		get
		{
			return (Brush)GetValue(IconFillProperty);
		}
		set
		{
			SetValue(IconFillProperty, value);
		}
	}

	public bool CanMultiSelected
	{
		get
		{
			return (bool)GetValue(CanMultiSelectedProperty);
		}
		set
		{
			SetValue(CanMultiSelectedProperty, value);
		}
	}

	public string Group
	{
		get
		{
			return (string)GetValue(GroupProperty);
		}
		set
		{
			SetValue(GroupProperty, value);
		}
	}

	public bool IsAI
	{
		get
		{
			return (bool)GetValue(IsAIProperty);
		}
		set
		{
			SetValue(IsAIProperty, value);
		}
	}

	public string MemberId
	{
		get
		{
			if (string.IsNullOrEmpty((string)GetValue(MemberIdProperty)))
			{
				SetValue(MemberIdProperty, Guid.NewGuid().ToString());
			}
			return (string)GetValue(MemberIdProperty);
		}
		set
		{
			SetValue(MemberIdProperty, value);
		}
	}

	public event EventHandler Clicked;

	private static event EventHandler<OptionCheckArgs> SelectedChanged;

	public OptionCheckBox()
	{
		InitializeComponent();
	}

	private void OnSelectedChanged(OptionCheckArgs e)
	{
		if (!CanMultiSelected && Group == e.Group && e.Selected)
		{
			SetValue(SelectedProperty, MemberId == e.UId);
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		this.Clicked?.Invoke(sender, e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/optioncheckbox.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Root = (OptionCheckBox)target;
			Root.MouseLeftButtonUp += OnMouseUp;
			break;
		case 2:
			TitleText = (EmjTextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
