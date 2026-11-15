using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.Input;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels.AI;

internal class AIAudioTransResultViewModel : INotifyPropertyChanged, IDisposable
{
	private AIAudioViewStateType _viewStateType = AIAudioViewStateType.TranscriptResult;

	private int _groupTitleSelectValue;

	private FlowDocument _allTextDocument;

	private string _topic;

	private string _summary;

	private bool _isEmptyContent;

	private string _errorText;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? startSummaryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? copyCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? insertCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? retryCommand;

	public AIAudioViewStateType ViewStateType
	{
		get
		{
			return _viewStateType;
		}
		set
		{
			_viewStateType = value;
			NotifyPropertyChanged("ViewStateType");
		}
	}

	public int GroupTitleSelectValue
	{
		get
		{
			return _groupTitleSelectValue;
		}
		set
		{
			_groupTitleSelectValue = value;
			NotifyPropertyChanged("GroupTitleSelectValue");
			OnGroupTitleChanged(collect: true);
		}
	}

	public FlowDocument AllTextDocument
	{
		get
		{
			return _allTextDocument;
		}
		set
		{
			_allTextDocument = value;
			NotifyPropertyChanged("AllTextDocument");
		}
	}

	public string Topic
	{
		get
		{
			return _topic;
		}
		set
		{
			_topic = value;
			NotifyPropertyChanged("Topic");
		}
	}

	public string Summary
	{
		get
		{
			return _summary;
		}
		set
		{
			_summary = value;
			NotifyPropertyChanged("Summary");
		}
	}

	public bool IsEmptyContent
	{
		get
		{
			return _isEmptyContent;
		}
		set
		{
			if (_isEmptyContent != value)
			{
				_isEmptyContent = value;
				NotifyPropertyChanged("IsEmptyContent");
			}
		}
	}

	public string ErrorText
	{
		get
		{
			return _errorText;
		}
		set
		{
			_errorText = value;
			NotifyPropertyChanged("ErrorText");
		}
	}

	public Action OnClose { get; set; }

	private AttachmentModel model { get; set; }

	private string projectId { get; set; }

	private string rawTopic { get; set; }

	public List<AIAudioOriginalItem> originalItems { get; set; } = new List<AIAudioOriginalItem>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand StartSummaryCommand => startSummaryCommand ?? (startSummaryCommand = new AsyncRelayCommand(StartSummary));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CopyCommand => copyCommand ?? (copyCommand = new AsyncRelayCommand(Copy));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand InsertCommand => insertCommand ?? (insertCommand = new AsyncRelayCommand(Insert));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RetryCommand => retryCommand ?? (retryCommand = new AsyncRelayCommand(Retry));

	public event PropertyChangedEventHandler? PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public AIAudioTransResultViewModel(AttachmentModel attachModel, string projId, AIAudioViewType viewType)
	{
		model = attachModel;
		projectId = projId;
		GroupTitleSelectValue = (int)viewType;
		LoadData();
		AIAudioService.OnToast = (Action<string, string>)Delegate.Combine(AIAudioService.OnToast, new Action<string, string>(OnServiceToast));
		AIAudioService.OnStateChange = (Action<string>)Delegate.Combine(AIAudioService.OnStateChange, new Action<string>(OnServiceResult));
		DataChangedNotifier.IsDarkChanged += DataChangedNotifier_IsDarkChanged;
	}

	private void DataChangedNotifier_IsDarkChanged(object sender, EventArgs e)
	{
		LoadData();
	}

	private void OnServiceResult(string attachmentId)
	{
		if (!(attachmentId != model.id))
		{
			Application.Current.Dispatcher.Invoke((Func<Task>)async delegate
			{
				LoadData();
				await OnGroupTitleChanged();
			});
		}
	}

	private void OnServiceToast(string attachmentId, string msg)
	{
		if (!(attachmentId != model.id) && ViewStateType != AIAudioViewStateType.Loading)
		{
			Utils.Toast(msg);
		}
	}

	private void Demo()
	{
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:00",
			Content = "到时候市场部出 2 个人（负责方案和物料需求）、设计部出 1 个人（对接设计文件）参加，你预订好之后，记得把会议时间、地点、参会人员名单同步给供应商，再提醒他们提前准备好报价单、过往的物料制作案例样本，方便咱们现场沟通。\u200b"
		});
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:12",
			Content = "嗯，好，那我们再先聊一下下季度的发布计划吧。其实上周已经有过初步讨论了，这次主要确认一些关键节点。\u200b"
		});
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:22",
			Content = "小王，你们设计这边的首页改版，大概什么时候能给出初稿？，大概什么时候能给出初稿？\u200b"
		});
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:00",
			Content = "到时候市场部出 2 个人（负责方案和物料需求）、设计部出 1 个人（对接设计文件）参加，你预订好之后，记得把会议时间、地点、参会人员名单同步给供应商，再提醒他们提前准备好报价单、过往的物料制作案例样本，方便咱们现场沟通。\u200b"
		});
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:12",
			Content = "嗯，好，那我们再先聊一下下季度的发布计划吧。其实上周已经有过初步讨论了，这次主要确认一些关键节点。\u200b"
		});
		originalItems.Add(new AIAudioOriginalItem
		{
			Time = "00:22",
			Content = "小王，你们设计这边的首页改版，大概什么时候能给出初稿？，大概什么时候能给出初稿？\u200b"
		});
		FlowDocument flowDocument = new FlowDocument();
		foreach (AIAudioOriginalItem originalItem in originalItems)
		{
			Paragraph paragraph = new Paragraph
			{
				Margin = default(Thickness)
			};
			if (!string.IsNullOrEmpty(originalItem.Time))
			{
				paragraph.Inlines.Add(new Run(originalItem.Time + " ")
				{
					Foreground = ThemeUtil.GetColor("BaseColorOpacity40"),
					FontSize = 12.0
				});
			}
			flowDocument.Blocks.Add(paragraph);
			paragraph = new Paragraph();
			paragraph.Margin = new Thickness(0.0, 6.0, 0.0, 16.0);
			paragraph.LineHeight = 23.8;
			if (!string.IsNullOrEmpty(originalItem.Content))
			{
				paragraph.Inlines.Add(new Run(originalItem.Content)
				{
					Foreground = ThemeUtil.GetColor("BaseColorOpacity90"),
					FontSize = 14.0
				});
			}
			flowDocument.Blocks.Add(paragraph);
		}
		flowDocument.PagePadding = new Thickness(0.0);
		AllTextDocument = flowDocument;
		Topic = "总结：本次会议主要是讨论下季度的发布计划，以及关键时间节点。";
		Summary = "讨论内容：\n 首页改版（设计部）\n 初稿提交时间：下周五之前完成。\n 核心功能开发（开发部）\n 计划上线时间：十月底。\n 风险说明：需视测试进度而定，测试顺利则按期完成。\n 宣传方案（市场部）\n 定稿时间：功能上线前一周。\n 供应商沟通\n 安排：下周召开一次沟通会议。\n 负责人：小李负责预订会议室。";
	}

	private async Task OnGroupTitleChanged(bool collect = false)
	{
		(HandleState, HandleState, AudioAttachmentAIResultModel) tuple = await AIAudioService.CheckHandleState(model.id);
		AIAudioViewType groupTitleSelectValue = (AIAudioViewType)GroupTitleSelectValue;
		if (collect)
		{
			UserActCollectUtils.AddClickEvent("ai_recording", groupTitleSelectValue.ToString().ToLower(), "show");
		}
		IsEmptyContent = tuple.Item1 == HandleState.Handled && string.IsNullOrEmpty(tuple.Item3?.Raw);
		switch (groupTitleSelectValue)
		{
		case AIAudioViewType.Transcript:
			switch (tuple.Item1)
			{
			case HandleState.None:
			case HandleState.Handling:
				ViewStateType = AIAudioViewStateType.Loading;
				break;
			case HandleState.Handled:
				if (AIAudioService.CheckIfEmpty(tuple.Item3))
				{
					ViewStateType = AIAudioViewStateType.SimpleError;
					ErrorText = Utils.GetString("AINoTextDetected");
				}
				else
				{
					ViewStateType = AIAudioViewStateType.TranscriptResult;
				}
				break;
			case HandleState.Error:
			{
				ErrorText = AIAudioService.GetErrorTextByCode(tuple.Item3.ErrorCode, AIAudioViewType.Transcript);
				bool flag2 = AIAudioService.CheckErrorCanRetry(tuple.Item3.ErrorCode, inView: true);
				ViewStateType = (flag2 ? AIAudioViewStateType.NetworkError : AIAudioViewStateType.SimpleError);
				break;
			}
			}
			break;
		case AIAudioViewType.Summary:
			switch (tuple.Item2)
			{
			case HandleState.None:
				if (AIAudioService.CheckIfTooShort(tuple.Item3))
				{
					ViewStateType = AIAudioViewStateType.SimpleError;
					ErrorText = Utils.GetString("AIContentTooShortToSummary");
				}
				else
				{
					ViewStateType = AIAudioViewStateType.ToSummary;
				}
				break;
			case HandleState.Handling:
				ViewStateType = AIAudioViewStateType.Loading;
				break;
			case HandleState.Handled:
				ViewStateType = AIAudioViewStateType.SummaryResult;
				break;
			case HandleState.Error:
			{
				if (tuple.Item1 == HandleState.Error)
				{
					ViewStateType = AIAudioViewStateType.ToSummary;
					break;
				}
				ErrorText = AIAudioService.GetErrorTextByCode(tuple.Item3.ErrorCode, AIAudioViewType.Summary);
				bool flag = AIAudioService.CheckErrorCanRetry(tuple.Item3.ErrorCode, inView: true);
				ViewStateType = (flag ? AIAudioViewStateType.NetworkError : AIAudioViewStateType.SimpleError);
				break;
			}
			}
			break;
		}
	}

	private async Task LoadData()
	{
		AudioAttachmentAIResultModel audioAttachmentAIResultModel = await AudioAttachmentAIResultDao.GetByAttachmentId(model.id);
		if (audioAttachmentAIResultModel == null)
		{
			return;
		}
		originalItems.Clear();
		if (audioAttachmentAIResultModel.TimeStamp != null)
		{
			for (int i = 0; i < audioAttachmentAIResultModel.TimeStamp.Count; i++)
			{
				AIAudioOriginalItem aIAudioOriginalItem = new AIAudioOriginalItem();
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(audioAttachmentAIResultModel.TimeStamp[i][0]);
				if (timeSpan.TotalHours >= 1.0)
				{
					aIAudioOriginalItem.Time = timeSpan.ToString("hh\\:mm\\:ss");
				}
				else
				{
					aIAudioOriginalItem.Time = timeSpan.ToString("mm\\:ss");
				}
				if (i < audioAttachmentAIResultModel.Text.Count)
				{
					aIAudioOriginalItem.Content = audioAttachmentAIResultModel.Text[i];
				}
				originalItems.Add(aIAudioOriginalItem);
			}
			FlowDocument flowDocument = new FlowDocument();
			foreach (AIAudioOriginalItem originalItem in originalItems)
			{
				Paragraph paragraph = new Paragraph
				{
					Margin = default(Thickness)
				};
				if (!string.IsNullOrEmpty(originalItem.Time))
				{
					paragraph.Inlines.Add(new Run(originalItem.Time + " ")
					{
						Foreground = ThemeUtil.GetColor("BaseColorOpacity40"),
						FontSize = 12.0
					});
				}
				flowDocument.Blocks.Add(paragraph);
				paragraph = new Paragraph();
				paragraph.Margin = new Thickness(0.0, 6.0, 0.0, 16.0);
				paragraph.LineHeight = 23.8;
				if (!string.IsNullOrEmpty(originalItem.Content))
				{
					paragraph.Inlines.Add(new Run(originalItem.Content)
					{
						Foreground = ThemeUtil.GetColor("BaseColorOpacity90"),
						FontSize = 14.0
					});
				}
				flowDocument.Blocks.Add(paragraph);
			}
			flowDocument.PagePadding = new Thickness(0.0);
			AllTextDocument = flowDocument;
		}
		rawTopic = audioAttachmentAIResultModel.Topic;
		Topic = Utils.GetString("AITopicStart") + audioAttachmentAIResultModel.Topic;
		Summary = audioAttachmentAIResultModel.Summary;
	}

	private string GetText(bool markdown = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		switch ((AIAudioViewType)GroupTitleSelectValue)
		{
		case AIAudioViewType.Transcript:
			foreach (AIAudioOriginalItem originalItem in originalItems)
			{
				stringBuilder.AppendLine(originalItem.Time);
				stringBuilder.AppendLine(originalItem.Content);
			}
			break;
		case AIAudioViewType.Summary:
			if (!markdown)
			{
				stringBuilder.AppendLine(Topic);
			}
			else
			{
				stringBuilder.Append("**");
				stringBuilder.Append(Utils.GetString("AITopicStart"));
				stringBuilder.Append("**");
				stringBuilder.AppendLine(rawTopic);
			}
			stringBuilder.AppendLine(Summary);
			break;
		}
		return stringBuilder.ToString().TrimEnd();
	}

	[RelayCommand]
	public async Task StartSummary()
	{
		if ((await AIAudioService.CheckHandleState(model.id)).Item1 == HandleState.Handling)
		{
			Utils.Toast(Utils.GetString("AITranscribingTryAgain"));
			return;
		}
		ViewStateType = AIAudioViewStateType.Loading;
		AIAudioService.Summary(model.id, projectId, model.taskId);
	}

	[RelayCommand]
	public async Task Copy()
	{
		UserActCollectUtils.AddClickEvent("ai_recording", ((AIAudioViewType)GroupTitleSelectValue/*cast due to .constrained prefix*/).ToString().ToLower(), "copy_text");
		string text = GetText();
		try
		{
			Clipboard.SetText(text);
			Utils.Toast(Utils.GetString("Copied"));
		}
		catch
		{
		}
	}

	[RelayCommand]
	public async Task Insert()
	{
		UserActCollectUtils.AddClickEvent("ai_recording", ((AIAudioViewType)GroupTitleSelectValue/*cast due to .constrained prefix*/).ToString().ToLower(), "add_to_desc");
		TaskModel taskModel = await TaskDao.GetThinTaskById(model.taskId);
		if (taskModel != null)
		{
			string text = GetText(markdown: true);
			taskModel.content = taskModel.content + Environment.NewLine + text;
			await TaskService.SaveTaskContent(model.taskId, taskModel.content);
			SyncManager.TryDelaySync(3000);
			TaskChangeNotifier.NotifyTaskNeedScrollToBottom(model.taskId);
			OnClose?.Invoke();
		}
	}

	[RelayCommand]
	public async Task Retry()
	{
		switch ((AIAudioViewType)GroupTitleSelectValue)
		{
		case AIAudioViewType.Transcript:
			AIAudioService.Transcript(model.id, projectId, model.taskId);
			break;
		case AIAudioViewType.Summary:
			AIAudioService.Summary(model.id, projectId, model.taskId);
			break;
		}
		await OnGroupTitleChanged();
	}

	public void Dispose()
	{
		AIAudioService.OnToast = (Action<string, string>)Delegate.Remove(AIAudioService.OnToast, new Action<string, string>(OnServiceToast));
		AIAudioService.OnStateChange = (Action<string>)Delegate.Remove(AIAudioService.OnStateChange, new Action<string>(OnServiceResult));
		DataChangedNotifier.ThemeModeChanged -= DataChangedNotifier_IsDarkChanged;
	}
}
