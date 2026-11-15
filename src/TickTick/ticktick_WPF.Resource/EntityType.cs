using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Resource;

public static class EntityType
{
	public const string Task = "task";

	public const string Subtask = "item";

	public const string CalendarEvent = "event";

	public const string Derivative = "derivative";

	public const string Agenda = "agenda";

	public const string Course = "course";

	private static readonly Dictionary<string, int> KeyDict = new Dictionary<string, int>
	{
		{ "task", 1 },
		{ "item", 2 },
		{ "event", 3 },
		{ "derivative", 4 },
		{ "agenda", 1 },
		{ "course", 10 }
	};

	public static int GetEntityTypeNum(string type)
	{
		KeyDict.TryGetValue(type, out var value);
		return value;
	}

	public static string GetEntityType(int order)
	{
		return KeyDict.FirstOrDefault((KeyValuePair<string, int> x) => x.Value == order).Key;
	}

	public static int GetEntityTypeNum(DisplayType type)
	{
		return GetEntityTypeNum(GetEntityType(type));
	}

	public static string GetEntityType(DisplayType type)
	{
		return type switch
		{
			DisplayType.Task => "task", 
			DisplayType.CheckItem => "item", 
			DisplayType.Agenda => "agenda", 
			DisplayType.Derivative => "derivative", 
			DisplayType.Event => "event", 
			DisplayType.Course => "course", 
			_ => "task", 
		};
	}
}
