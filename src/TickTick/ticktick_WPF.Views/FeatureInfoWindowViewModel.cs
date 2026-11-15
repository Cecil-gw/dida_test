using System;

namespace ticktick_WPF.Views;

public class FeatureInfoWindowViewModel
{
	public string Title { get; set; }

	public string Info1 { get; set; }

	public string Info2 { get; set; }

	public string GifPath { get; set; }

	public string CommandTitle { get; set; }

	public Action Action { get; set; }

	public FeatureInfoWindowViewModel(string title, string info1, string info2, string path, string cTitle, Action act)
	{
		Title = title;
		Info1 = info1;
		Info2 = info2;
		GifPath = path;
		CommandTitle = cTitle;
		Action = act;
	}

	public void DoAction()
	{
		if (Action != null)
		{
			Action();
		}
	}
}
