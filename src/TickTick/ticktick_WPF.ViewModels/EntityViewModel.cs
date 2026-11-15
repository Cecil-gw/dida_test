namespace ticktick_WPF.ViewModels;

public class EntityViewModel : BaseViewModel
{
	public string Title { get; set; }

	public string Key { get; set; }

	public EntityViewModel()
	{
	}

	public EntityViewModel(string title, string key)
	{
		Title = title;
		Key = key;
	}
}
