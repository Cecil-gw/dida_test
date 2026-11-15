using System.Collections.ObjectModel;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public class SplashInputItems : BaseInputItems<string>
{
	private readonly bool _withPriority;

	private readonly bool _withAttachment;

	private readonly bool _withTemplate;

	private readonly bool _isNote;

	private readonly bool _isCalendar;

	private readonly bool _isNoteProject;

	private readonly bool _canConvertToEvent;

	public bool WithPriority => _withPriority;

	public bool WithAttachment => _withAttachment;

	public bool WithTemplate => _withTemplate;

	public bool IsNote => _isNote;

	public bool IsCalendar => _isCalendar;

	public bool IsNoteProject => _isNoteProject;

	public bool CanConvertToEvent => _canConvertToEvent;

	public SplashInputItems(bool withPriority = true, bool withAttachment = true, bool withTemplate = true, bool isNote = false, bool isCalendar = false, bool isNoteProject = false, bool canConvertToEvent = false)
	{
		_withPriority = withPriority;
		_withAttachment = withAttachment;
		_withTemplate = withTemplate;
		_isNote = isNote;
		_isCalendar = isCalendar;
		_isNoteProject = isNoteProject;
		_canConvertToEvent = canConvertToEvent;
		LoadData();
	}

	protected override ObservableCollection<InputItemViewModel<string>> InitData()
	{
		ObservableCollection<InputItemViewModel<string>> observableCollection = new ObservableCollection<InputItemViewModel<string>>
		{
			new InputItemViewModel<string>(Utils.GetString("date"), "date", "*", needPy: true, Utils.GetGeometry("Ic.TaskOM.DateRemind")),
			new InputItemViewModel<string>(Utils.GetString("List"), "list", "~", needPy: true, Utils.GetGeometry("Ic.ProjectList.MoveTo"))
		};
		if (!_isCalendar)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("Tags"), "tag", "#", needPy: true, Utils.GetGeometry("Ic.ProjectList.Tag")));
		}
		if (_withAttachment)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("Attachment"), "attachment", "attachment", needPy: true, Utils.GetGeometry("Ic.MarkdownEditor.Attachment")));
		}
		if (_withPriority)
		{
			observableCollection.Insert(1, new InputItemViewModel<string>(Utils.GetString("priority"), "priority", "!", needPy: true, Utils.GetGeometry("Ic.ProjectList.Priority")));
		}
		if (_withTemplate)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("AddFromTemplate"), "add_template", "template", needPy: true, Utils.GetGeometry("Ic.TaskOM.Template")));
		}
		QuickAddConvertOptions quickAddConvertOptions = QuickAddConvertOptionPolicy.Create(_isNote, _isCalendar, _isNoteProject, _canConvertToEvent);
		if (quickAddConvertOptions.ShowConvertToTask)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("ConvertToTask"), "convert_to_task", "switch_task", needPy: true, Utils.GetGeometry("Ic.TaskOM.ConvertToTask")));
		}
		if (quickAddConvertOptions.ShowConvertToNote)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("ConvertToNote"), "convert_to_note", "switch_note", needPy: true, Utils.GetGeometry("Ic.TaskOM.ConvertToNote")));
		}
		if (quickAddConvertOptions.ShowConvertToEvent)
		{
			observableCollection.Add(new InputItemViewModel<string>(Utils.GetString("ConvertToEvent"), "convert_to_event", "switch_event", needPy: true, Utils.GetGeometry("Ic.TaskOM.ConvertToSchedule")));
		}
		return observableCollection;
	}
}
