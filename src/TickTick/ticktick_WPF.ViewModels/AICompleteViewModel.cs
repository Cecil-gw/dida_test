using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views;

namespace ticktick_WPF.ViewModels;

internal class AICompleteViewModel : INotifyPropertyChanged
{
	private AICompleteIconType _iconType;

	private string _summaryLine;

	private bool _isShowExpandSummaryButton;

	private bool _isExpandSummary;

	private string _waitSupplementedInfo;

	private bool _isShowWaitSupInfo;

	private bool _isShowSuggestion;

	private string _suggestion;

	private bool _isShowSubTask;

	private bool _isShowGeneratingLine;

	private bool _isShowAIChoiceArea;

	private bool _isExpandAIChoices;

	private bool _isShowPages;

	private bool _isShowAIWarning;

	private bool _isShowSupplementTextbox;

	private bool _isShowEditTextbox;

	private string _editText;

	private bool _isShowStopButton;

	private bool _isShowBottomBtns;

	private string _supplementedText;

	private int _totalPageCount;

	private int _currentPageCount;

	private bool _isShowFeedbackPopup;

	private string _feedBackText;

	private bool _isFeedBackWithData;

	private bool _isShowRebuildPopup;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? rebuildCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? reEditCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? closeEditCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? stopCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string>? chooseActionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? prevPageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? nextPageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<AISubTaskEditItem>? deleteSubTaskCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? showFeedBackCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string>? feedBackEmojiChooseCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? postFeedBackCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private CommunityToolkit.Mvvm.Input.RelayCommand? switchExpandChoicesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? saveCommand;

	public Action OnShowRebuildPopup { get; set; }

	public Action OnTextboxGotFocus { get; set; }

	public Action OnUpdateSummaryLineButton { get; set; }

	public Action OnCloseWindow { get; set; }

	public Action OnScrollToBottom { get; set; }

	public Action OnSuggestionMdFocus { get; set; }

	private TaskBaseViewModel rawTask { get; set; }

	private AIUserInputDto userInput { get; set; }

	private string scene { get; set; }

	private CancellationTokenSource cts { get; set; }

	private TaskCompletionSource<bool> receiveTcs { get; set; }

	private TaskCompletionSource<bool> generateTcs { get; set; }

	private BlockingCollection<string> collection { get; set; }

	private StringBuilder sb { get; set; } = new StringBuilder();

	public AICompleteHistoryPage CurrentPage
	{
		get
		{
			if (CurrentPageCount != 0)
			{
				return Pages[CurrentPageCount - 1];
			}
			return null;
		}
	}

	private AICompleteHistoryPage LastPage
	{
		get
		{
			if (CurrentPageCount <= 1 || Pages.Count <= 1)
			{
				return Pages.FirstOrDefault();
			}
			return Pages[CurrentPageCount - 2];
		}
	}

	private int delayOutPut { get; set; } = 2000;

	private int outputInterval { get; set; } = 60;

	private int eachTimeWordLen { get; set; } = 3;

	private DateTime closeFeedBackTime { get; set; }

	private DateTime closeRebuildPopupTime { get; set; }

	public bool HasMinied { get; set; }

	public AICompleteState lastState { get; set; }

	public string Title { get; set; }

	public AICompleteStep Step
	{
		get
		{
			if (CurrentPage != null)
			{
				return CurrentPage.Step;
			}
			return AICompleteStep.None;
		}
		set
		{
			AICompleteStep aICompleteStep = ((CurrentPage != null) ? CurrentPage.Step : AICompleteStep.None);
			if (value != aICompleteStep)
			{
				CurrentPage.Step = value;
				NotifyPropertyChanged("Step");
				UpdateView();
			}
		}
	}

	public AICompleteState State
	{
		get
		{
			if (CurrentPage != null)
			{
				return CurrentPage.State;
			}
			return AICompleteState.Finish;
		}
		set
		{
			AICompleteState aICompleteState = ((CurrentPage == null) ? AICompleteState.Finish : CurrentPage.State);
			if (value != aICompleteState)
			{
				lastState = aICompleteState;
				CurrentPage.State = value;
				NotifyPropertyChanged("State");
				UpdateView();
			}
		}
	}

	public AICompleteIconType IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			_iconType = value;
			NotifyPropertyChanged("IconType");
		}
	}

	public string SummaryLine
	{
		get
		{
			return _summaryLine;
		}
		set
		{
			_summaryLine = value;
			NotifyPropertyChanged("SummaryLine");
			OnUpdateSummaryLineButton?.Invoke();
		}
	}

	public bool IsShowExpandSummaryButton
	{
		get
		{
			return _isShowExpandSummaryButton;
		}
		set
		{
			if (_isShowExpandSummaryButton != value)
			{
				_isShowExpandSummaryButton = value;
				NotifyPropertyChanged("IsShowExpandSummaryButton");
			}
		}
	}

	public bool IsExpandSummary
	{
		get
		{
			return _isExpandSummary;
		}
		set
		{
			if (_isExpandSummary != value)
			{
				_isExpandSummary = value;
				NotifyPropertyChanged("IsExpandSummary");
			}
		}
	}

	public string WaitSupplementedInfo
	{
		get
		{
			return _waitSupplementedInfo;
		}
		set
		{
			_waitSupplementedInfo = value;
			NotifyPropertyChanged("WaitSupplementedInfo");
		}
	}

	public bool IsShowWaitSupInfo
	{
		get
		{
			return _isShowWaitSupInfo;
		}
		set
		{
			if (_isShowWaitSupInfo != value)
			{
				_isShowWaitSupInfo = value;
				NotifyPropertyChanged("IsShowWaitSupInfo");
			}
		}
	}

	public bool IsShowSuggestion
	{
		get
		{
			return _isShowSuggestion;
		}
		set
		{
			if (_isShowSuggestion != value)
			{
				_isShowSuggestion = value;
				NotifyPropertyChanged("IsShowSuggestion");
			}
		}
	}

	public string Suggestion
	{
		get
		{
			return _suggestion;
		}
		set
		{
			_suggestion = value;
			NotifyPropertyChanged("Suggestion");
		}
	}

	public bool IsShowSubTask
	{
		get
		{
			return _isShowSubTask;
		}
		set
		{
			if (_isShowSubTask != value)
			{
				_isShowSubTask = value;
				NotifyPropertyChanged("IsShowSubTask");
			}
		}
	}

	public bool IsShowGeneratingLine
	{
		get
		{
			return _isShowGeneratingLine;
		}
		set
		{
			if (_isShowGeneratingLine != value)
			{
				_isShowGeneratingLine = value;
				NotifyPropertyChanged("IsShowGeneratingLine");
			}
		}
	}

	public bool IsShowAIChoiceArea
	{
		get
		{
			return _isShowAIChoiceArea;
		}
		set
		{
			if (_isShowAIChoiceArea != value)
			{
				_isShowAIChoiceArea = value;
				NotifyPropertyChanged("IsShowAIChoiceArea");
			}
		}
	}

	public bool IsExpandAIChoices
	{
		get
		{
			return _isExpandAIChoices;
		}
		set
		{
			if (_isExpandAIChoices != value)
			{
				_isExpandAIChoices = value;
				NotifyPropertyChanged("IsExpandAIChoices");
			}
		}
	}

	public bool IsShowPages
	{
		get
		{
			return _isShowPages;
		}
		set
		{
			if (_isShowPages != value)
			{
				_isShowPages = value;
				NotifyPropertyChanged("IsShowPages");
			}
		}
	}

	public bool IsShowAIWarning
	{
		get
		{
			return _isShowAIWarning;
		}
		set
		{
			if (_isShowAIWarning != value)
			{
				_isShowAIWarning = value;
				NotifyPropertyChanged("IsShowAIWarning");
			}
		}
	}

	public bool IsShowSupplementTextbox
	{
		get
		{
			return _isShowSupplementTextbox;
		}
		set
		{
			_isShowSupplementTextbox = value;
			NotifyPropertyChanged("IsShowSupplementTextbox");
		}
	}

	public bool IsShowEditTextbox
	{
		get
		{
			return _isShowEditTextbox;
		}
		set
		{
			_isShowEditTextbox = value;
			NotifyPropertyChanged("IsShowEditTextbox");
		}
	}

	public string EditText
	{
		get
		{
			return _editText;
		}
		set
		{
			_editText = value;
			NotifyPropertyChanged("EditText");
		}
	}

	public bool IsShowStopButton
	{
		get
		{
			return _isShowStopButton;
		}
		set
		{
			_isShowStopButton = value;
			NotifyPropertyChanged("IsShowStopButton");
		}
	}

	public bool IsShowBottomBtns
	{
		get
		{
			return _isShowBottomBtns;
		}
		set
		{
			_isShowBottomBtns = value;
			NotifyPropertyChanged("IsShowBottomBtns");
		}
	}

	public string SupplementedText
	{
		get
		{
			return _supplementedText;
		}
		set
		{
			_supplementedText = value;
			NotifyPropertyChanged("SupplementedText");
		}
	}

	public ObservableCollection<string> SuggestChoices { get; set; } = new ObservableCollection<string>();

	public ObservableCollection<AISubTaskEditItem> SubTasks { get; set; } = new ObservableCollection<AISubTaskEditItem>();

	public int TotalPageCount
	{
		get
		{
			return _totalPageCount;
		}
		set
		{
			_totalPageCount = value;
			NotifyPropertyChanged("TotalPageCount");
		}
	}

	public int CurrentPageCount
	{
		get
		{
			return _currentPageCount;
		}
		set
		{
			_currentPageCount = value;
			NotifyPropertyChanged("CurrentPageCount");
		}
	}

	public bool IsShowFeedbackPopup
	{
		get
		{
			return _isShowFeedbackPopup;
		}
		set
		{
			_isShowFeedbackPopup = value;
			NotifyPropertyChanged("IsShowFeedbackPopup");
			if (!value)
			{
				closeFeedBackTime = DateTime.Now;
			}
		}
	}

	public string FeedBackText
	{
		get
		{
			return _feedBackText;
		}
		set
		{
			_feedBackText = value;
			NotifyPropertyChanged("FeedBackText");
		}
	}

	public bool IsFeedBackWithData
	{
		get
		{
			return _isFeedBackWithData;
		}
		set
		{
			_isFeedBackWithData = value;
			NotifyPropertyChanged("IsFeedBackWithData");
		}
	}

	public bool IsShowRebuildPopup
	{
		get
		{
			return _isShowRebuildPopup;
		}
		set
		{
			_isShowRebuildPopup = value;
			NotifyPropertyChanged("IsShowRebuildPopup");
			if (!value)
			{
				closeRebuildPopupTime = DateTime.Now;
			}
		}
	}

	private ObservableCollection<AICompleteHistoryPage> Pages { get; set; } = new ObservableCollection<AICompleteHistoryPage>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RebuildCommand => rebuildCommand ?? (rebuildCommand = new AsyncRelayCommand(Rebuild));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ReEditCommand => reEditCommand ?? (reEditCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ReEdit));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CloseEditCommand => closeEditCommand ?? (closeEditCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(CloseEdit));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand StopCommand => stopCommand ?? (stopCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Stop));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> ChooseActionCommand => chooseActionCommand ?? (chooseActionCommand = new RelayCommand<string>(ChooseAction));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PrevPageCommand => prevPageCommand ?? (prevPageCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(PrevPage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand NextPageCommand => nextPageCommand ?? (nextPageCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(NextPage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<AISubTaskEditItem> DeleteSubTaskCommand => deleteSubTaskCommand ?? (deleteSubTaskCommand = new RelayCommand<AISubTaskEditItem>(DeleteSubTask));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ShowFeedBackCommand => showFeedBackCommand ?? (showFeedBackCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowFeedBack));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> FeedBackEmojiChooseCommand => feedBackEmojiChooseCommand ?? (feedBackEmojiChooseCommand = new RelayCommand<string>(FeedBackEmojiChoose));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PostFeedBackCommand => postFeedBackCommand ?? (postFeedBackCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(PostFeedBack));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SwitchExpandChoicesCommand => switchExpandChoicesCommand ?? (switchExpandChoicesCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(SwitchExpandChoices));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveCommand => saveCommand ?? (saveCommand = new AsyncRelayCommand(Save));

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public AICompleteViewModel(TaskBaseViewModel task)
	{
		rawTask = task;
		Title = task.Title;
		Init();
	}

	private async Task Init()
	{
		userInput = new AIUserInputDto
		{
			title = rawTask.Title,
			content = rawTask.Content,
			category = rawTask.ProjectName + "/" + rawTask.ColumnName,
			startDate = rawTask.StartDate,
			dueDate = rawTask.DueDate,
			tags = rawTask.Tags?.ToList()
		};
		try
		{
			TaskModel taskModel = await TaskDao.GetFullTaskById(rawTask.Id);
			if (taskModel != null)
			{
				userInput.items = taskModel.items?.ToList();
			}
			if (rawTask.ChildrenIds != null && rawTask.ChildrenIds.Any())
			{
				userInput.childIds = new List<AIUserInputDto.ChildId>();
				List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(rawTask.Id, rawTask.ProjectId);
				if (list != null)
				{
					foreach (TaskModel item2 in list)
					{
						AIUserInputDto.ChildId item = new AIUserInputDto.ChildId
						{
							content = item2.content,
							title = item2.title,
							dueDate = item2.dueDate,
							startDate = item2.startDate,
							tags = item2.tags?.ToList()
						};
						userInput.childIds.Add(item);
					}
				}
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		AddPage();
		Analyzing();
	}

	private void AddPage()
	{
		if (CurrentPage != null)
		{
			CurrentPage.Suggestion = Suggestion;
			CurrentPage.SubTasks = SubTasks?.ToList();
		}
		Pages.Add(new AICompleteHistoryPage
		{
			Page = Pages.Count + 1
		});
		CurrentPageCount = Pages.Count;
		TotalPageCount = Pages.Count;
		IsShowPages = Pages.Count > 1;
		WaitSupplementedInfo = CurrentPage.SupplementQuestion;
	}

	private void BeforeRequest()
	{
		sb.Clear();
		cts?.Dispose();
		cts = new CancellationTokenSource();
		receiveTcs = new TaskCompletionSource<bool>();
		generateTcs = new TaskCompletionSource<bool>();
		collection = new BlockingCollection<string>();
		ConsumeTask();
	}

	private async Task ConsumeTask()
	{
		Task.Run(async delegate
		{
			bool flag = false;
			bool flag2 = false;
			foreach (string item in collection.GetConsumingEnumerable())
			{
				flag2 = true;
				switch (Step)
				{
				case AICompleteStep.Analyze:
				case AICompleteStep.Supplement:
					OnReceivingAnalyzing(item);
					break;
				case AICompleteStep.BuildSuggestion:
					OnReceivingBuilding(item);
					break;
				case AICompleteStep.CreateSubTask:
					OnReceivingSubTask(item);
					break;
				case AICompleteStep.ImproveSuggestionAndSubTask:
					OnReceivingImproveAll(item);
					break;
				}
				if (!flag)
				{
					flag = true;
					OutputTask(Step);
				}
			}
			receiveTcs?.TrySetResult(result: true);
			if (!flag2 && State != AICompleteState.Stopped)
			{
				Utils.Toast("服务暂不可用，请稍后再试");
				State = AICompleteState.Stopped;
				generateTcs?.TrySetResult(result: true);
			}
		});
	}

	private async Task OutputTask(AICompleteStep actStep)
	{
		await Task.Delay(delayOutPut);
		if (State != AICompleteState.Stopped)
		{
			switch (actStep)
			{
			case AICompleteStep.Analyze:
			case AICompleteStep.Supplement:
				OutputAnalayzing();
				break;
			case AICompleteStep.BuildSuggestion:
				OutputBuilding();
				break;
			case AICompleteStep.CreateSubTask:
				OutputSubTask();
				break;
			case AICompleteStep.ImproveSuggestionAndSubTask:
				OutputImproveAll();
				break;
			}
		}
	}

	private async Task Analyzing()
	{
		Step = AICompleteStep.Analyze;
		State = AICompleteState.Querying;
		BeforeRequest();
		await Communicator.PostAITaskScene(userInput, collection, cts?.Token);
		await Task.Delay(delayOutPut);
		if (State != AICompleteState.Stopped)
		{
			if (!receiveTcs.Task.IsCompleted || !string.IsNullOrEmpty(CurrentPage.SupplementQuestion))
			{
				State = AICompleteState.Building;
			}
			await receiveTcs.Task;
			await generateTcs.Task;
			if (!IsShowWaitSupInfo)
			{
				await Building();
			}
			else
			{
				State = AICompleteState.Finish;
			}
		}
	}

	private void OnReceivingAnalyzing(string line)
	{
		AITaskSceneDto aITaskSceneDto = DealReceiveData<AITaskSceneDto>(line);
		if (aITaskSceneDto != null)
		{
			scene = aITaskSceneDto.scene;
			CurrentPage.SupplementQuestion = aITaskSceneDto.ideaAdvice;
		}
	}

	private async Task OutputAnalayzing()
	{
		int sIndex = 0;
		await Task.Run(async delegate
		{
			while (true)
			{
				CancellationTokenSource cancellationTokenSource = cts;
				if ((cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested) || (string.IsNullOrEmpty(CurrentPage.SupplementQuestion) && receiveTcs.Task.IsCompleted))
				{
					break;
				}
				if (!string.IsNullOrEmpty(CurrentPage.SupplementQuestion))
				{
					int val = sIndex + eachTimeWordLen;
					val = Math.Min(val, CurrentPage.SupplementQuestion.Length);
					if (val == sIndex && receiveTcs.Task.IsCompleted)
					{
						break;
					}
					sIndex = val;
					Application.Current.Dispatcher.Invoke(delegate
					{
						Step = AICompleteStep.Supplement;
						WaitSupplementedInfo = CurrentPage.SupplementQuestion.Substring(0, sIndex);
					});
				}
				await Task.Delay(outputInterval);
			}
			generateTcs?.TrySetResult(result: true);
		});
	}

	private async Task Building()
	{
		Step = AICompleteStep.BuildSuggestion;
		AITaskAdviceInputDto dto = new AITaskAdviceInputDto
		{
			userInput = JsonConvert.SerializeObject(userInput),
			scene = scene,
			nextStepAdvice = LastPage.SelectChoice,
			preCompleteAdvice = LastPage.Suggestion,
			userPrompt = LastPage.EditPrompt,
			userInputForIdeaAdvice = SupplementedText
		};
		State = AICompleteState.Querying;
		BeforeRequest();
		await Communicator.PostAITaskAdviceStream(dto, collection, cts?.Token);
		await Task.Delay(delayOutPut);
		if (State != AICompleteState.Stopped)
		{
			State = AICompleteState.Building;
			await receiveTcs.Task;
			await generateTcs.Task;
			if (State != AICompleteState.Stopped)
			{
				State = AICompleteState.Finish;
				OnScrollToBottom?.Invoke();
			}
		}
	}

	private void OnReceivingBuilding(string line)
	{
		AITaskAdviceDto aITaskAdviceDto = DealReceiveData<AITaskAdviceDto>(line);
		if (aITaskAdviceDto != null)
		{
			CurrentPage.Suggestion = aITaskAdviceDto.completeAdvice;
			CurrentPage.Choices = aITaskAdviceDto.nextStepAdvices;
		}
	}

	private async Task OutputBuilding()
	{
		int sIndex = 0;
		await Task.Run(async delegate
		{
			while (true)
			{
				CancellationTokenSource cancellationTokenSource = cts;
				if (cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested)
				{
					break;
				}
				if (!string.IsNullOrEmpty(CurrentPage.Suggestion))
				{
					int val = sIndex + eachTimeWordLen;
					val = Math.Min(val, CurrentPage.Suggestion.Length);
					if (val > 0 && CurrentPage.Suggestion[val - 1] == '\\' && val < CurrentPage.Suggestion.Length)
					{
						val++;
					}
					if (val > sIndex)
					{
						sIndex = val;
						Application.Current.Dispatcher.Invoke(delegate
						{
							Suggestion = CurrentPage.Suggestion.Substring(0, sIndex);
						});
					}
					else
					{
						List<string> choices = CurrentPage.Choices;
						if (choices != null && choices.Any())
						{
							if (SuggestChoices.Count < choices.Count - 1)
							{
								Application.Current.Dispatcher.Invoke(delegate
								{
									IsShowAIChoiceArea = true;
									IsExpandAIChoices = !SubTasks.Any();
									SuggestChoices.Add(choices[SuggestChoices.Count]);
								});
								await Task.Delay(2 * outputInterval);
							}
							else if (receiveTcs.Task.IsCompleted)
							{
								Application.Current.Dispatcher.Invoke(delegate
								{
									SuggestChoices.Add(choices.LastOrDefault());
								});
								break;
							}
						}
					}
				}
				await Task.Delay(outputInterval);
			}
			generateTcs?.TrySetResult(result: true);
		});
	}

	private async Task SubTaskGenerating()
	{
		Step = AICompleteStep.CreateSubTask;
		SubTasks.Clear();
		AITaskDecompInputDto dto = new AITaskDecompInputDto
		{
			userInput = JsonConvert.SerializeObject(userInput),
			scene = scene,
			userInputForIdeaAdvice = SupplementedText,
			completeAdvice = LastPage.Suggestion
		};
		State = AICompleteState.Querying;
		BeforeRequest();
		await Communicator.PostAITaskDecompStream(dto, collection, cts?.Token);
		await Task.Delay(delayOutPut);
		if (State != AICompleteState.Stopped)
		{
			State = AICompleteState.Building;
			await receiveTcs.Task;
			await generateTcs.Task;
			if (State != AICompleteState.Stopped)
			{
				State = AICompleteState.Finish;
				OnScrollToBottom?.Invoke();
			}
			CurrentPage.Suggestion = Suggestion;
			if (SuggestChoices.Count >= 4)
			{
				SuggestChoices.RemoveAt(0);
			}
			CurrentPage.Choices = SuggestChoices.ToList();
		}
	}

	private void OnReceivingSubTask(string line)
	{
		AITaskDecompDto aITaskDecompDto = DealReceiveData<AITaskDecompDto>(line);
		if (aITaskDecompDto == null)
		{
			return;
		}
		List<string> subTasks = aITaskDecompDto.subTasks;
		if (subTasks == null || !subTasks.Any())
		{
			return;
		}
		List<AISubTaskEditItem> list = new List<AISubTaskEditItem>();
		foreach (string item in subTasks)
		{
			list.Add(new AISubTaskEditItem
			{
				Name = item
			});
		}
		CurrentPage.SubTasks = list;
	}

	private async Task OutputSubTask()
	{
		int sIndex = 0;
		await Task.Run(async delegate
		{
			while (true)
			{
				CancellationTokenSource cancellationTokenSource = cts;
				if (cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested)
				{
					break;
				}
				List<AISubTaskEditItem> subTasks = CurrentPage.SubTasks;
				if ((subTasks == null || !subTasks.Any()) && receiveTcs.Task.IsCompleted)
				{
					break;
				}
				if (subTasks != null && subTasks.Any())
				{
					int val = sIndex + eachTimeWordLen;
					int val2 = subTasks.Sum((AISubTaskEditItem x) => x.Name.Length);
					val = Math.Min(val, val2);
					if (val == sIndex && receiveTcs.Task.IsCompleted)
					{
						break;
					}
					sIndex = val;
					Application.Current.Dispatcher.Invoke(delegate
					{
						SubTasks.Clear();
						int num = sIndex;
						for (int i = 0; i < subTasks.Count - 1; i++)
						{
							if (num <= 0)
							{
								break;
							}
							AISubTaskEditItem aISubTaskEditItem = subTasks[i];
							if (aISubTaskEditItem.Name.Length > num)
							{
								SubTasks.Add(new AISubTaskEditItem
								{
									Name = aISubTaskEditItem.Name.Substring(0, num)
								});
								num = 0;
								break;
							}
							num -= aISubTaskEditItem.Name.Length;
							SubTasks.Add(aISubTaskEditItem);
						}
					});
				}
				await Task.Delay(outputInterval);
			}
			generateTcs?.TrySetResult(result: true);
		});
	}

	private async Task ImprovingSuggestAndSubTask()
	{
		Step = AICompleteStep.ImproveSuggestionAndSubTask;
		SubTasks.Clear();
		AITaskImproveInputDto dto = new AITaskImproveInputDto
		{
			userInput = JsonConvert.SerializeObject(userInput),
			scene = scene,
			nextStepAdvice = LastPage.SelectChoice,
			preCompleteAdvice = LastPage.Suggestion,
			preSubTasks = LastPage.SubTasks.Select((AISubTaskEditItem x) => x.Name).ToList(),
			userPrompt = LastPage.EditPrompt
		};
		State = AICompleteState.Querying;
		BeforeRequest();
		await Communicator.PostAITaskImproveStream(dto, collection, cts?.Token);
		await Task.Delay(delayOutPut);
		if (State != AICompleteState.Stopped)
		{
			State = AICompleteState.Building;
			await receiveTcs.Task;
			await generateTcs.Task;
			if (State != AICompleteState.Stopped)
			{
				State = AICompleteState.Finish;
				OnScrollToBottom?.Invoke();
			}
		}
	}

	private void OnReceivingImproveAll(string line)
	{
		AITaskImproveDto aITaskImproveDto = DealReceiveData<AITaskImproveDto>(line);
		if (aITaskImproveDto == null)
		{
			return;
		}
		CurrentPage.Suggestion = aITaskImproveDto.completeAdvice;
		CurrentPage.Choices = aITaskImproveDto.nextStepAdvices;
		List<string> subTasks = aITaskImproveDto.subTasks;
		if (subTasks == null || !subTasks.Any())
		{
			return;
		}
		List<AISubTaskEditItem> list = new List<AISubTaskEditItem>();
		foreach (string item in subTasks)
		{
			list.Add(new AISubTaskEditItem
			{
				Name = item
			});
		}
		CurrentPage.SubTasks = list;
	}

	private async Task OutputImproveAll()
	{
		int sIndex = 0;
		int stIndex = 0;
		await Task.Run(async delegate
		{
			while (true)
			{
				CancellationTokenSource cancellationTokenSource = cts;
				if (cancellationTokenSource != null && cancellationTokenSource.Token.IsCancellationRequested)
				{
					break;
				}
				if (!string.IsNullOrEmpty(CurrentPage.Suggestion))
				{
					int val = sIndex + eachTimeWordLen;
					val = Math.Min(val, CurrentPage.Suggestion.Length);
					if (val > 0 && CurrentPage.Suggestion[val - 1] == '\\' && val < CurrentPage.Suggestion.Length)
					{
						val++;
					}
					if (val > sIndex)
					{
						sIndex = val;
						Application.Current.Dispatcher.Invoke(delegate
						{
							Suggestion = CurrentPage.Suggestion.Substring(0, sIndex);
						});
					}
					else
					{
						List<AISubTaskEditItem> subTasks = CurrentPage.SubTasks;
						if (subTasks != null && subTasks.Any())
						{
							int val2 = stIndex + eachTimeWordLen;
							int val3 = subTasks.Sum((AISubTaskEditItem x) => x.Name.Length);
							val2 = Math.Min(val2, val3);
							if (val2 > stIndex)
							{
								stIndex = val2;
								Application.Current.Dispatcher.Invoke(delegate
								{
									IsShowSubTask = true;
									SubTasks.Clear();
									int num = stIndex;
									for (int i = 0; i < subTasks.Count - 1; i++)
									{
										if (num <= 0)
										{
											break;
										}
										AISubTaskEditItem aISubTaskEditItem = subTasks[i];
										if (aISubTaskEditItem.Name.Length > num)
										{
											SubTasks.Add(new AISubTaskEditItem
											{
												Name = aISubTaskEditItem.Name.Substring(0, num)
											});
											num = 0;
											break;
										}
										num -= aISubTaskEditItem.Name.Length;
										SubTasks.Add(aISubTaskEditItem);
									}
								});
							}
							else
							{
								List<string> choices = CurrentPage.Choices;
								if (choices != null && choices.Any())
								{
									if (SuggestChoices.Count < choices.Count - 1)
									{
										Application.Current.Dispatcher.Invoke(delegate
										{
											IsShowAIChoiceArea = true;
											IsExpandAIChoices = !SubTasks.Any();
											SuggestChoices.Add(choices[SuggestChoices.Count]);
										});
										await Task.Delay(2 * outputInterval);
									}
									else if (receiveTcs.Task.IsCompleted)
									{
										Application.Current.Dispatcher.Invoke(delegate
										{
											SuggestChoices.Add(choices.LastOrDefault());
										});
										break;
									}
								}
							}
						}
					}
				}
				await Task.Delay(outputInterval);
			}
			generateTcs?.TrySetResult(result: true);
		});
	}

	private T DealReceiveData<T>(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return default(T);
		}
		string text = line.Trim();
		if (text.StartsWith("data:"))
		{
			text = text.Substring(5);
		}
		try
		{
			text = JsonConvert.DeserializeObject<string>(text);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
		sb.Append(text);
		try
		{
			return JsonConvert.DeserializeObject<T>(JsonAutoCompleteHelper.CompleteJson(sb.ToString()));
		}
		catch (Exception ex)
		{
			Console.WriteLine(sb.ToString() + "  " + ex);
			return default(T);
		}
	}

	private void UpdateView()
	{
		bool isShowGeneratingLine = false;
		if (lastState != AICompleteState.Building || State != AICompleteState.Finish)
		{
			IsShowExpandSummaryButton = false;
			IsShowSuggestion = false;
			IsShowWaitSupInfo = false;
			IsShowAIChoiceArea = false;
			IsShowAIWarning = false;
			IsShowSubTask = false;
			IsExpandAIChoices = false;
			IsExpandSummary = false;
			bool flag = (IsShowEditTextbox = false);
			bool flag3 = (IsShowBottomBtns = flag);
			bool isShowStopButton = (IsShowSupplementTextbox = flag3);
			IsShowStopButton = isShowStopButton;
		}
		switch (Step)
		{
		case AICompleteStep.Analyze:
		{
			AICompleteState state = State;
			if ((uint)(state - 2) <= 1u)
			{
				IconType = AICompleteIconType.Stop;
				SummaryLine = Utils.GetString("AIGenerationStopped");
			}
			else
			{
				IconType = AICompleteIconType.Analyzing;
				SummaryLine = Utils.GetString("AlAnalyzing");
				isShowGeneratingLine = true;
			}
			break;
		}
		case AICompleteStep.Supplement:
		{
			AICompleteState state = State;
			if ((uint)(state - 1) <= 1u)
			{
				IconType = AICompleteIconType.SupplementInfo;
				SummaryLine = Utils.GetString("AIProvideMoreInfoText");
				IsShowWaitSupInfo = true;
				if (State == AICompleteState.Finish)
				{
					IsShowSupplementTextbox = true;
					OnTextboxGotFocus?.Invoke();
				}
			}
			break;
		}
		case AICompleteStep.BuildSuggestion:
			if (State == AICompleteState.Querying)
			{
				Suggestion = string.Empty;
				SuggestChoices.Clear();
				IconType = AICompleteIconType.Analyzing;
				SummaryLine = Utils.GetString("AIGeneratingSuggestion");
				isShowGeneratingLine = true;
				IsShowStopButton = true;
				break;
			}
			IsShowSuggestion = true;
			switch (State)
			{
			case AICompleteState.Building:
				IsShowStopButton = true;
				break;
			case AICompleteState.Finish:
				if (CurrentPage.Page > 1)
				{
					string lastSummary2 = LastPage.LastSummary;
					if (!string.IsNullOrEmpty(lastSummary2))
					{
						IconType = AICompleteIconType.Edit;
						SummaryLine = lastSummary2;
					}
					else
					{
						IconType = AICompleteIconType.Complete;
						SummaryLine = Utils.GetString("AISuggestionsGenerated");
					}
				}
				else if (!string.IsNullOrEmpty(CurrentPage.SupplementQuestion))
				{
					IconType = AICompleteIconType.Edit;
					SummaryLine = Utils.GetString("AIInfoAdded");
					IsShowExpandSummaryButton = true;
				}
				else
				{
					IconType = AICompleteIconType.Complete;
					SummaryLine = Utils.GetString("AISuggestionsGenerated");
				}
				IsShowStopButton = false;
				IsShowBottomBtns = true;
				IsShowAIChoiceArea = true;
				IsExpandAIChoices = !SubTasks.Any();
				IsShowAIWarning = true;
				OnSuggestionMdFocus?.Invoke();
				break;
			case AICompleteState.Stopped:
				IconType = AICompleteIconType.Stop;
				SummaryLine = Utils.GetString("AIGenerationStopped");
				IsShowBottomBtns = true;
				if (SuggestChoices != null && SuggestChoices.Any())
				{
					IsShowAIChoiceArea = true;
					IsShowAIWarning = true;
				}
				break;
			}
			break;
		case AICompleteStep.CreateSubTask:
			IsShowSuggestion = true;
			if (State == AICompleteState.Querying)
			{
				SubTasks.Clear();
				IconType = AICompleteIconType.Analyzing;
				SummaryLine = Utils.GetStringFormat("AIBreakDownToSubTasks");
				isShowGeneratingLine = true;
				IsShowStopButton = true;
				break;
			}
			IsShowSubTask = true;
			switch (State)
			{
			case AICompleteState.Building:
				IsShowStopButton = true;
				break;
			case AICompleteState.Finish:
				IconType = AICompleteIconType.Complete;
				SummaryLine = Utils.GetString("AISubtasksGenerated");
				IsShowStopButton = false;
				IsShowBottomBtns = true;
				IsShowAIChoiceArea = true;
				IsExpandAIChoices = !SubTasks.Any();
				IsShowAIWarning = true;
				IsShowSubTask = SubTasks.Any();
				break;
			case AICompleteState.Stopped:
				IconType = AICompleteIconType.Stop;
				SummaryLine = Utils.GetString("AIGenerationStopped");
				IsShowBottomBtns = true;
				if (SuggestChoices != null && SuggestChoices.Any())
				{
					IsShowAIChoiceArea = true;
					IsShowAIWarning = true;
				}
				break;
			}
			break;
		case AICompleteStep.ImproveSuggestionAndSubTask:
			if (State == AICompleteState.Querying)
			{
				Suggestion = string.Empty;
				SubTasks.Clear();
				SuggestChoices.Clear();
				IconType = AICompleteIconType.Analyzing;
				SummaryLine = Utils.GetString("AIGeneratingSuggestionsAndSubTasks");
				isShowGeneratingLine = true;
				IsShowStopButton = true;
				break;
			}
			IsShowSuggestion = true;
			switch (State)
			{
			case AICompleteState.Building:
				IsShowStopButton = true;
				break;
			case AICompleteState.Finish:
			{
				string lastSummary = LastPage.LastSummary;
				if (!string.IsNullOrEmpty(lastSummary))
				{
					IconType = AICompleteIconType.Edit;
					SummaryLine = lastSummary;
				}
				else
				{
					IconType = AICompleteIconType.Complete;
					SummaryLine = Utils.GetString("AISuggestionsAndSubtasksGenerated");
				}
				IsShowStopButton = false;
				IsExpandAIChoices = !SubTasks.Any();
				IsShowAIChoiceArea = true;
				IsShowAIWarning = true;
				IsShowBottomBtns = true;
				IsShowSubTask = SubTasks.Any();
				OnSuggestionMdFocus?.Invoke();
				break;
			}
			case AICompleteState.Stopped:
				IconType = AICompleteIconType.Stop;
				SummaryLine = Utils.GetString("AIGenerationStopped");
				IsShowBottomBtns = true;
				if (SuggestChoices != null && SuggestChoices.Any())
				{
					IsShowAIChoiceArea = true;
					IsShowAIWarning = true;
				}
				if (SubTasks != null && SubTasks.Any())
				{
					IsShowSubTask = true;
				}
				break;
			}
			break;
		}
		IsShowGeneratingLine = isShowGeneratingLine;
	}

	public void ConfirmEdit()
	{
		if (IsShowSupplementTextbox)
		{
			AICompleteHistoryPage currentPage = CurrentPage;
			string supplementInput = (SupplementedText = EditText);
			currentPage.SupplementInput = supplementInput;
			Building();
		}
		else
		{
			CurrentPage.EditPrompt = EditText;
			AddPage();
			if (SubTasks.Any())
			{
				ImprovingSuggestAndSubTask();
			}
			else
			{
				Building();
			}
		}
		EditText = string.Empty;
	}

	public void RebuildMenuSelect(bool onlySubTask)
	{
		AddPage();
		if (onlySubTask)
		{
			SubTaskGenerating();
		}
		else if (SubTasks.Any())
		{
			ImprovingSuggestAndSubTask();
		}
		else
		{
			Building();
		}
	}

	[RelayCommand]
	public async Task Rebuild()
	{
		if (SubTasks.Any())
		{
			if (!((DateTime.Now - closeRebuildPopupTime).TotalSeconds < 0.3))
			{
				OnShowRebuildPopup?.Invoke();
			}
			return;
		}
		AddPage();
		if (SubTasks.Any())
		{
			await ImprovingSuggestAndSubTask();
		}
		else
		{
			await Building();
		}
	}

	[RelayCommand]
	public void ReEdit()
	{
		IsShowEditTextbox = true;
		OnTextboxGotFocus?.Invoke();
	}

	[RelayCommand]
	public void CloseEdit()
	{
		EditText = string.Empty;
		IsShowEditTextbox = false;
	}

	[RelayCommand]
	public void Stop()
	{
		cts?.Cancel();
		if (State != AICompleteState.Finish)
		{
			State = AICompleteState.Stopped;
		}
	}

	[RelayCommand]
	public void ChooseAction(string action)
	{
		CurrentPage.SelectChoice = action;
		AddPage();
		if (action == SuggestChoices.FirstOrDefault() && SuggestChoices.Count >= 4)
		{
			SubTaskGenerating();
		}
		else if (SubTasks.Any())
		{
			ImprovingSuggestAndSubTask();
		}
		else
		{
			Building();
		}
	}

	[RelayCommand]
	public void PrevPage()
	{
		if (State != AICompleteState.Querying && State != AICompleteState.Building && CurrentPageCount > 1)
		{
			CurrentPage.Suggestion = Suggestion;
			CurrentPage.SubTasks = SubTasks?.ToList();
			CurrentPageCount--;
			ShowPageData();
			UpdateView();
		}
	}

	[RelayCommand]
	public void NextPage()
	{
		if (State != AICompleteState.Querying && State != AICompleteState.Building && CurrentPageCount < TotalPageCount)
		{
			CurrentPage.Suggestion = Suggestion;
			CurrentPage.SubTasks = SubTasks?.ToList();
			CurrentPageCount++;
			ShowPageData();
			UpdateView();
		}
	}

	[RelayCommand]
	public void DeleteSubTask(AISubTaskEditItem item)
	{
		int num = SubTasks.IndexOf(item);
		if (num >= 0)
		{
			SubTasks.RemoveAt(num);
		}
	}

	private void ShowPageData()
	{
		NotifyPropertyChanged("Step");
		WaitSupplementedInfo = CurrentPage.SupplementQuestion;
		Suggestion = CurrentPage.Suggestion;
		SuggestChoices.Clear();
		if (CurrentPage.Choices != null)
		{
			foreach (string choice in CurrentPage.Choices)
			{
				SuggestChoices.Add(choice);
			}
		}
		SubTasks.Clear();
		if (CurrentPage.SubTasks == null)
		{
			return;
		}
		foreach (AISubTaskEditItem subTask in CurrentPage.SubTasks)
		{
			SubTasks.Add(subTask);
		}
	}

	[RelayCommand]
	public void ShowFeedBack()
	{
		if (!((DateTime.Now - closeFeedBackTime).TotalSeconds < 0.1))
		{
			IsShowFeedbackPopup = true;
			OnTextboxGotFocus?.Invoke();
		}
	}

	[RelayCommand]
	public void FeedBackEmojiChoose(string index)
	{
		Console.WriteLine(index);
	}

	[RelayCommand]
	public void PostFeedBack()
	{
		Console.WriteLine(FeedBackText);
		IsShowFeedbackPopup = false;
	}

	[RelayCommand]
	public void SwitchExpandChoices()
	{
		if (IsExpandAIChoices)
		{
			OnScrollToBottom?.Invoke();
		}
	}

	[RelayCommand]
	public async Task Save()
	{
		bool replaceSubs = false;
		TaskModel exist = await TaskDao.GetTaskById(rawTask.Id);
		bool add;
		if (exist != null)
		{
			if (exist.deleted != 0)
			{
				if (new CustomerDialog("继续保存完成建议", "当前任务已被删除，是否添加一个新任务？", Utils.GetString("PublicSave"), Utils.GetString("Cancel"), Utils.GetToastWindow() as Window).ShowDialog() != true)
				{
					return;
				}
				rawTask.Id = Utils.GetGuid();
				add = true;
			}
			else
			{
				add = false;
				if ((await TaskDao.GetAllSubTasksById(exist.id, exist.projectId)).Any() && SubTasks.Any())
				{
					CustomerDialog customerDialog = new CustomerDialog("继续保存完成建议", "当前任务已有子任务，是否替换原有子任务", "替换", "保留", Utils.GetToastWindow() as Window);
					customerDialog.ShowDialog();
					if (!customerDialog.YesNoCancelResult.HasValue)
					{
						return;
					}
					replaceSubs = customerDialog.YesNoCancelResult == true;
				}
			}
		}
		else
		{
			add = true;
		}
		OnCloseWindow?.Invoke();
		bool isNote = false;
		if (add)
		{
			if (string.IsNullOrEmpty(rawTask.Content))
			{
				rawTask.Content += Suggestion;
			}
			else
			{
				TaskBaseViewModel taskBaseViewModel = rawTask;
				taskBaseViewModel.Content = taskBaseViewModel.Content + Environment.NewLine + Environment.NewLine + Suggestion;
			}
			TaskModel task = rawTask.CovertToNewTask();
			await TaskService.AddTask(task);
			DataChangedNotifier.NotifyAddTaskToSelectView(task);
		}
		else
		{
			switch (exist.kind)
			{
			case "CHECKLIST":
				if (string.IsNullOrEmpty(exist.desc))
				{
					exist.desc += Suggestion;
				}
				else
				{
					exist.desc = exist.desc + Environment.NewLine + Environment.NewLine + Suggestion;
				}
				break;
			case "TEXT":
				if (string.IsNullOrEmpty(exist.content))
				{
					exist.content += Suggestion;
				}
				else
				{
					exist.content = exist.content + Environment.NewLine + Environment.NewLine + Suggestion;
				}
				break;
			case "NOTE":
				isNote = true;
				if (string.IsNullOrEmpty(exist.content))
				{
					exist.content += Suggestion;
				}
				else
				{
					exist.content = exist.content + Environment.NewLine + Environment.NewLine + Suggestion;
				}
				if (SubTasks.Any())
				{
					exist.content = exist.content + Environment.NewLine + Environment.NewLine;
					exist.content += string.Join(Environment.NewLine, SubTasks.Select((AISubTaskEditItem x) => x.Name));
				}
				break;
			}
			exist.modifiedTime = DateTime.Now;
			await TaskService.UpdateTask(exist);
		}
		if (SubTasks.Any() && !isNote)
		{
			if (replaceSubs && exist != null)
			{
				await TaskService.DeleteAllSubTasks(exist);
			}
			List<TaskModel> list = new List<TaskModel>();
			foreach (AISubTaskEditItem subTask in SubTasks)
			{
				subTask.TaskId = Utils.GetGuid();
				TaskModel taskModel = new TaskModel();
				taskModel.id = subTask.TaskId;
				taskModel.title = subTask.Name;
				if (subTask.IsChecked)
				{
					taskModel.status = 2;
				}
				if (subTask.TimeData != null)
				{
					TimeData timeData = subTask.TimeData;
					taskModel.isAllDay = timeData.IsAllDay;
					taskModel.startDate = timeData.StartDate;
					taskModel.dueDate = timeData.DueDate;
					taskModel.reminders = timeData.Reminders?.ToArray();
					taskModel.repeatFrom = timeData.RepeatFrom;
					taskModel.repeatFlag = timeData.RepeatFlag;
					taskModel.exDates = ExDateSerilizer.ToString(timeData.ExDates?.ToArray());
					taskModel.isFloating = timeData.TimeZone.IsFloat;
					taskModel.timeZone = timeData.TimeZone.TimeZoneName;
					taskModel.reminders = timeData.Reminders?.ToArray();
				}
				list.Add(taskModel);
			}
			List<TaskModel> list2 = await TaskService.AddSubTasks(rawTask, list.ToArray());
			if (list2 != null)
			{
				foreach (TaskModel item in list2)
				{
					AISubTaskEditItem aISubTaskEditItem = SubTasks.FirstOrDefault((AISubTaskEditItem x) => x.TaskId == item.id);
					if (aISubTaskEditItem?.TimeData != null)
					{
						await TaskService.SetDate(item.id, aISubTaskEditItem.TimeData);
					}
				}
			}
		}
		if (HasMinied)
		{
			Utils.Toast("完成建议已保存");
		}
	}
}
