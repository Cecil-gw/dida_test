using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class CardBase : ConditionCard
{
	protected bool IsSave;

	protected CardBase(CardViewModel viewModel)
		: base(viewModel)
	{
		ViewModel = viewModel;
		ViewModel.Content = viewModel.ToCardText();
		Init(viewModel);
		EditDialog = GenerateEditDialog();
		Popup.Child = EditDialog;
		Popup.Closed += Popup_Closed;
		if (ViewModel.ShowDropdown)
		{
			Popup.IsOpen = true;
		}
	}

	protected virtual void Init(CardViewModel model)
	{
	}

	protected override void OnCardClick()
	{
		if (!Popup.IsOpen)
		{
			Popup.IsOpen = true;
		}
	}

	private void Popup_Closed(object sender, EventArgs e)
	{
		PopupClosed();
	}

	private void PopupClosed()
	{
		if (!EditDialog.IsSave)
		{
			EditDialog.Restore();
		}
		EditDialog.IsSave = false;
	}

	protected override void OnDeleteClick()
	{
		ReturnToInitFilter();
	}

	private void ReturnToInitFilter()
	{
		ViewModel.ShowDropdown = false;
		FireEvent(new ChangeData
		{
			Type = ChangeType.Filter,
			From = ViewModel,
			To = new CardViewModel
			{
				Type = CardType.InitFilter
			}
		});
	}

	protected void CardPopupClosed(object sender, EventArgs e)
	{
		if (EditDialog == null)
		{
			return;
		}
		if (!IsSave)
		{
			EditDialog.Restore();
			if (!CheckFilterValid())
			{
				ReturnToInitFilter();
			}
		}
		IsSave = false;
	}

	protected virtual bool CheckFilterValid()
	{
		return true;
	}
}
