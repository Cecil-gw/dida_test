using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

public class DeleteLineColorizer : DocumentColorizingTransformer
{
	public static TextDecorationCollection Strikethrough;

	public static TextDecorationCollection HintStrikethrough;

	private static readonly TextDecoration TextDecoration;

	private static readonly TextDecoration HintTextDecoration;

	static DeleteLineColorizer()
	{
		TextDecoration = new TextDecoration
		{
			Location = TextDecorationLocation.Strikethrough
		};
		Pen pen = new Pen(ThemeUtil.GetColor("BaseColorOpacity20_40"), 2.0);
		TextDecoration.Pen = pen;
		Strikethrough = new TextDecorationCollection { TextDecoration };
		HintTextDecoration = new TextDecoration
		{
			Location = TextDecorationLocation.Strikethrough
		};
		Pen pen2 = new Pen(ThemeUtil.GetColor("BaseColorOpacity10_20"), 2.0);
		HintTextDecoration.Pen = pen2;
		HintStrikethrough = new TextDecorationCollection { HintTextDecoration };
	}

	public static void OnThemeChanged()
	{
		TextDecoration.Pen = new Pen(ThemeUtil.GetColor("BaseColorOpacity20_40"), 2.0);
		HintTextDecoration.Pen = new Pen(ThemeUtil.GetColor("BaseColorOpacity10_20"), 2.0);
	}

	protected override void ColorizeLine(DocumentLine line)
	{
		foreach (VisualLineElement currentElement in base.CurrentElements)
		{
			currentElement.TextRunProperties.SetTextDecorations(Strikethrough);
		}
	}
}
