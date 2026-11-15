using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Print;

public class TitleRightColumnView : UserControl, IComponentConnector
{
	public double NeedWidth;

	internal TextBlock FirstTag;

	internal TextBlock DateText;

	internal Rectangle AvatarImage;

	private bool _contentLoaded;

	public TitleRightColumnView()
	{
		InitializeComponent();
	}

	public TitleRightColumnView(RightColumnViewModel model)
	{
		base.DataContext = model;
		InitializeComponent();
		if (!string.IsNullOrEmpty(model.AvatarUrl.Trim()))
		{
			AvatarImage.Fill = new ImageBrush(AvatarHelper.GetAvatarByUrl(model.AvatarUrl));
		}
		if (model.StartDate.HasValue && !Utils.IsEmptyDate(model.StartDate.Value))
		{
			DateText.Text = DateUtils.FormatListDateString(model.StartDate.Value, model.DueDate, model.IsAllDay);
		}
		if (!string.IsNullOrEmpty(model.FirstTag))
		{
			NeedWidth += Math.Min(MeasureString(model.FirstTag).Width, 70.0) + 13.0;
		}
		if (!string.IsNullOrEmpty(model.SecondTag))
		{
			NeedWidth += Math.Min(MeasureString(model.SecondTag).Width, 70.0) + 13.0;
		}
		if (!string.IsNullOrEmpty(model.MoreTag))
		{
			NeedWidth += MeasureString(model.MoreTag).Width + 13.0;
		}
		if (!string.IsNullOrEmpty(model.ProjectName))
		{
			NeedWidth += Math.Min(MeasureString(model.ProjectName).Width, 70.0) + 10.0;
		}
		if (model.ShowAttachment)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowComment)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowDescription)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowReminder)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowLocation)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowProgress)
		{
			NeedWidth += 20.0;
		}
		if (model.ShowRepeat)
		{
			NeedWidth += 20.0;
		}
		if (!string.IsNullOrEmpty(DateText.Text))
		{
			NeedWidth += MeasureString(DateText.Text).Width + 12.0;
		}
		if (!string.IsNullOrEmpty(model.AvatarUrl) && model.AvatarUrl != "-1")
		{
			NeedWidth += 35.0;
		}
	}

	private Size MeasureString(string candidate)
	{
		return Utils.MeasureString(candidate, new Typeface(FirstTag.FontFamily, FirstTag.FontStyle, FirstTag.FontWeight, FirstTag.FontStretch), FirstTag.FontSize);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/titlerightcolumnview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
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
			FirstTag = (TextBlock)target;
			break;
		case 2:
			DateText = (TextBlock)target;
			break;
		case 3:
			AvatarImage = (Rectangle)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
