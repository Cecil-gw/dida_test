using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.ViewModels;

public class InboxProjectViewModel : SmartProjectViewModel, IDroppable
{
	public new string ProjectId => Utils.GetInboxId();

	public InboxProjectViewModel(SmartProject project)
		: base(project)
	{
		base.CanDrop = true;
		base.Id = ProjectId;
		ProjectModel projectModel = CacheManager.GetProjects().ToList().FirstOrDefault((ProjectModel p) => p.Isinbox);
		if (projectModel != null)
		{
			base.Color = projectModel.color;
		}
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		string saveIdentity = GetSaveIdentity();
		bool flag = !string.IsNullOrEmpty(saveIdentity) && TaskListWindow.Windows.ContainsKey(saveIdentity);
		bool flag2 = SmartProjectViewModel.IsProjectAuto("SmartListInbox");
		List<ContextAction> list = new List<ContextAction>();
		list.Add(new ContextAction(ContextActionKey.Show)
		{
			Selected = !flag2
		});
		list.Add(new ContextAction(ContextActionKey.ShowIfNotEmpty)
		{
			Selected = flag2
		});
		list.Add(new ContextAction(ContextActionKey.Hide));
		return new List<ContextAction>
		{
			new ContextAction(ContextActionKey.Edit),
			new ContextAction(ContextActionKey.DisplayOption)
			{
				SubActions = list
			},
			new ContextAction(flag ? ContextActionKey.CloseWindow : ContextActionKey.OpenNewWindow)
		};
	}

	public override async void LoadCount()
	{
		base.Count = await Task.Run(async () => await TaskCountCache.TryGetCount(ProjectIdentity.CreateInboxProject()));
	}

	public override ProjectIdentity GetIdentity()
	{
		return ProjectIdentity.CreateInboxProject();
	}
}
