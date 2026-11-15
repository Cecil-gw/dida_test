using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class SortProjectData : ProjectData
{
	public SortOption SortOption
	{
		get
		{
			return base.ProjectIdentity.SortOption;
		}
		set
		{
			base.ProjectIdentity.SortOption = value;
		}
	}

	public bool ShowLoadMore { get; set; }

	public static async Task<bool> IsSortOrderEmpty(SortOption option, string id)
	{
		return true;
	}

	protected static void SaveSpecialProjectSortType(string index, SortOption sortOption)
	{
		LocalSettings.Settings[index] = sortOption.ToSmartString();
	}

	public virtual string GetTitle()
	{
		return string.Empty;
	}

	public virtual void SaveSortOption(SortOption sortOption)
	{
	}

	public virtual string GetEmptyTitle(bool allCompleted)
	{
		return base.EmptyTitle;
	}

	public virtual string GetEmptyContent(bool allCompleted)
	{
		return base.EmptyContent;
	}

	public virtual bool ShowFilterExpired()
	{
		return false;
	}

	public virtual bool ShowCalendarExpired()
	{
		return false;
	}

	public virtual EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return null;
	}
}
