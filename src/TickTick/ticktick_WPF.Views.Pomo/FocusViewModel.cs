using System.Threading.Tasks;
using System.Windows.Media;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo;

public class FocusViewModel : BaseViewModel
{
	private string _title;

	private string _focusId;

	private ProjectIdentity _identity;

	public string ObjId;

	public bool IsHabit;

	public bool NoTask => string.IsNullOrEmpty(_focusId);

	public ProjectIdentity Identity
	{
		get
		{
			return _identity ?? ProjectIdentity.CreateSmartIdentity("_special_id_today");
		}
		set
		{
			_identity = value;
			OnPropertyChanged("ProjectName");
			OnPropertyChanged("Icon");
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public string FocusId => _focusId;

	public string ProjectName => Identity.GetDisplayTitle();

	public Geometry Icon => Identity.GetProjectIcon();

	public string TimerId { get; set; }

	public int Type
	{
		get
		{
			if (!string.IsNullOrEmpty(TimerId))
			{
				return 2;
			}
			if (IsHabit)
			{
				return 1;
			}
			return 0;
		}
	}

	public FocusViewModel()
	{
	}

	public FocusViewModel(string focusId)
	{
		_focusId = focusId;
	}

	public async Task SetupTask(string id, int type, string defaultTitle = null)
	{
		if (!(_focusId == id))
		{
			await SetFocusIdAndTitle(id, type, defaultTitle ?? "");
			OnPropertyChanged("NoTask");
		}
	}

	private async Task SetFocusIdAndTitle(string id, int type, string defaultTitle)
	{
		if (string.IsNullOrEmpty(id))
		{
			ObjId = "";
			TimerId = "";
			Title = "";
			_focusId = "";
			IsHabit = false;
			return;
		}
		TimerModel timer = null;
		HabitModel habit = null;
		ObjId = id;
		_focusId = id;
		switch (type)
		{
		case 0:
		{
			timer = await TimerDao.GetTimerByObjId(id);
			TaskModel taskModel = await TaskDao.GetThinTaskById(id);
			Title = ((taskModel != null) ? taskModel.title : (timer?.Name ?? defaultTitle));
			break;
		}
		case 1:
			timer = await TimerDao.GetTimerByObjId(id);
			habit = await HabitDao.GetHabitById(id);
			Title = ((habit != null) ? habit.Name : (timer?.Name ?? defaultTitle));
			break;
		case 2:
			timer = await TimerDao.GetTimerById(id);
			if (timer != null)
			{
				TimerId = id;
				_focusId = id;
				string objType = timer.ObjType;
				if (!(objType == "task"))
				{
					if (objType == "habit")
					{
						habit = await HabitDao.GetHabitById(timer.ObjId);
						Title = ((habit != null) ? habit.Name : timer.Name);
					}
					else
					{
						Title = timer.Name;
					}
				}
				else
				{
					TaskModel taskModel = await TaskDao.GetThinTaskById(timer.ObjId);
					Title = ((taskModel != null) ? taskModel.title : timer.Name);
				}
			}
			else
			{
				Title = defaultTitle;
			}
			break;
		}
		TimerId = timer?.Id;
		IsHabit = habit != null;
		if (timer != null)
		{
			_focusId = timer.Id;
			ObjId = timer.ObjId;
		}
		else
		{
			_focusId = id;
			ObjId = id;
		}
	}

	public void SetIdentity(ProjectIdentity identity)
	{
		if (identity != null)
		{
			Identity = identity;
		}
	}

	public void SetFocusId(string id)
	{
		_focusId = id;
	}
}
