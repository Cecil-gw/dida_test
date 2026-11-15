using System.Windows.Input;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MarkDown;

public static class EditorCommands
{
	public static readonly ICommand ToggleBoldCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).Bold();
	});

	public static readonly ICommand ToggleStrokeCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).StrokeLine();
	});

	public static readonly ICommand ToggleItalicCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).Italic();
	});

	public static readonly ICommand ToggleHighLightCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).HighlightWithSelectedColor();
	});

	public static readonly ICommand ToggleUnderLineCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).UnderLine();
	});

	public static readonly ICommand ToggleCodeCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).Code();
	});

	public static readonly ICommand InsertLinkCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).ShowInsertLinkForCurrentContext();
	});

	public static readonly ICommand InsertHead1Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(1);
	});

	public static readonly ICommand InsertHead2Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(2);
	});

	public static readonly ICommand InsertHead3Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(3);
	});

	public static readonly ICommand InsertHead4Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(4);
	});

	public static readonly ICommand InsertHead5Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(5);
	});

	public static readonly ICommand InsertHead6Command = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertHeader(6);
	});

	public static readonly ICommand InsertUnOrderList = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertUnOrderList();
	});

	public static readonly ICommand InsertNumberedList = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertNumberedList();
	});

	public static readonly ICommand IndentCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).Indent();
	});

	public static readonly ICommand OutIndentCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).DeIndent();
	});

	public static readonly ICommand QuoteCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).Quote();
	});

	public static readonly ICommand InsertItemCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertCheckItem();
	});

	public static readonly ICommand PastePlainTextCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).PastePlainText();
	});

	public static readonly ICommand SplitLineCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).InsertLine();
	});

	public static readonly ICommand ExitCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).ExitEditor();
	});

	public static readonly ICommand ToggleCheckItemCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).ToggleCheckItem();
	});

	public static readonly ICommand SaveContentCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).TrySaveContent();
	});

	public static readonly ICommand EnterImmersiveCommand = new RelayCommand(delegate(object editor)
	{
		((MarkDownEditor)editor).EnterImmersiveMode();
	});
}
