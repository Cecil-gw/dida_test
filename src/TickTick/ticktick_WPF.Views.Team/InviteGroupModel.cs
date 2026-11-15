using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Team;

public class InviteGroupModel : BaseViewModel
{
	private string _id;

	private string _projectName;

	private List<InviteUserModel> _users;

	private bool _opened;

	private int _selectNum;

	private bool _selectAll;

	public string Id { get; set; }

	public string ProjectName { get; set; }

	public List<InviteUserModel> Users
	{
		get
		{
			return _users;
		}
		set
		{
			_users = value;
			OnPropertyChanged("Users");
		}
	}

	public bool Opened
	{
		get
		{
			return _opened;
		}
		set
		{
			_opened = value;
			OnPropertyChanged("Opened");
		}
	}

	public int SelectNum
	{
		get
		{
			return _selectNum;
		}
		set
		{
			_selectNum = value;
			SelectAll = _selectNum == _users.Count;
		}
	}

	public bool SelectAll
	{
		get
		{
			return _selectAll;
		}
		set
		{
			_selectAll = value;
			OnPropertyChanged("SelectAll");
		}
	}

	public static async Task<InviteGroupModel> Build(ProjectUsersMode mode, List<string> shareUserCodeList)
	{
		string currentUser = LocalSettings.Settings.LoginUserId;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == mode.projectId);
		List<InviteUserModel> list = InviteUserModel.Build(mode.shareUsers.Where((ShareUserModel model) => model.userId.ToString() != currentUser).ToList(), shareUserCodeList);
		if (list.Count == 0)
		{
			return null;
		}
		return new InviteGroupModel
		{
			Id = mode.projectId,
			ProjectName = projectModel?.name,
			Opened = false,
			Users = list
		};
	}
}
