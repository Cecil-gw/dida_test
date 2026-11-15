using System.Collections.Generic;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class ContextAction : BaseViewModel
{
	public ContextActionKey ActionKey { get; set; }

	public string Title { get; set; }

	public string Key { get; set; }

	public List<ContextAction> SubActions { get; set; }

	public bool Selected { get; set; }

	public ContextAction()
	{
	}

	public ContextAction(ContextActionKey actionKey)
	{
		ActionKey = actionKey;
		switch (actionKey)
		{
		case ContextActionKey.OpenNewWindow:
			Title = Utils.GetString("OpenNewWindow");
			break;
		case ContextActionKey.CloseWindow:
			Title = Utils.GetString("CloseWindow");
			break;
		default:
			Title = Utils.GetString(actionKey.ToString());
			break;
		}
	}
}
