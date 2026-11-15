using System.Collections.Generic;

namespace ticktick_WPF.Views.MarkDown;

public interface IEmojiRender
{
	Dictionary<int, string> GetEmojiDict();
}
