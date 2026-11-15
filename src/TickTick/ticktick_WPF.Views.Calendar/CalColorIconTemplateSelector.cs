using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.Entity.Calendar;

namespace ticktick_WPF.Views.Calendar;

public class CalColorIconTemplateSelector : DataTemplateSelector
{
	public DataTemplate FileTemplate { get; set; }

	public DataTemplate PathTemplate { get; set; }

	public DataTemplate ImageSourceTemplate { get; set; }

	public DataTemplate EmojiTemplate { get; set; }

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is CalColorConfigItem calColorConfigItem)
		{
			switch (calColorConfigItem.IconType)
			{
			case IconType.File:
				return FileTemplate;
			case IconType.Path:
				return PathTemplate;
			case IconType.ImageSource:
				return ImageSourceTemplate;
			case IconType.Emoji:
				return EmojiTemplate;
			}
		}
		return base.SelectTemplate(item, container);
	}
}
