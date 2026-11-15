using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Models;

public static class SharePhoneInviteErrorHelper
{
	public static SharePhoneInviteOutcome Classify(IEnumerable<ShareUsersError> errors, int successCount, bool hasOtherFailures = false)
	{
		List<ShareUsersError> list = errors?.Where((ShareUsersError e) => e != null && (e.errorCode == "user_not_exist" || e.errorCode == "unsupported_phone_num")).ToList() ?? new List<ShareUsersError>();
		if (list.Count == 0)
		{
			return SharePhoneInviteOutcome.None;
		}
		if (successCount > 0)
		{
			return SharePhoneInviteOutcome.Partial;
		}
		if (hasOtherFailures)
		{
			return SharePhoneInviteOutcome.AllUnable;
		}
		if (list.All((ShareUsersError e) => e.errorCode == "user_not_exist"))
		{
			return SharePhoneInviteOutcome.AllUnregistered;
		}
		if (list.All((ShareUsersError e) => e.errorCode == "unsupported_phone_num"))
		{
			return SharePhoneInviteOutcome.AllInvalid;
		}
		return SharePhoneInviteOutcome.AllUnable;
	}
}
