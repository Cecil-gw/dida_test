using System.Windows.Controls;
using System.Windows.Input;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class HoverDeleteButton : HoverIconButton
{
	public HoverDeleteButton()
	{
		BackBorder.SetResourceReference(Panel.BackgroundProperty, "DeleteHoverColor");
		base.IsImage = false;
		base.IconData = Utils.GetGeometry("Ic.ProjectList.Delete");
		base.MouseEnter += OnMouseEnter;
		base.MouseLeave += OnMouseLeave;
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		SetIconColor("BaseColorOpacity100");
		base.ImageOpacity = 1.0;
	}

	private void OnMouseEnter(object sender, MouseEventArgs e)
	{
		SetIconColor("TickRedColor");
		base.ImageOpacity = 1.0;
	}
}
