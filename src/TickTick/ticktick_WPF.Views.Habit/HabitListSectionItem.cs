using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.Habit;

public class HabitListSectionItem : EditableSectionControl
{
	private bool _startDrag;

	private HabitListControl _parent;

	private HabitSectionListViewModel Model => base.DataContext as HabitSectionListViewModel;

	public HabitListSectionItem()
		: base(inherit: true)
	{
		InitializeComponent();
		EditBox.PreviewKeyDown += OnKeyDown;
		Container.MouseLeftButtonUp += OnSectionClick;
		base.DataContextChanged += OnDataBind;
		base.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
	}

	private async void Rename()
	{
		MorePopup.IsOpen = false;
		SetEditing(edit: true);
		EditBox.Text = Model.Title;
		await Task.Delay(50);
		EditBox.Focus();
		EditBox.SelectAll();
		EditBox.LostFocus -= OnLostFocus;
		EditBox.LostFocus += OnLostFocus;
	}

	private async void OnLostFocus(object sender, RoutedEventArgs e)
	{
		if (base.Editing)
		{
			SaveRename();
			ErrorPopup.IsOpen = false;
		}
	}

	private void Delete()
	{
		bool flag = true;
		if (Model.Num > 0)
		{
			flag = new CustomerDialog(Utils.GetString("Delete") + " " + Model.Title, Utils.GetString("HabitSectionDeleteInfo"), Utils.GetString("Delete"), Utils.GetString("Cancel")).ShowDialog() == true;
		}
		if (flag)
		{
			HabitService.DeleteSectionById(Model.Id);
		}
		MorePopup.IsOpen = false;
	}

	protected override void SetOptionAction(CustomSectionOption option)
	{
		option.SetAction(null, null, Rename, Delete);
	}

	protected override async void AddButtonClick()
	{
		if (await HabitUtils.CheckHabitLimit())
		{
			AddOrEditHabitDialog addOrEditHabitDialog = new AddOrEditHabitDialog(Model.Id);
			addOrEditHabitDialog.Owner = App.Window;
			addOrEditHabitDialog.ShowDialog();
		}
	}

	private async void SaveRename()
	{
		HabitSectionListViewModel model = Model;
		if (model?.Section == null)
		{
			return;
		}
		string columnName = EditBox.Text.Trim();
		SetEditing(edit: false);
		if (!(columnName == model.Title))
		{
			var (flag, message) = await HabitService.CheckSectionName(columnName);
			if (flag)
			{
				model.Title = columnName;
				model.Section.Name = columnName;
				HabitService.UpdateSection(model.Section);
			}
			else
			{
				Utils.Toast(message);
			}
		}
	}

	private void CancelRename()
	{
		SetEditing(edit: false);
	}

	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			CancelRename();
			break;
		case Key.Return:
			SaveRename();
			break;
		}
	}

	private void OnSectionClick(object sender, MouseButtonEventArgs e)
	{
		if (!base.InOperate && Model != null)
		{
			Model.IsOpen = !Model.IsOpen;
			HabitService.UpdateSection(Model.Section);
			GetParent()?.OnSectionOpenChanged(Model);
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (Model != null)
		{
			MoreButton.Visibility = (Model.IsOther ? Visibility.Collapsed : Visibility.Visible);
			base.Margin = new Thickness(0.0, Model.IsFirstOne ? 2 : 6, 0.0, 0.0);
		}
	}

	protected override void StartDrag(MouseEventArgs e)
	{
		GetParent()?.TryDragItem(this, e);
	}

	private HabitListControl GetParent()
	{
		_parent = _parent ?? Utils.FindParent<HabitListControl>(this);
		return _parent;
	}
}
