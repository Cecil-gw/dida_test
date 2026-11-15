namespace ticktick_WPF.Views.CustomControl;

public class DataComboBoxViewModel : SimpleComboBoxViewModel
{
	public object Data { get; set; }

	public DataComboBoxViewModel(object data, string text)
		: base(text)
	{
		Data = data;
	}
}
