using System.Collections.Generic;

namespace ticktick_WPF.Entity.Dto;

internal class UserBindingInfo
{
	public class ThirdSideBind
	{
		public string openId { get; set; }

		public int siteId { get; set; }

		public string nickName { get; set; }

		public string email { get; set; }
	}

	public List<ThirdSideBind> thirdSiteBinds { get; set; }

	public bool fakedEmail { get; set; }

	public string email { get; set; }
}
