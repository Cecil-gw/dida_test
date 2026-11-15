using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class ShareAddUsersMode
{
	public List<ShareUsersError> errors { get; set; }

	public List<ShareAddUserMode> records { get; set; }
}
