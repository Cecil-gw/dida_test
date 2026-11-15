using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.AI;

public sealed class AIConversationRenameEditor : EmojiEditor
{
	public static readonly DependencyProperty CaretBrushProperty = DependencyProperty.Register("CaretBrush", typeof(Brush), typeof(AIConversationRenameEditor), new PropertyMetadata(null, delegate(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		((AIConversationRenameEditor)sender).GetEditBox().TextArea.Caret.CaretBrush = (Brush)args.NewValue;
	}));

	internal bool IsTextCompositionActive { get; private set; }

	public Brush CaretBrush
	{
		get
		{
			return (Brush)GetValue(CaretBrushProperty);
		}
		set
		{
			SetValue(CaretBrushProperty, value);
		}
	}

	public AIConversationRenameEditor()
	{
		base.AcceptReturn = false;
		base.WordWrap = false;
		base.MaxLength = 64;
		base.TextVerticalAlignment = VerticalAlignment.Center;
		TextCompositionManager.AddPreviewTextInputStartHandler(this, delegate
		{
			IsTextCompositionActive = true;
		});
		TextCompositionManager.AddPreviewTextInputUpdateHandler(this, delegate
		{
			IsTextCompositionActive = true;
		});
		TextCompositionManager.AddPreviewTextInputHandler(this, delegate
		{
			IsTextCompositionActive = false;
		});
		base.LostKeyboardFocus += delegate
		{
			if (!base.IsKeyboardFocusWithin)
			{
				IsTextCompositionActive = false;
			}
		};
	}

	public void SelectAll()
	{
		GetEditBox().SelectAll();
	}

	internal static AIConversationRenameEditor FromFocusedElement()
	{
		for (DependencyObject dependencyObject = Keyboard.FocusedElement as DependencyObject; dependencyObject != null; dependencyObject = ((dependencyObject is Visual) ? VisualTreeHelper.GetParent(dependencyObject) : LogicalTreeHelper.GetParent(dependencyObject)))
		{
			if (dependencyObject is AIConversationRenameEditor result)
			{
				return result;
			}
		}
		return null;
	}
}
