using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.MarkDown;

public interface ILinkTextEditor
{
	Dictionary<int, LinkInfo> GetLinkNameDict();

	Dictionary<int, LinkInfo> GetLinkUrlDict();

	void NavigateTask(string projectTaskProjectId, string projectTaskTaskId);

	void ShowInsertLink(LinkLineInfoModel model);

	void UnRegisterCaretChanged();

	void RegisterCaretChanged();

	TextEditor GetEditBox();

	Brush GetHighLightColor();

	Brush GetBracketColor();
}
