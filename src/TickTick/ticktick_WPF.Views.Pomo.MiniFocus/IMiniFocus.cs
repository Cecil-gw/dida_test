namespace ticktick_WPF.Views.Pomo.MiniFocus;

public interface IMiniFocus
{
	void Init();

	void SetOpacity(double opacity);

	void SetCountText();

	void OnStatusChanged();

	void OnFocusTypeChanged();

	double GetLeftMargin();

	double GetActualWidth();

	double GetActualHeight();

	void SetMoving(bool b);

	bool CanDragMove();

	bool CanHide();

	void SetHideStyle(bool isHide);

	void OnWindowStartHide();

	double GetExtraWidth();
}
