using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Search;

public class SearchProjectTemplateModel : SearchItemBaseModel
{
	public override bool CanSelect { get; set; } = true;

	public new string Id { get; set; }

	public string Key { get; set; }

	public string Name { get; set; }

	public string Emoji { get; set; }

	public SearchProjectTemplateModel(ProjectTemplate template)
	{
		Id = template.Id;
		Key = template.Keyword;
		Name = template.Name;
	}
}
