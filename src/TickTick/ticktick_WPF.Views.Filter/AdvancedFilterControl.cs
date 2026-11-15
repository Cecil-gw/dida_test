using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.Filter;

public class AdvancedFilterControl : UserControl, IComponentConnector
{
	private AdvancedFilterViewModel _viewModel = new AdvancedFilterViewModel();

	internal WrapPanel ConditionPanel;

	private bool _contentLoaded;

	public bool ExistNote { get; set; }

	public List<CardViewModel> CardList
	{
		private get
		{
			return ViewModel.CardList;
		}
		set
		{
			ViewModel.CardList = value;
			NotifyDataSetChanged();
		}
	}

	public AdvancedFilterViewModel ViewModel
	{
		get
		{
			return _viewModel;
		}
		set
		{
			_viewModel = value;
			NotifyDataSetChanged();
		}
	}

	public AdvancedFilterControl()
	{
		InitializeComponent();
	}

	private void NotifyDataSetChanged()
	{
		ConditionPanel.Children.Clear();
		if (ViewModel.CardList == null || ViewModel.CardList.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < ViewModel.CardList.Count; i++)
		{
			ViewModel.CardList[i].Index = i;
			CardViewModel model = ViewModel.CardList[i];
			ConditionCard filterConditionCard = GetFilterConditionCard(model);
			if (filterConditionCard != null)
			{
				filterConditionCard.OnConditionChanged += OnConditionChanged;
				filterConditionCard.GetDropDownList += CardGetDropDownList;
				ConditionPanel.Children.Add(filterConditionCard);
			}
		}
	}

	private void NotifyItemChanged(int index, CardViewModel viewModel)
	{
		ConditionCard filterConditionCard = GetFilterConditionCard(viewModel);
		filterConditionCard.OnConditionChanged += OnConditionChanged;
		filterConditionCard.GetDropDownList += CardGetDropDownList;
		ConditionPanel.Children.RemoveAt(index);
		ConditionPanel.Children.Insert(index, filterConditionCard);
	}

	private List<CondType> CardGetDropDownList()
	{
		List<CondType> list = new List<CondType>
		{
			CondType.Lists,
			CondType.Tag,
			CondType.Date,
			CondType.Priority,
			CondType.Assignee,
			CondType.KeyWords,
			CondType.TaskType
		};
		foreach (CardViewModel item in CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList())
		{
			if (!(item is ProjectOrGroupCardViewModel))
			{
				if (!(item is DateCardViewModel))
				{
					if (!(item is PriorityCardViewModel))
					{
						if (!(item is AssigneeCardViewModel))
						{
							if (!(item is KeywordsViewModel))
							{
								if (item is TaskTypeViewModel)
								{
									list.Remove(CondType.TaskType);
								}
							}
							else
							{
								list.Remove(CondType.KeyWords);
							}
						}
						else
						{
							list.Remove(CondType.Assignee);
						}
					}
					else
					{
						list.Remove(CondType.Priority);
					}
				}
				else
				{
					list.Remove(CondType.Date);
				}
			}
			else
			{
				list.Remove(CondType.Lists);
			}
		}
		List<TagModel> tags = TagDataHelper.GetTags(sort: false);
		if ((tags == null || tags.Count == 0) && list.Contains(CondType.Tag))
		{
			list.Remove(CondType.Tag);
		}
		if (CacheManager.GetProjects().Count((ProjectModel p) => p.IsShareList()) == 0 && list.Contains(CondType.Assignee))
		{
			list.Remove(CondType.Assignee);
		}
		if (!ExistNote)
		{
			list.Remove(CondType.TaskType);
		}
		return list;
	}

	private ConditionCard GetFilterConditionCard(CardViewModel model)
	{
		ConditionCard result = null;
		model.Version = ViewModel.Version;
		switch (model.Type)
		{
		case CardType.AddMore:
			result = new AddMoreCard(model);
			break;
		case CardType.InitLogic:
		case CardType.LogicAnd:
		case CardType.LogicOr:
			result = new LogicConditionCard(model);
			break;
		case CardType.InitFilter:
			result = new InitConditionCard(model);
			break;
		case CardType.Filter:
			if (model is ProjectOrGroupCardViewModel viewModel)
			{
				result = new ProjectOrGroupCard(viewModel);
			}
			else if (model is TagCardViewModel viewModel2)
			{
				result = new TagCard(viewModel2);
			}
			else if (model is DateCardViewModel viewModel3)
			{
				result = new DateCard(viewModel3);
			}
			else if (model is PriorityCardViewModel viewModel4)
			{
				result = new PriorityCard(viewModel4);
			}
			else if (model is AssigneeCardViewModel viewModel5)
			{
				result = new AssigneeCard(viewModel5);
			}
			else if (model is KeywordsViewModel viewModel6)
			{
				result = new KeywordsCard(viewModel6);
			}
			else if (model is TaskTypeViewModel viewModel7)
			{
				result = new TaskTypeCard(viewModel7);
			}
			break;
		}
		return result;
	}

	private void OnConditionChanged(object sender, ChangeData delta)
	{
		switch (delta.Type)
		{
		case ChangeType.AddMore:
			CardList = FilterConditionProvider.AddMoreCondition(ViewModel.CardList);
			break;
		case ChangeType.Filter:
		case ChangeType.Logic:
			delta.To.Index = delta.From.Index;
			CardList[delta.From.Index] = delta.To;
			NotifyItemChanged(delta.To.Index, delta.To);
			AdjustLeftCondition(delta);
			break;
		}
		TryRemoveEmptyFilter();
	}

	private void TryRemoveEmptyFilter()
	{
		if (CardList.Count == 5 && CardList[1].Type == CardType.InitLogic && CardList[2].Type == CardType.InitFilter)
		{
			CardList.RemoveAt(1);
			CardList.RemoveAt(1);
			CardList = FilterConditionProvider.AddMoreCondition(ViewModel.CardList);
			NotifyDataSetChanged();
		}
	}

	private void AdjustLeftCondition(ChangeData delta)
	{
		if (delta.To.Index - 1 <= 0)
		{
			return;
		}
		switch (delta.To.Type)
		{
		case CardType.InitFilter:
			AdjustLeftCard(delta, () => CardList[delta.To.Index - 1].Type != CardType.InitFilter, CardType.InitLogic);
			break;
		case CardType.Filter:
			AdjustLeftCard(delta, () => CardList[delta.To.Index - 1].Type != CardType.LogicAnd && CardList[delta.To.Index - 1].Type != CardType.LogicOr, CardType.LogicAnd);
			break;
		}
	}

	private void AdjustLeftCard(ChangeData changeData, Func<bool> condition, CardType type)
	{
		if (changeData.To.Index > 1)
		{
			int index = changeData.To.Index - 1;
			if (condition())
			{
				CardViewModel cardViewModel = new CardViewModel
				{
					Type = type,
					Index = index
				};
				CardList[index] = cardViewModel;
				NotifyItemChanged(index, cardViewModel);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/advancedfiltercontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			ConditionPanel = (WrapPanel)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
