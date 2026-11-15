using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class PtfAllViewModel : ProjectItemViewModel
{
	public PtfType Type { get; set; }

	public bool IsTag => Type == PtfType.Tag;

	public bool IsProject => Type == PtfType.Project;

	public bool IsFilter => Type == PtfType.Filter;

	public bool IsSubscribe => Type == PtfType.Subscribe;

	public bool IsTeam
	{
		get
		{
			if (IsProject && !string.IsNullOrEmpty(base.TeamId))
			{
				return base.TeamId != "c1a7e08345e444dea187e21a692f0d7a";
			}
			return false;
		}
	}

	public PtfAllViewModel(PtfType type)
	{
		base.Id = type.ToString();
		base.IsGroupItem = true;
		base.IsPtfItem = true;
		Type = type;
		base.CanDrag = true;
		switch (type)
		{
		case PtfType.Project:
			base.Title = Utils.GetString("lists");
			base.Open = LocalSettings.Settings.AllProjectOpened;
			break;
		case PtfType.Tag:
			base.Title = Utils.GetString("Tags");
			base.Open = LocalSettings.Settings.AllTagOpened;
			break;
		case PtfType.Filter:
			base.Title = Utils.GetString("Filters");
			base.Open = LocalSettings.Settings.AllFilterOpened;
			break;
		case PtfType.Subscribe:
			base.Title = Utils.GetString("SubscribedCalendars");
			base.Open = LocalSettings.Settings.AllSubscribeOpened;
			break;
		}
		base.IsPtfAll = true;
		base.ShowCount = false;
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		bool isTag = IsTag;
		List<ContextAction> list = new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Show),
			new ContextAction(ContextActionKey.Hide)
		};
		if (isTag)
		{
			list.Insert(0, new ContextAction(ContextActionKey.ShowIfNotEmpty)
			{
				Title = Utils.GetString("ShowOmitEmptyTags")
			});
		}
		return list;
	}
}
