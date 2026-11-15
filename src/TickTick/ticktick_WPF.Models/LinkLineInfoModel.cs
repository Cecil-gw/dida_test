using System;
using ICSharpCode.AvalonEdit.Rendering;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class LinkLineInfoModel
{
	private int _startIndex = -1;

	public int StartIndex
	{
		get
		{
			if (StartIndexProvider == null)
			{
				return _startIndex;
			}
			return StartIndexProvider() ?? (-1);
		}
		set
		{
			_startIndex = value;
		}
	}

	[JsonIgnore]
	public Func<int?> StartIndexProvider { get; set; }

	public string Url { get; set; }

	public string Text { get; set; }

	public VisualLine VisualLine { get; set; }

	public LinkInfoSource Source { get; set; }

	public bool IsNew { get; set; } = true;

	public bool TryResolveStartIndex(out int startIndex)
	{
		if (StartIndexProvider == null)
		{
			startIndex = _startIndex;
			return true;
		}
		int? num = StartIndexProvider();
		startIndex = num ?? (-1);
		if (num.HasValue)
		{
			return startIndex >= 0;
		}
		return false;
	}
}
