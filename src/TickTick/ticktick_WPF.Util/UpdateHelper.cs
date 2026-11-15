using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public static class UpdateHelper
{
	public static async Task<UpdateModel> CheckUpdate()
	{
		if (Utils.IsNetworkAvailable())
		{
			try
			{
				string text = await Communicator.CheckUpdate();
				if (!string.IsNullOrWhiteSpace(text))
				{
					UpdateModel updateModel = new UpdateModel
					{
						downLoadUri = text
					};
					string[] source = text.Split('/');
					updateModel.fileName = source.Last();
					updateModel.versionNum = double.Parse(updateModel.fileName.Split('.').First().Split('_')
						.Last());
					ReleaseNoteModel releaseNoteModel = await Utils.GetReleaseNote();
					if (releaseNoteModel != null)
					{
						if (DateTime.TryParseExact(releaseNoteModel.release_date.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
						{
							updateModel.publishDate = result;
						}
						updateModel.forceUpdate = releaseNoteModel.IsForced();
						updateModel.greyForced = releaseNoteModel.IsGreyForced();
						updateModel.forceVersions = releaseNoteModel.forceVersions;
						updateModel.releaseNote = releaseNoteModel;
					}
					return updateModel;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}

	public static bool NeedUpgradeVersion(double next, double current)
	{
		return next > current;
	}

	public static int GetGreyDate(DateTime publishDate)
	{
		int num = new Random().Next(1, 100);
		int num2 = ((num > 5) ? ((num <= 10) ? 1 : ((num <= 20) ? 2 : ((num <= 50) ? 3 : 4))) : 0);
		return DateUtils.GetDateNum(publishDate.AddDays(num2));
	}

	internal static void TryDeleteOriginPackage()
	{
		Task.Run(delegate
		{
			if (Directory.Exists(AppPaths.PackageDir))
			{
				string[] files = Directory.GetFiles(AppPaths.PackageDir);
				for (int i = 0; i < files.Length; i++)
				{
					FileInfo fileInfo = new FileInfo(files[i]);
					if ((DateTime.Today - fileInfo.LastWriteTime).TotalDays > 40.0)
					{
						fileInfo.Attributes = FileAttributes.Normal;
						fileInfo.Delete();
					}
				}
			}
		});
	}
}
