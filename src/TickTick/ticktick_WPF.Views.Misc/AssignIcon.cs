using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AssignIcon : Border
{
	private bool? _empty;

	public AssignIcon()
	{
		base.VerticalAlignment = VerticalAlignment.Center;
		base.HorizontalAlignment = HorizontalAlignment.Center;
	}

	public async void SetAvatar(string avatarUrl)
	{
		bool empty = string.IsNullOrEmpty(avatarUrl);
		if (!_empty.HasValue || _empty != empty)
		{
			if (empty)
			{
				base.Width = 28.0;
				base.Height = 28.0;
				base.BorderBrush = Brushes.Transparent;
				HoverIconButton hoverIconButton = new HoverIconButton
				{
					VerticalAlignment = VerticalAlignment.Center,
					IsImage = false,
					ImageOpacity = 0.6
				};
				hoverIconButton.SetResourceReference(HoverIconButton.IconDataProperty, "Ic.Sharing.AssignToOther");
				Child = hoverIconButton;
				base.BorderThickness = new Thickness(0.0);
				base.Margin = new Thickness(0.0);
				base.Background = Brushes.Transparent;
				base.CornerRadius = new CornerRadius(0.0);
			}
			else
			{
				base.Width = 22.0;
				base.Height = 22.0;
				base.Margin = new Thickness(2.0);
				base.CornerRadius = new CornerRadius(10.0);
				base.BorderThickness = new Thickness(1.0);
				SetResourceReference(Border.BorderBrushProperty, "BaseColorOpacity5");
				Child = null;
			}
		}
		if (!empty)
		{
			BitmapImage imageSource = await AvatarHelper.GetAvatarByUrlAsync(avatarUrl);
			base.Background = new ImageBrush
			{
				ImageSource = imageSource
			};
		}
		_empty = empty;
	}
}
