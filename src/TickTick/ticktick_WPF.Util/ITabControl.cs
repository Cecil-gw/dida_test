namespace ticktick_WPF.Util;

public interface ITabControl
{
	bool HandleTab(bool shift);

	bool HandleEnter();

	bool HandleEsc();

	bool UpDownSelect(bool isUp);

	bool LeftRightSelect(bool isLeft);
}
