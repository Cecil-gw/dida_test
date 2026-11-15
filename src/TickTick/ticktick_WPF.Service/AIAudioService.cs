using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Entity.Enums;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

internal class AIAudioService
{
	public static Action<string, string> OnToast { get; set; }

	public static Action<string> OnStateChange { get; set; }

	private static Task queryTask { get; set; }

	private static Dictionary<string, (string actId, TaskCompletionSource<AudioAttachmentAIResultModel> tcs)> transcriptings { get; set; } = new Dictionary<string, (string, TaskCompletionSource<AudioAttachmentAIResultModel>)>();

	private static List<string> summaryingList { get; set; } = new List<string>();

	private static Dictionary<string, bool> needShowExpandCache { get; set; } = new Dictionary<string, bool>();

	public static bool CheckShowExpandByCache(string attachmentId)
	{
		if (needShowExpandCache.TryGetValue(attachmentId, out var value))
		{
			return value;
		}
		return false;
	}

	public static async Task<(HandleState transcript, HandleState summary, AudioAttachmentAIResultModel model)> CheckHandleState(string attachmentId)
	{
		(bool, bool, AudioAttachmentAIResultModel) tuple = await CheckIfHasRecord(attachmentId);
		HandleState handleState = HandleState.None;
		if (tuple.Item1)
		{
			handleState = HandleState.Handled;
		}
		else if (transcriptings.ContainsKey(attachmentId) || summaryingList.Contains(attachmentId))
		{
			handleState = HandleState.Handling;
		}
		else if (tuple.Item3 != null && !string.IsNullOrEmpty(tuple.Item3.ErrorCode))
		{
			handleState = HandleState.Error;
		}
		HandleState handleState2 = HandleState.None;
		if (tuple.Item2)
		{
			handleState2 = HandleState.Handled;
		}
		else if (summaryingList.Contains(attachmentId))
		{
			handleState2 = HandleState.Handling;
		}
		else if (tuple.Item3 != null && !string.IsNullOrEmpty(tuple.Item3.ErrorCode))
		{
			handleState2 = HandleState.Error;
		}
		bool flag = handleState != HandleState.None || handleState2 != HandleState.None;
		if (flag && handleState != HandleState.None && handleState2 == HandleState.None && tuple.Item3 != null && string.IsNullOrEmpty(tuple.Item3.Raw))
		{
			flag = false;
		}
		needShowExpandCache[attachmentId] = flag;
		return (transcript: handleState, summary: handleState2, model: tuple.Item3);
	}

	public static async Task<(bool hasTranscript, bool hasSummary, AudioAttachmentAIResultModel model)> CheckIfHasRecord(string attachmentId)
	{
		bool hasTranscript = false;
		bool hasSummary = false;
		AudioAttachmentAIResultModel local = await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId);
		if (local != null)
		{
			if (!string.IsNullOrEmpty(local.TextJson) && !string.IsNullOrEmpty(local.TimeStampJson) && local.Raw != null)
			{
				hasTranscript = true;
			}
			if (!string.IsNullOrEmpty(local.Topic) && !string.IsNullOrEmpty(local.Summary))
			{
				hasSummary = true;
			}
			if (hasTranscript && hasSummary)
			{
				return (hasTranscript: hasTranscript, hasSummary: hasSummary, model: local);
			}
			if (hasTranscript && !hasSummary && CheckIfTooShort(local))
			{
				return (hasTranscript: hasTranscript, hasSummary: false, model: local);
			}
			if (!string.IsNullOrEmpty(local.ErrorCode) && !CheckErrorCanRetry(local.ErrorCode))
			{
				return (hasTranscript: hasTranscript, hasSummary: hasSummary, model: local);
			}
		}
		AudioAttachmentAIResultModel model = local;
		if (!transcriptings.ContainsKey(attachmentId) && !summaryingList.Contains(attachmentId))
		{
			List<AIQueryGenerResultDto> list = await Communicator.AIQueryGenerResult(attachmentId, null);
			if (list != null && list.Any())
			{
				model = new AudioAttachmentAIResultModel
				{
					AttachmentId = attachmentId,
					UserId = Utils.GetCurrentUserStr()
				};
				foreach (AIQueryGenerResultDto item in list)
				{
					string genType = item.genType;
					if (!(genType == "asr"))
					{
						if (genType == "summary")
						{
							hasSummary = true;
							AIVoiceSummaryDto aIVoiceSummaryDto = JsonConvert.DeserializeObject<AIVoiceSummaryDto>(item.generated);
							model.Topic = aIVoiceSummaryDto.topic;
							model.Summary = aIVoiceSummaryDto.summary;
						}
					}
					else
					{
						hasTranscript = true;
						AIVoiceTranscriptDto aIVoiceTranscriptDto = JsonConvert.DeserializeObject<AIVoiceTranscriptDto>(item.generated);
						model.Raw = aIVoiceTranscriptDto.raw;
						model.Language = aIVoiceTranscriptDto.language;
						model.Text = aIVoiceTranscriptDto.text;
						model.TimeStamp = aIVoiceTranscriptDto.timestamp;
					}
				}
				local = await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId);
				if (local == null)
				{
					local = model;
					await BaseDao<AudioAttachmentAIResultModel>.InsertAsync(local);
				}
				else
				{
					local.Raw = model.Raw;
					local.Language = model.Language;
					local.Text = model.Text;
					local.TimeStamp = model.TimeStamp;
					local.Topic = model.Topic;
					local.Summary = model.Summary;
					await BaseDao<AudioAttachmentAIResultModel>.UpdateAsync(local);
				}
				model = local;
			}
		}
		return (hasTranscript: hasTranscript, hasSummary: hasSummary, model: model);
	}

	public static bool CheckIfEmpty(AudioAttachmentAIResultModel local)
	{
		if (local == null)
		{
			return true;
		}
		if (string.IsNullOrEmpty(local.Raw) || local.TimeStamp == null || !local.TimeStamp.Any() || local.Text == null || !local.Text.Any())
		{
			return true;
		}
		return false;
	}

	public static bool CheckIfTooShort(AudioAttachmentAIResultModel local)
	{
		if (local == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(local.Raw))
		{
			return false;
		}
		bool result = false;
		if (local.Language == "zh")
		{
			if (local.Raw.Length < 50)
			{
				result = true;
			}
		}
		else if (local.Raw.Length < 150)
		{
			result = true;
		}
		return result;
	}

	public static bool CheckErrorCanRetry(string errorCode, bool inView = false)
	{
		switch (errorCode)
		{
		case "file_not_recognized":
		case "audio_file":
		case "unsupported_language":
		case "moderation":
			return false;
		default:
			if (inView && errorCode == "token_usage_exceed_limit")
			{
				return false;
			}
			return true;
		}
	}

	public static async Task Transcript(string attachmentId, string projectId, string taskId, TaskCompletionSource<AudioAttachmentAIResultModel> tcs = null)
	{
		if (transcriptings.ContainsKey(attachmentId))
		{
			return;
		}
		transcriptings[attachmentId] = (null, null);
		OnStateChange?.Invoke(attachmentId);
		if (await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId) == null)
		{
			AudioAttachmentAIResultModel model = new AudioAttachmentAIResultModel
			{
				AttachmentId = attachmentId,
				UserId = Utils.GetCurrentUserStr()
			};
			await BaseDao<AudioAttachmentAIResultModel>.InsertAsync(model);
		}
		(AITaskIdDto dto, string errorCode) result = await Communicator.AIVoiceTranscriptTask(attachmentId, projectId, taskId);
		if (result.dto == null)
		{
			UtilLog.Error("AIVoiceTranscriptTask " + attachmentId + " failed: " + result.errorCode);
			AudioAttachmentAIResultModel model = await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId);
			if (model != null)
			{
				model.ErrorCode = result.errorCode;
				await BaseDao<AudioAttachmentAIResultModel>.UpdateAsync(model);
			}
			transcriptings.Remove(attachmentId);
			tcs?.TrySetResult(null);
			string errorTextByCode = GetErrorTextByCode(result.errorCode, AIAudioViewType.Transcript);
			ToastNotify(attachmentId, errorTextByCode);
			OnStateChange?.Invoke(attachmentId);
		}
		else
		{
			transcriptings[attachmentId] = (result.dto.taskId, tcs);
			StartQuery();
		}
	}

	private static void StartQuery()
	{
		if (queryTask != null)
		{
			return;
		}
		queryTask = Task.Run(async delegate
		{
			new List<AIRecommendPropertyDto>();
			while (transcriptings.Any((KeyValuePair<string, (string actId, TaskCompletionSource<AudioAttachmentAIResultModel> tcs)> x) => !string.IsNullOrEmpty(x.Value.actId)))
			{
				List<string> toRemove = new List<string>();
				foreach (KeyValuePair<string, (string, TaskCompletionSource<AudioAttachmentAIResultModel>)> pair in transcriptings)
				{
					string attachmentId = pair.Key;
					string actTaskId = pair.Value.Item1;
					if (!string.IsNullOrEmpty(actTaskId))
					{
						(CommonDto<AIVoiceTranscriptDto> dto, string errorCode) result = await Communicator.AIVoiceTranscriptQueryTaskResult(actTaskId);
						if (result.dto == null)
						{
							UtilLog.Error("AIVoiceTranscriptQueryTaskResult " + attachmentId + " failed: " + result.errorCode);
							AudioAttachmentAIResultModel audioAttachmentAIResultModel = await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId);
							if (audioAttachmentAIResultModel != null)
							{
								audioAttachmentAIResultModel.ErrorCode = result.errorCode;
								await BaseDao<AudioAttachmentAIResultModel>.UpdateAsync(audioAttachmentAIResultModel);
							}
							string errorTextByCode = GetErrorTextByCode(result.errorCode, AIAudioViewType.Transcript);
							ToastNotify(attachmentId, errorTextByCode);
							toRemove.Add(attachmentId);
							pair.Value.Item2?.TrySetResult(null);
						}
						else
						{
							AIUploadResultType state = (AIUploadResultType)result.dto.code;
							switch (state)
							{
							case AIUploadResultType.Success:
							{
								AIVoiceTranscriptDto data = result.dto.data;
								AudioAttachmentAIResultModel local = await AudioAttachmentAIResultDao.GetByAttachmentId(attachmentId);
								if (local != null)
								{
									if (data != null)
									{
										local.Language = data.language;
										local.Raw = data.raw;
										local.Text = data.text;
										local.TimeStamp = data.timestamp;
									}
									local.ErrorCode = result.errorCode;
									await BaseDao<AudioAttachmentAIResultModel>.UpdateAsync(local);
								}
								else
								{
									local = new AudioAttachmentAIResultModel
									{
										AttachmentId = attachmentId,
										UserId = Utils.GetCurrentUserStr()
									};
									if (data != null)
									{
										local.Language = data.language;
										local.Raw = data.raw;
										local.Text = data.text;
										local.TimeStamp = data.timestamp;
									}
									local.ErrorCode = result.errorCode;
									await BaseDao<AudioAttachmentAIResultModel>.InsertAsync(local);
								}
								if (string.IsNullOrEmpty(result.errorCode))
								{
									if (CheckIfEmpty(local))
									{
										ToastNotify(attachmentId, Utils.GetString("AINoTextDetected"));
									}
									else
									{
										ToastNotify(attachmentId, Utils.GetString("AITranscriptionCompleted"));
									}
								}
								pair.Value.Item2?.TrySetResult(local);
								break;
							}
							case AIUploadResultType.InvalidId:
								ToastNotify(attachmentId, Utils.GetString("AIRequestFailedToast"));
								UtilLog.Error("AIVoiceTranscriptQueryTaskResult invalid id: " + actTaskId + ", attachmentId:" + attachmentId);
								break;
							case AIUploadResultType.HandlingFailed:
								ToastNotify(attachmentId, Utils.GetString("AIRequestFailedToast"));
								UtilLog.Error("AIVoiceTranscriptQueryTaskResult handling failed: " + actTaskId + ", attachmentId:" + attachmentId);
								break;
							}
							if (state != AIUploadResultType.Handling)
							{
								toRemove.Add(attachmentId);
								pair.Value.Item2?.TrySetResult(null);
							}
						}
					}
				}
				if (toRemove.Any())
				{
					foreach (string item in toRemove)
					{
						transcriptings.Remove(item);
						OnStateChange?.Invoke(item);
					}
				}
				if (!transcriptings.Any((KeyValuePair<string, (string actId, TaskCompletionSource<AudioAttachmentAIResultModel> tcs)> x) => !string.IsNullOrEmpty(x.Value.actId)))
				{
					break;
				}
				await Task.Delay(5000);
			}
			queryTask = null;
		});
	}

	public static async Task<AudioAttachmentAIResultModel> Summary(string attachmentId, string projectId, string taskId)
	{
		(HandleState, HandleState, AudioAttachmentAIResultModel) tuple = await CheckHandleState(attachmentId);
		if (summaryingList.Contains(attachmentId))
		{
			return null;
		}
		summaryingList.Add(attachmentId);
		OnStateChange?.Invoke(attachmentId);
		try
		{
			if (tuple.Item1 == HandleState.Handling)
			{
				ToastNotify(attachmentId, Utils.GetString("AITranscribingTryAgain"));
				return null;
			}
			AudioAttachmentAIResultModel local = tuple.Item3;
			if (tuple.Item1 != HandleState.Handled)
			{
				TaskCompletionSource<AudioAttachmentAIResultModel> tcs = new TaskCompletionSource<AudioAttachmentAIResultModel>();
				await Transcript(attachmentId, projectId, taskId, tcs);
				local = await tcs.Task;
			}
			if (local == null)
			{
				return null;
			}
			string content = local.Raw;
			if (string.IsNullOrEmpty(content))
			{
				return null;
			}
			if (CheckIfTooShort(local))
			{
				return local;
			}
			(AIVoiceSummaryDto dto, string errorCode) result = await Communicator.AIVoiceSummary(content, attachmentId);
			if (result.dto == null)
			{
				UtilLog.Error("AIVoiceSummary " + attachmentId + " failed: " + result.errorCode);
				string errorTextByCode = GetErrorTextByCode(result.errorCode, AIAudioViewType.Summary);
				ToastNotify(attachmentId, errorTextByCode);
			}
			var (aIVoiceSummaryDto, _) = result;
			if (local != null)
			{
				if (aIVoiceSummaryDto != null)
				{
					local.Topic = aIVoiceSummaryDto.topic;
					local.Summary = aIVoiceSummaryDto.summary;
				}
				local.ErrorCode = result.errorCode;
				await BaseDao<AudioAttachmentAIResultModel>.UpdateAsync(local);
			}
			else
			{
				local = new AudioAttachmentAIResultModel
				{
					UserId = Utils.GetCurrentUserStr(),
					AttachmentId = attachmentId,
					Raw = content
				};
				if (aIVoiceSummaryDto != null)
				{
					local.Topic = aIVoiceSummaryDto.topic;
					local.Summary = aIVoiceSummaryDto.summary;
				}
				local.ErrorCode = result.errorCode;
				await BaseDao<AudioAttachmentAIResultModel>.InsertAsync(local);
			}
			if (string.IsNullOrEmpty(result.errorCode))
			{
				ToastNotify(attachmentId, Utils.GetString("AISummaryCompleted"));
			}
			return local;
		}
		finally
		{
			summaryingList.Remove(attachmentId);
			OnStateChange?.Invoke(attachmentId);
		}
	}

	public static string GetErrorTextByCode(string errorCode, AIAudioViewType viewType)
	{
		string text = null;
		if (errorCode == null)
		{
			goto IL_014d;
		}
		int length = errorCode.Length;
		if (length <= 10)
		{
			if (length != 8)
			{
				if (length != 10)
				{
					goto IL_014d;
				}
				char c = errorCode[0];
				if (c != 'a')
				{
					if (c != 'm')
					{
						if (c != 'n' || !(errorCode == "no_network"))
						{
							goto IL_014d;
						}
						text = "NetworkException";
					}
					else
					{
						if (!(errorCode == "moderation"))
						{
							goto IL_014d;
						}
						text = "AIContentSensitiveInfo";
					}
				}
				else
				{
					if (!(errorCode == "audio_file"))
					{
						goto IL_014d;
					}
					text = "AIAudioProcessFailed";
				}
			}
			else
			{
				if (!(errorCode == "time_out"))
				{
					goto IL_014d;
				}
				text = "AIConnectTimeout";
			}
		}
		else
		{
			if (length == 19)
			{
				char c = errorCode[0];
				if (c != 'f')
				{
					if (c != 'p')
					{
						if (c == 'r' && errorCode == "rate_limit_exceeded")
						{
							text = "AISystemBusy";
							goto IL_0162;
						}
					}
					else if (errorCode == "parse_field_missing")
					{
					}
				}
				else if (errorCode == "file_not_recognized")
				{
					text = "AIUnsupportedAudioFormat";
					goto IL_0162;
				}
				goto IL_014d;
			}
			if (length != 20)
			{
				if (length != 24 || !(errorCode == "token_usage_exceed_limit"))
				{
					goto IL_014d;
				}
				text = "AIUsageExceedLimitToast";
			}
			else
			{
				if (!(errorCode == "unsupported_language"))
				{
					goto IL_014d;
				}
				text = "AILanguageNotSupport";
			}
		}
		goto IL_0162;
		IL_014d:
		text = ((viewType != AIAudioViewType.Transcript && viewType == AIAudioViewType.Summary) ? "AISummaryFailed" : "AITranscriptionFailed");
		goto IL_0162;
		IL_0162:
		return Utils.GetString(text);
	}

	private static void ToastNotify(string attachmentId, string text)
	{
		if (OnToast != null)
		{
			OnToast(attachmentId, text);
		}
		else
		{
			Utils.Toast(text);
		}
	}
}
