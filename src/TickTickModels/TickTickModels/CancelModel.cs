namespace TickTickModels;

public class CancelModel
{
	private bool _cancel;

	public void Cancel()
	{
		_cancel = true;
	}

	public bool IsCanceled()
	{
		return _cancel;
	}
}
