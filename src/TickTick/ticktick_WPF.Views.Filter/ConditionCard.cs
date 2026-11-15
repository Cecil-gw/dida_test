using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.Filter;

public class ConditionCard : UserControl, IComponentConnector
{
	public delegate List<CondType> GetDropdownListHandler();

	protected ConditionEditDialog EditDialog;

	protected CardViewModel ViewModel;

	internal ConditionCard Card;

	internal Grid NormalGrid;

	internal Border DeleteBorder;

	internal Path DeletePath;

	internal EscPopup Popup;

	internal Grid KeywordsGrid;

	internal TextBox KeywordsText;

	private bool _contentLoaded;

	public event EventHandler<ChangeData> OnConditionChanged;

	public event GetDropdownListHandler GetDropDownList;

	public ConditionCard(CardViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;
		base.DataContext = ViewModel;
		if (viewModel is KeywordsViewModel keywordsViewModel)
		{
			KeywordsGrid.Visibility = Visibility.Visible;
			NormalGrid.Visibility = Visibility.Collapsed;
			KeywordsText.Text = keywordsViewModel.Keyword;
			if (keywordsViewModel.IsNewAdd)
			{
				DelayFocus();
			}
		}
		DeleteBorder.Visibility = ((ViewModel.Type != CardType.Filter && ViewModel.Type != CardType.LogicAnd && ViewModel.Type != CardType.LogicOr) ? Visibility.Hidden : Visibility.Visible);
	}

	private async void DelayFocus()
	{
		await Task.Delay(100);
		KeywordsText.Focus();
	}

	private void OnClick(object sender, MouseButtonEventArgs e)
	{
		OnCardClick();
	}

	protected void ShowSelectLogicDialog()
	{
		List<ListItemData> listItems = new List<ListItemData>
		{
			new ListItemData("and", Utils.GetString("and")),
			new ListItemData("or", Utils.GetString("or"))
		};
		ShowDropdownDialog(listItems, delegate(object x, ListItemData item)
		{
			CardViewModel cardViewModel = new CardViewModel
			{
				Index = ViewModel.Index
			};
			if (item.Key.ToString() == "and")
			{
				cardViewModel.Type = CardType.LogicAnd;
			}
			else if (item.Key.ToString() == "or")
			{
				cardViewModel.Type = CardType.LogicOr;
			}
			FireEvent(new ChangeData
			{
				Type = ChangeType.Logic,
				From = ViewModel,
				To = cardViewModel
			});
			Popup.IsOpen = false;
		});
	}

	protected void ShowDropdownDialog(List<ListItemData> listItems, EventHandler<ListItemData> handler)
	{
		DropdownDialog dropdownDialog = new DropdownDialog
		{
			ItemsSource = listItems
		};
		dropdownDialog.OnItemSelected += handler;
		Popup.Child = dropdownDialog;
		Popup.IsOpen = true;
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		OnDeleteClick();
		e.Handled = true;
	}

	protected virtual void OnCardClick()
	{
	}

	protected virtual void OnDeleteClick()
	{
	}

	protected virtual ConditionEditDialog GenerateEditDialog()
	{
		return null;
	}

	protected void FireEvent(ChangeData data)
	{
		this.OnConditionChanged?.Invoke(this, data);
	}

	protected List<CondType> GetLeftCondTypes()
	{
		return this.GetDropDownList?.Invoke();
	}

	private void OnKeyWordsTextChanged(object sender, TextChangedEventArgs e)
	{
		if (ViewModel is KeywordsViewModel keywordsViewModel)
		{
			keywordsViewModel.Keyword = KeywordsText.Text.Trim();
		}
	}

	private void OnKeyWordsGridClick(object sender, MouseButtonEventArgs e)
	{
		if (!KeywordsText.IsFocused)
		{
			KeywordsText.Focus();
			KeywordsText.Select(KeywordsText.Text.Length, 0);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/filterconditioncard.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Card = (ConditionCard)target;
			break;
		case 2:
			NormalGrid = (Grid)target;
			break;
		case 3:
			((Grid)target).MouseLeftButtonUp += OnClick;
			break;
		case 4:
			DeleteBorder = (Border)target;
			DeleteBorder.MouseLeftButtonUp += OnDeleteClick;
			break;
		case 5:
			DeletePath = (Path)target;
			break;
		case 6:
			Popup = (EscPopup)target;
			break;
		case 7:
			KeywordsGrid = (Grid)target;
			KeywordsGrid.MouseLeftButtonUp += OnKeyWordsGridClick;
			break;
		case 8:
			KeywordsText = (TextBox)target;
			KeywordsText.TextChanged += OnKeyWordsTextChanged;
			break;
		case 9:
			((Border)target).MouseLeftButtonUp += OnDeleteClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
