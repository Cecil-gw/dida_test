using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.MarkDown;

public sealed class LinkUrlElement : FormattedTextElement
{
	private readonly ILinkTextEditor _editor;

	private readonly bool _isTaskLink;

	private readonly string _name;

	private readonly string _url;

	private readonly TextAnchor _offsetAnchor;

	private bool _dark;

	private LinkLineInfoModel linkLineInfo { get; set; }

	public LinkUrlElement(int offset, int length, string url, string title, VisualLine parentVisualLine, ILinkTextEditor editor, TextLine text, bool dark)
		: base(text, length)
	{
		linkLineInfo = new LinkLineInfoModel
		{
			Text = title,
			Url = url,
			IsNew = false,
			Source = LinkInfoSource.TextLineButton,
			StartIndex = offset,
			VisualLine = parentVisualLine
		};
		_dark = dark;
		_url = url;
		_name = title;
		_editor = editor;
		_isTaskLink = TaskUtils.ParseTaskUrlWithoutTitle(_url) != null;
		_offsetAnchor = parentVisualLine?.Document?.CreateAnchor(offset);
		if (_offsetAnchor != null)
		{
			_offsetAnchor.MovementType = AnchorMovementType.AfterInsertion;
		}
		linkLineInfo.StartIndexProvider = () => (_offsetAnchor != null && !_offsetAnchor.IsDeleted) ? new int?(_offsetAnchor.Offset) : ((int?)null);
	}

	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		return new LinkUrlTextRun(this, _isTaskLink, base.TextRunProperties, _dark);
	}

	private LinkLineInfoModel GetCurrentLinkLineInfo()
	{
		return linkLineInfo;
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			e.Handled = true;
			_editor.ShowInsertLink(GetCurrentLinkLineInfo());
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		if (!e.Handled)
		{
			base.OnMouseLeave(e);
			if (_editor is MarkDownEditor markDownEditor)
			{
				markDownEditor.HideLinkMenuPopup();
			}
		}
	}

	protected override void OnQueryCursor(QueryCursorEventArgs e)
	{
		e.Handled = true;
		e.Cursor = Cursors.Hand;
		base.OnQueryCursor(e);
		if (_editor is MarkDownEditor markDownEditor)
		{
			markDownEditor.TryShowLinkMenuPopup(GetCurrentLinkLineInfo());
		}
	}
}
