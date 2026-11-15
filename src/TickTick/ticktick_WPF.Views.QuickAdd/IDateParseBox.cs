namespace ticktick_WPF.Views.QuickAdd;

public interface IDateParseBox
{
	bool CanParseDate { get; set; }

	void ForceRender();

	void RemoveTokenText(ref string text);

	double GetFontSize();

	string GetColor();
}
