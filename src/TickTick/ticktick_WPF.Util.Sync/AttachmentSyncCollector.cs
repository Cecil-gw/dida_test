using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class AttachmentSyncCollector
{
	public static async Task CollectRemoteAttachments(TaskModel serverTask, AttachmentSyncBean attachmentSyncBean)
	{
		List<AttachmentModel> list = await AttachmentDao.GetTaskAttachments(serverTask.id);
		bool flag = serverTask.Attachments != null && serverTask.Attachments.Length != 0;
		bool flag2 = list != null && list.Count > 0;
		AttachmentModel[] attachments;
		if (flag && !flag2)
		{
			attachments = serverTask.Attachments;
			foreach (AttachmentModel attachmentModel in attachments)
			{
				if (string.IsNullOrEmpty(attachmentModel.path))
				{
					UtilLog.Info("NullPathRemoteAttachment : " + attachmentModel.id);
				}
				attachmentSyncBean.Added.Add(AttachmentTransfer.ConvertServerToLocal(attachmentModel, serverTask.id));
			}
			return;
		}
		if (!flag && flag2)
		{
			foreach (AttachmentModel item in list)
			{
				if (item.sync_status == 2.ToString())
				{
					attachmentSyncBean.Deleted.Add(item);
				}
			}
			return;
		}
		if (!flag)
		{
			return;
		}
		Dictionary<string, AttachmentModel> localAttachmentMap = GetLocalAttachmentMap(list);
		attachments = serverTask.Attachments;
		foreach (AttachmentModel attachmentModel2 in attachments)
		{
			if (string.IsNullOrEmpty(attachmentModel2?.id))
			{
				continue;
			}
			if (localAttachmentMap.ContainsKey(attachmentModel2.id))
			{
				AttachmentModel attachmentModel3 = localAttachmentMap[attachmentModel2.id];
				if (attachmentModel3.status != attachmentModel2.status || attachmentModel3.path != attachmentModel2.path)
				{
					attachmentSyncBean.Updated.Add(AttachmentTransfer.ConvertServerToLocal(attachmentModel2, serverTask.id));
				}
				localAttachmentMap.Remove(attachmentModel2.id);
			}
			else
			{
				attachmentModel2.fileType = AttachmentProvider.GetFileType(attachmentModel2.fileName).ToString();
				attachmentSyncBean.Added.Add(AttachmentTransfer.ConvertServerToLocal(attachmentModel2, serverTask.id));
			}
		}
		foreach (AttachmentModel value in localAttachmentMap.Values)
		{
			if (value.sync_status == 2.ToString())
			{
				attachmentSyncBean.Deleted.Add(value);
			}
		}
	}

	public static Dictionary<string, AttachmentModel> GetLocalAttachmentMap(List<AttachmentModel> attachments)
	{
		Dictionary<string, AttachmentModel> dictionary = new Dictionary<string, AttachmentModel>();
		if (attachments != null && attachments.Count > 0)
		{
			foreach (AttachmentModel attachment in attachments)
			{
				if (!string.IsNullOrEmpty(attachment.id) && !dictionary.ContainsKey(attachment.id))
				{
					dictionary.Add(attachment.id, attachment);
				}
			}
		}
		return dictionary;
	}
}
