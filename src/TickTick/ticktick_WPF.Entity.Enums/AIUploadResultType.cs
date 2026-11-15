namespace ticktick_WPF.Entity.Enums;

public enum AIUploadResultType
{
	Success = 0,
	NoTask = 1,
	RequestFailed = 500,
	Handling = 10000,
	InvalidId = 10001,
	HandlingFailed = 10002
}
