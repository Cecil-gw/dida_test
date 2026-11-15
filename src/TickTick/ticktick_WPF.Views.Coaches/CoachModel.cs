using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Coaches.CoachCommands;

namespace ticktick_WPF.Views.Coaches;

public class CoachModel
{
	public string Title { get; set; }

	public string Info { get; set; }

	public string GifPath { get; set; }

	public BitmapImage Image { get; set; }

	public CoachModel Next { get; set; }

	public CoachModel Pre { get; set; }

	public string CommandTitle { get; set; }

	public ICommand Command { get; set; }

	public static async Task<CoachModel> GetTimeLineCoach()
	{
		CoachModel first = await BuildTimeLineGuideModel(1);
		CoachModel second = await BuildTimeLineGuideModel(2);
		CoachModel coachModel = await BuildTimeLineGuideModel(3);
		first.Next = second;
		second.Pre = first;
		second.Next = coachModel;
		coachModel.Pre = second;
		coachModel.Command = new TimelineCoachCommand();
		coachModel.CommandTitle = Utils.GetString("LearnMore");
		return first;
		static async Task<CoachModel> BuildTimeLineGuideModel(int i)
		{
			string text = string.Format("TimelineGuide{0}{1}.gif", Utils.IsCn() ? "_cn_" : "_en_", i);
			string gifPath = AppPaths.ResourceDir + text;
			CoachModel model = new CoachModel
			{
				Title = Utils.GetString("TimeLineGuideTitle" + i),
				Info = Utils.GetString("TimeLineGuideInfo" + i),
				GifPath = gifPath
			};
			string remotePath = "https://" + BaseUrl.GetPullDomain() + "/windows/static/" + text;
			await IOUtils.CheckResourceExist(AppPaths.ResourceDir, text, remotePath);
			return model;
		}
	}
}
