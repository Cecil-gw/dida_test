using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class TaskSearchModel
{
	public string Id => SourceModel?.Id;

	public string Title => SourceModel?.Title;

	public string Content { get; set; }

	public string Desc => SourceModel?.Desc;

	public string Attachment { get; set; }

	public string CheckItems { get; set; } = "";

	public string Comment { get; set; } = "";

	public TaskBaseViewModel SourceModel { get; set; }

	public bool Contains(string searchKey)
	{
		return GetText().Contains(searchKey);
	}

	public string GetText()
	{
		return (string.Empty + Title + "\r\n" + Content + "\r\n" + Desc + "\r\n" + Attachment + "\r\n" + CheckItems + "\r\n" + Comment).ToLowerInvariant();
	}
}
