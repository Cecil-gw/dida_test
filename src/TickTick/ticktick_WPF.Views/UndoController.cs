namespace ticktick_WPF.Views;

public abstract class UndoController
{
	public abstract string GetTitle();

	public abstract string GetContent();

	public abstract void Undo();

	public abstract void Finished();
}
