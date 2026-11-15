using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

namespace ticktick_WPF.Views.CountDown.Infrastructure;

public class UpdateTemplateBehavior : Behavior<ContentControl>
{
	public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(UpdateTemplateBehavior), new FrameworkPropertyMetadata(null, OnContentChanged));

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(UpdateTemplateBehavior), new FrameworkPropertyMetadata(null, OnValueChanged));

	public object Content
	{
		get
		{
			return GetValue(ContentProperty);
		}
		set
		{
			SetValue(ContentProperty, value);
		}
	}

	public object Value
	{
		get
		{
			return GetValue(ValueProperty);
		}
		set
		{
			SetValue(ValueProperty, value);
		}
	}

	private static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
	}

	private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is UpdateTemplateBehavior updateTemplateBehavior && e.OldValue != null)
		{
			updateTemplateBehavior.Update();
		}
	}

	private void Update()
	{
		if (Content != null)
		{
			BindingOperations.ClearBinding(base.AssociatedObject, ContentPresenter.ContentProperty);
			base.AssociatedObject.Content = null;
			BindingOperations.SetBinding(base.AssociatedObject, ContentPresenter.ContentProperty, new Binding("Content")
			{
				Source = this
			});
		}
	}
}
