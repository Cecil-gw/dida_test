using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Filter;

public class TagEditDialog : ConditionEditDialogBase<string>
{
	private List<string> _originalTags;

	private List<string> _selectedTags;

	public event EventHandler<List<string>> OnSelectedTagChanged;

	public TagEditDialog()
		: this(showAll: false)
	{
	}

	private TagEditDialog(bool showAll)
		: this(showAll, new List<string>())
	{
	}

	public TagEditDialog(bool showAll, List<string> selectedTags, LogicType logicType = LogicType.Or)
	{
		InitializeComponent();
		_selectedTags = selectedTags;
		_originalTags = selectedTags.ToList();
		InitData(showAll, selectedTags, logicType);
		base.OnSelectedValueChanged += delegate
		{
			if (!base.ViewModel.IsAllSelected)
			{
				_selectedTags = GetSelectedValues();
				base.ViewModel.WithoutTagsSelected = _selectedTags.Contains("!tag");
				if (_selectedTags.Contains("!tag"))
				{
					_selectedTags.Remove("!tag");
					_selectedTags.Insert(0, "!tag");
				}
				base.ViewModel.WithTagsSelected = _selectedTags.Contains("*withtags");
				if (_selectedTags.Contains("*withtags"))
				{
					_selectedTags.Remove("*withtags");
					_selectedTags.Insert(0, "*withtags");
				}
				base.ViewModel.AllLogicEnabled = !_selectedTags.Contains("!tag");
				this.OnSelectedTagChanged?.Invoke(this, _selectedTags);
			}
			else
			{
				_selectedTags = new List<string>();
				this.OnSelectedTagChanged?.Invoke(this, new List<string>());
			}
		};
	}

	protected override bool HandleCheckedAble(FilterListItemViewModel selectedItem)
	{
		if (selectedItem.Value.ToString() == "!tag" && base.ViewModel.Logic == LogicType.And)
		{
			Utils.Toast(Utils.GetString("CannotSelectNoTagAndLogicAndAtTheSameTime"));
			return true;
		}
		if (base.ViewModel.Logic == LogicType.Not)
		{
			string text = selectedItem.Value.ToString();
			if (!(text == "!tag"))
			{
				if (text == "*withtags" && base.ViewModel.WithoutTagsSelected)
				{
					Utils.Toast(Utils.GetString("CannotSelectWithTagNoTagAndLogicNotAtTheSameTime"));
					return true;
				}
			}
			else if (base.ViewModel.WithTagsSelected)
			{
				Utils.Toast(Utils.GetString("CannotSelectWithTagNoTagAndLogicNotAtTheSameTime"));
				return true;
			}
		}
		return false;
	}

	protected override bool CanSave()
	{
		if (_selectedTags.Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}

	protected override void SyncOriginal()
	{
		_originalTags = _selectedTags.ToList();
	}

	private void InitData(bool showAll, ICollection<string> selectedTags, LogicType logicType)
	{
		ObservableCollection<FilterListItemViewModel> observableCollection = new ObservableCollection<FilterListItemViewModel>();
		List<TagModel> tags = TagDataHelper.GetTags();
		if (tags != null && tags.Count > 0)
		{
			observableCollection.Add(new FilterListItemViewModel
			{
				Title = Utils.GetString("WithTags"),
				Icon = "Ic.Tag.HasTag",
				Value = "*withtags",
				ShowIcon = true,
				Selected = selectedTags.Contains("*withtags")
			});
			observableCollection.Add(new FilterListItemViewModel
			{
				Title = Utils.GetString("NoTagsTitle"),
				Icon = "Ic.Tag.NoTag",
				Value = "!tag",
				ShowIcon = true,
				Selected = selectedTags.Contains("!tag")
			});
			List<TagModel> source = tags.Where((TagModel t) => t.IsChild()).ToList();
			foreach (FilterListItemViewModel item in from tag in tags
				where !tag.IsChild()
				select new FilterListItemViewModel(tag, selectedTags))
			{
				if (item.IsTagParent)
				{
					List<TagModel> source2 = source.Where((TagModel t) => t.parent == (string)item.Value).ToList();
					item.Children = source2.Select((TagModel t) => new FilterListItemViewModel(t, selectedTags)).ToList();
					item.PartSelected = !item.Selected && (item.Children?.Any((FilterListItemViewModel c) => c.Selected)).Value;
				}
				observableCollection.Add(item);
				if (item.Unfold)
				{
					item.Children?.ForEach(observableCollection.Add);
				}
			}
		}
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.Tag,
			ShowAll = showAll,
			AllData = Utils.GetGeometry("Ic.Tag.AllTag"),
			AllPathWidth = 18,
			CanSelectLogic = true,
			Logic = logicType,
			ItemsSource = observableCollection,
			IsAllSelected = (selectedTags.Count == 0),
			WithTagsSelected = selectedTags.Contains("*withtags"),
			WithoutTagsSelected = selectedTags.Contains("!tag"),
			SupportedLogic = new List<LogicType>
			{
				LogicType.And,
				LogicType.Or,
				LogicType.Not
			}
		};
	}

	public override void Restore()
	{
		RestoreSetSelected();
		this.OnSelectedTagChanged?.Invoke(this, _originalTags);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			item.Selected = _originalTags.Contains(item.Value.ToString());
		}
	}
}
