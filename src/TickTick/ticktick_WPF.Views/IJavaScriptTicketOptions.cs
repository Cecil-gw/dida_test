using System.Runtime.InteropServices;

namespace ticktick_WPF.Views;

[ComVisible(true)]
[Guid("71B5512B-14BE-4CC6-9D7C-31B45B9DF78A")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IJavaScriptTicketOptions
{
	[DispId(1)]
	void toastForTicketWithMessage(string message);

	[DispId(2)]
	void uploadLogsForTicketWithID(string ticketId, string tt);

	[DispId(3)]
	string getXDeviceInfoForTicket();

	[DispId(4)]
	void setDefaultMatrixRule(string ruleType);

	[DispId(5)]
	void navigateUrl(string url);

	[DispId(6)]
	void navigateUrlWithToken(string url);

	[DispId(7)]
	void submitHolidayCalendar(string payload);

	[DispId(8)]
	void applyProjectTemplate(string projectTemplateId);

	[DispId(9)]
	void closePage();
}
