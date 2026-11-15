using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Update;

public class NewVersonViewModel : BaseViewModel
{
	public string NewVersion { get; set; }

	public string OldVersion { get; set; }

	public string ReleaseNote { get; set; }

	public string DownloadPath { get; set; }

	public string[] ForceVersions { get; set; }

	public PackageSizeModel SizeModel { get; set; }

	public UpdateModel UpdateModel { get; set; }
}
