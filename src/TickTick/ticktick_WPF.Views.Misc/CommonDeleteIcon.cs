using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views.Misc;

public class CommonDeleteIcon : UserControl, IComponentConnector
{
	public static readonly DependencyProperty HoverSelectedProperty = DependencyProperty.Register("HoverSelected", typeof(bool), typeof(CommonDeleteIcon), new PropertyMetadata(false));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(CommonDeleteIcon), new PropertyMetadata((object)null));

	public static readonly DependencyProperty TextPaddingProperty = DependencyProperty.Register("TextPadding", typeof(Thickness), typeof(CommonDeleteIcon), new PropertyMetadata(new Thickness(8.0, 0.0, 4.0, 0.0)));

	public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(CommonDeleteIcon), new PropertyMetadata(new Thickness(16.0, 0.0, 0.0, 0.0)));

	public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(CommonDeleteIcon), new PropertyMetadata((object)null));

	public static readonly DependencyProperty IconFillProperty = DependencyProperty.Register("IconFill", typeof(Brush), typeof(CommonDeleteIcon), new PropertyMetadata((object)null));

	public static readonly DependencyProperty GroupProperty = DependencyProperty.Register("Group", typeof(string), typeof(CommonDeleteIcon), new PropertyMetadata((object)null));

	public static readonly DependencyProperty MemberIdProperty = DependencyProperty.Register("MemberId", typeof(string), typeof(CommonDeleteIcon), new PropertyMetadata((object)null));

	internal CommonDeleteIcon Root;

	internal TextBlock TitleText;

	private bool _contentLoaded;

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

	public CommonDeleteIcon()
	{
		InitializeComponent();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/commondeleteicon.xaml", UriKind.Relative);
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
			Root = (CommonDeleteIcon)target;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
