namespace ticktick_WPF.Util;

public static class Api
{
	public const string STATUS_API = "/about/status";

	public const string LOGIN_API = "/api/v2/user/signon";

	public const string SIGNOUT_API = "/api/v2/user/signout";

	public const string USERSTATUS_API = "/api/v2/user/status";

	public const string INVITECODE_API = "/api/v2/user/signup/inviteCode";

	public const string SIGNUP_API = "/api/v2/user/signup";

	public const string REQUESTRESTPASSWORD_API = "/sign/requestRestPassword?username=";

	public const string LIMITS_API = "/api/v2/configs/limits";

	public const string ISNEWUSER = "/api/v2/user/isJustRegistered";

	public const string ISUSERNEWSTATE = "/api/v3/subscribe/free_trial/user";

	public const string USERINFO_API = "/api/v2/user/profile";

	public const string BATCHCHECK_API = "/api/v3/batch/check/";

	public const string BATCHCHECKTRASH_API = "/api/v2/project/all/trash/pagination?start={0}&limit={1}";

	public const string PULLTRASH_API = "/api/v2/project/all/trash/page?next={0}&limit={1}&type={2}";

	public const string TRASHRESTORE_API = "/api/v2/trash/restore";

	public const string NOTIFICATION_API = "/api/v2/notification";

	public const string NOTIFICATION_COUNT_API = "/api/v2/notification/unread";

	public const string PROJECT_API = "/api/v2/project";

	public const string PROJECT_ACCEPT_SHARE_API = "/api/v2/project/{0}/share/accept/{1}?actionStatus={2}";

	public const string SHAREQUOTA_API = "/api/v2/project/{0}/share/check-quota";

	public const string SHARECONTACTS_API = "/api/v2/share/shareContacts";

	public const string PHONE_CHECK_API = "/api/v2/phone/check";

	public const string AllSHARECONTACTS_API = "/api/v2/share/contacts";

	public const string USERPUBLICPROFILES_API = "/pub/api/v2/userPublicProfiles";

	public const string PROJECTGROUP_API = "/api/v2/projectGroup";

	public const string REMINDERTOPAY_API = "/api/v2/project/{0}/reminderToPay";

	public const string QQ_openID = "https://graph.qq.com/oauth2.0/me?access_token={0}";

	public const string THIRD_CHANGE_PASSWORD = "/api/v2/user/third/changePassword";

	public const string THIRDLOGIN_API = "/api/v2/user/sign/OAuth2?site={0}&accessToken={1}&uId={2}";

	public const string WXTHIRDLOGIN_API = "/api/v2/user/sign/wechat/validate?code={0}&state=";

	public const string WeiBoTHIRDLOGIN_API = "/api/v2/user/sign/weibo/validate?code={0}";

	public const string FBTHIRDLOGIN_API = "/api/v2/user/sign/facebook/validate?access_token={0}";

	public const string TWTHIRDLOGIN_API = "/api/v2/user/sign/twitter?accessToken={0}&accessTokenSecret={1}";

	public const string BATCH_UPDATE_PROJECT_GROUP = "/api/v2/batch/projectGroup";

	public const string BATCH_UPDATE_PROJECT = "/api/v2/batch/project";

	public const string BATCH_UPDATE_TASK = "/api/v2/batch/task";

	public const string BATCH_UPDATE_TASK_DELETEFOREVER = "/api/v2/task?deleteforever=true";

	public const string BATCH_UPDATE_TASK_PROJECT = "/api/v2/batch/taskProject";

	public const string BATCH_UPDATE_TASK_ORDER = "/api/v2/batch/taskOrder";

	public const string BATCH_UPDATE_FILTER = "/api/v2/batch/filter";

	public const string MATRIX_PROFILES = "/api/v2/matrixProfiles";

	public const string BATCH_UPDATE_MATRIX_PROFILE = "/api/v2/matrixProfiles/batch";

	public const string BATCH_UPDATE_TAG = "/api/v2/batch/tag";

	public const string BATCH_UPDATE_TASK_TRASHEMPTY = "/api/v2/trash/empty";

	public const string CLEANUPTRASH = "/api/v2/trash/cleanUp";

	public const string TASK_ASSIGN = "/api/v2/task/assign";

	public const string SIGNONTOKEN = "/api/v2/user/requestSignOnToken";

	public const string BATCH_UPDATE_ORDER = "/api/v2/batch/order";

	public const string BATCH_UPDATE_ORDER_V3 = "/api/v3/batch/order";

	public const string HANDLEAPPLICATION = "/api/v2/project/collaboration/{0}?notificationId={1}";

	public const string GET_COMPLETED_TASKS_API = "/api/v2/project/{0}/completed/?from={1}&to={2}&limit={3}";

	public const string GET_ALL_COMPLETED_TASKS_API = "/api/v2/project/all/completed/?from={0}&to={1}&limit={2}";

	public const string GET_CLOSED_TASKS_API = "/api/v2/project/{0}/closed/?from={1}&to={2}&limit={3}";

	public const string COMMENT_ATTACHMENT_API = "/api/v1/attachment/comment/{0}/{1}/{2}";

	public const string COUNTDOWN_ATTACHMENT_API = "/api/v1/attachment/countdown?id={0}&attachmentId={1}";

	public const string ATTACHMENT_API = "/api/v1/attachment/{0}/{1}/{2}";

	public const string UPLOAD_ATTACHMENT_API = "/api/v1/attachment/upload/{0}/{1}/{2}";

	public const string UPLOAD_COMMENT_ATTACHMENT_API = "/api/v1/attachment/upload/comment/{0}/{1}";

	public const string UPLOAD_COUNTDOWN_ATTACHMENT_API = "/api/v1/attachment/upload/countdown?attachmentId={0}";

	public const string DAILY_LIMIT_API = "/api/v1/attachment/dailyLimit";

	public const string GET_COMMENTS_API = "/api/v2/project/{0}/task/{1}/comments";

	public const string DELETE_COMMENT_API = "/api/v2/project/{0}/task/{1}/comment/{2}";

	public const string ADD_COMMENT_API = "/api/v2/project/{0}/task/{1}/comment";

	public const string TRAIL_EXPIRED_API = "/api/v2/uwu/et";

	public const string GET_HOLIDAY_API = "/pub/api/v1/calendar/holiday/cn/recent";

	public const string POMO_SETTINGS_API = "/api/v2/user/preferences/pomodoro";

	public const string FOCUS_OPTION_API = "/focus/batch/focusOp";

	public const string STATISTICS_API = "/api/v2/pomodoros/statistics/generalForDesktop";

	public const string FOCUSTIMELINE_API = "/api/v2/pomodoros/timeline?to={0}";

	public const string RECENTFOCUSTASK = "/api/v2/pomodoro/bind/recent";

	public const string TIMER = "/api/v2/timer";

	public const string TIMERTIMELINE = "/api/v2/timer/timeline/{0}?to={1}";

	public const string QUERY_POMO_REC_FILTER = "/api/v2/pomodoro/filter";

	public const string POMO_REC_DELETE_MULTI = "/api/v2/pomodoro";

	public const string POMO_REC_DELETE_ALL = "/api/v2/pomodoro/all";

	public const string POMO_CHECK_CHANGE_MONTH_API = "/api/v2/pomodoro/record/{0}";

	public const string TASK_ACTIVITY_API = "/api/v1/project/{0}/task/{1}/activity";

	public const string PROJECT_ACTIVITY_API = "/api/v1/project/{0}/activity";

	public const string TAG_API = "/api/v2/tag";

	public const string CALENDAR_SUBSCRIPTION = "/api/v2/calendar/subscription";

	public const string UNSUBSCRIBE_CALENDAR_API = "/api/v2/calendar/unsubscribe";

	public const string SUBSCRIBE_CALENDAR_API = "/api/v2/calendar/subscribe";

	public const string BIND_CALENDAR_ACCOUNTS = "/api/v2/calendar/bind/accounts";

	public const string BIND_CALENDAR_EVENTS = "/api/v2/calendar/bind/events/all";

	public const string BIND_CALENDAR = "/api/v2/calendar/bind";

	public const string BIND_CALENDAR_ACCOUNT_EVENTS = "/api/v2/calendar/bind/events/{0}";

	public const string BIND_EXCHANGE_EVENTS = "/api/v2/calendar/bind/events/exchange/{0}";

	public const string BIND_OUTLOOK_ACCOUNT_EVENTS = "/api/v2/calendar/bind/events/outlook";

	public const string BIND_CALENDAR_ACCOUNT_EVENTS_V4 = "/api/v4/calendar/bind/events/{0}";

	public const string CHECK_EXCHANGE_ACCOUNT = "/api/v2/calendar/exchange/check?account={0}";

	public const string CACHE_EVENTS = "/api/v2/calendar/cache/{0}";

	public const string BIND_CALENDAR_SORT = "/api/v2/calendar/subscribe/sortOrder";

	public const string MERGE_TAG = "/api/v2/tag/merge";

	public const string USER_SETTINGS = "/api/v2/user/preferences/settings?includeWeb=true";

	public const string USER_PREFERENCE = "/api/v2/user/preferences/ext";

	public const string DUPLICATE_PROJECT = "/api/v2/project/{0}/duplicate";

	public const string RESENT_VERIFY_EMAIL = "/api/v2/user/resentVerifyEmail";

	public const string TASK_ATTEND = "/api/v2/task-attend/{0}/attendees?taskId={1}";

	public const string DELETE_TASK_ATTEND = "/api/v2/task-attend/{0}/delete/{1}";

	public const string CREATE_ATTEND = "/api/v2/task-attend/invitation/create?projectId={0}&taskId={1}";

	public const string OPEN_INVITATION = "/pub/api/v2/task-attend/invitation/{0}/open";

	public const string ECHO_INVITATION = "/api/v2/task-attend/invitation/{0}/response?status={1}";

	public const string REMOVE_ATTENDEE = "/api/v2/task-attend/{0}/attendees/{1}?userCode={2}";

	public const string REMOVE_ATTEND = "/api/v2/task-attend/{0}/attend/{1}";

	public const string BATCH_UPDATE_COLUMN = "/api/v2/column";

	public const string PROJECT_COLUMN = "/api/v2/column/project/{0}";

	public const string PULL_REMOTE_COLUMN = "/api/v2/column?from={0}";

	public const string ORDER_BY_PROJECT = "/api/v2/task/order-by-project/{0}";

	public const string GET_TEAMS = "/api/v2/teams";

	public const string CREATE_TEAM = "/api/v2/team";

	public const string TEAM_CONTACTS = "/api/v2/team/{0}/share/shareContacts";

	public const string TEAM_MENBERS = "/api/v2/team/{0}/members";

	public const string TRANSFER_PROJECT = "/api/v2/project/{0}/transfer?toUserCode={1}";

	public const string ACCEPT_TEAM_APPLY = "/api/v2/team/collaboration/accept?notificationId={0}";

	public const string REFUSE_TEAM_APPLY = "/api/v2/team/collaboration/refuse?notificationId={0}";

	public const string BATCH_EVENTS = "/api/v2/calendar/bind/events/batch";

	public const string BATCH_EVENTS_V4 = "/api/v4/calendar/bind/events/batch";

	public const string TEMPLATEAPI = "/api/v2/templates";

	public const string PROJECT_TEMPLATES = "/api/v2/projectTemplates/all";

	public const string APPLY_PROJECT_TEMPLATE = "/api/v2/templates/project/{0}/apply";

	public const string HABITS = "/api/v2/habits";

	public const string HABITS_CHECKINS = "/api/v2/habitCheckins";

	public const string BATCHHABITS_CHECKINS = "/api/v2/habitCheckins/query";

	public const string HABITS_RECORDS = "/api/v2/habitRecords";

	public const string HABIT_SECTIONS = "/api/v2/habitSections";

	public const string BATCH_HABIT_SECTIONS = "/api/v2/habitSections/batch";

	public const string BATCH_HABITS_CHECKINS = "/api/v2/habitCheckins/batch";

	public const string GET_TASK = "/api/v2/task/{0}?projectId={1}&withChildren={2}";

	public const string PUSH_TASKPARENT = "/api/v2/batch/taskParent";

	public const string HABIT_SETTINGS_API = "/api/v2/user/preferences/habit";

	public const string BATCH_HABITS = "/api/v2/habits/batch";

	public const string REGISTER_PUSH = "/api/v2/push/register";

	public const string SEARCHTASK = "/api/v2/search/task?keywords={0}";

	public const string SEARCHALL = "/api/v2/search/all?keywords={0}";

	public const string TICKETDETAIL = "/v3/tickets/detail?id=";

	public const string TICKETS = "/v3/tickets/";

	public const string MATRIXEXAMPLE = "/webview/matrixRule";

	public const string COURSESCHEDULES = "/api/v1/course/timetable";

	public const string COURSESCHEDULE = "/api/v1/course/timetable/{0}";

	public const string HABITEXPORT = "/api/v2/data/export/habits";

	public const string USERBINDINFO = "/api/v2/user/userBindingInfo";

	public const string REMINDERDELAY = "/api/v2/reminder/delay";

	public const string FEEDSCODE = "/api/v2/calendar/feeds/code";

	public const string FEEDSCANCEL = "/api/v2/calendar/feeds/cancel";

	public const string MOVECOLUMN = "/api/v2/batch/columnProject";

	public const string TAGINSHARETASK = "/api/v2/tag/check/shareTask";

	public const string ABTEST_GROUP = "/datacollect/pub/v1/ab/group";

	public const string GUIDEPROJECT = "/pub/api/v2/guide/project?p={0}";

	public const string GUIDETASK = "/pub/api/v2/guide/task";

	public const string ABTEST_RESULT = "/datacollect/pub/v1/ab/group/result";

	public const string PAYMENT_V1 = "/api/v1/payment";

	public const string SUBSCRIBE_SPEC = "/pub/api/v2/subscribe/subscribe_spec";

	public const string ORDER_SPEC = "/pub/api/v2/payment/order_spec";

	public const string CHECK_PAYMENT_ORDER = "/api/v1/payment/check/";

	public const string CANCEL_PAYMENT_SUBS = "/api/v1/payment/cancel/";

	public const string SUBSCRIBE_LIST = "/api/v2/subscribe/list";

	public const string SUBSCRIBE_INFO = "/api/v2/subscribe/info";

	public const string CHECK_BLACK_LIST = "/api/v2/payment/isBlacklist";

	public const string CALENDARCONNECT = "/api/v2/calendar/connect?state={0}&code={1}&channel=win&redirectUrl={2}";

	public const string PROJECTBINDCALENDAR = "/api/v2/calendar/{0}/bind/project";

	public const string CALENDARTHIRDACCOUNTS = "/api/v2/calendar/third/accounts";

	public const string TASK_GOOGLE_EVENT_INFO_SYNC = "/api/v1/google-event-infos/sync";

	public const string DELETECONNECTACCOUNT = "/api/v2/calendar/connect/{0}";

	public const string UNBINDPROJECTCALENDAR = "/api/v2/calendar/{0}/bind/project?id={1}";

	public const string CREATECALENDAR = "/api/v2/calendar/{0}/create";

	public const string CALENDAR_SET_VISIBLE = "/api/v2/calendar/{0}/{1}/show?visibility={2}&isMobile=false";

	public const string CALENDAR_URL_SET_VISIBLE = "/api/v2/calendar/subscription/{0}/show?visibility={1}&isMobile=false";

	public const string BIND_ACCOUNT_CALENDAR_DESC = "/api/v2/calendar/{0}/desc";

	public const string CALENDAR_BATCH_SUBSCRIBE = "/api/v2/calendar/batch/subscribe";

	public const string CHECKPHONESIGNUP = "/pub/api/v2/user/check/phone";

	public const string CHECKEMAILSINEUP = "/pub/api/v2/user/check/email";

	public const string PHONESIGNUP = "/signup/validate/phone/{0}";

	public const string SENDVERIGYCODE = "/api/v2/user/sms/signup/code";

	public const string PUBCAPTCHA = "/pub/captcha";

	public const string ArchiveProject = "/api/v2/project/{0}/close?closed={1}&toAll={2}";

	public const string GETMFA = "/api/v2/user/mfa/setting";

	public const string GETSIGNMFA = "/api/v2/user/sign/mfa/setting";

	public const string POSTMFACODE = "/api/v2/user/mfa/code";

	public const string POSTSIGNMFACODE = "/api/v2/user/sign/mfa/code";

	public const string POSTSIGNVERIFYMFA = "/api/v2/user/sign/mfa/code/verify";

	public const string MAILTASKCODENEW = "/api/v2/settings/mail/task/code/new";

	public const string MAILTASKCODE = "/api/v2/settings/mail/task/code";

	public const string POSTTELEGRAMBINDCODE = "/api/v2/third/telegram/bind/code";

	public const string TAGCOMPLETEDTASK = "/api/v2/tag/completedTask";

	public const string POSTNOTIONSYNC = "/api/v2/connect/notion";

	public const string CountdownUrl = "/api/v2/countdown/batch";

	public const string CountdownListUrl = "/api/v2/countdown/list";

	public const string CountdownAttachmentUploadUrl = "/api/v1/attachment/countdown";

	public const string CountdownAttachmentDownloadUrl = "/api/v1/attachment/countdown";

	public const string UserRegTime = "/api/v2/user/regTime";

	public const string TaskActivityCountUrl = "/api/v2/task/activity/count";

	public const string AllTaskActivityCountUrl = "/api/v2/task/activity/count/all";

	public const string AiTaskSceneUrl = "/ai/api/v2/task/scene";

	public const string AiTaskAdviceUrl = "/ai/api/v2/task/advice";

	public const string AiTaskDecompUrl = "/ai/api/v2/task/decomposition";

	public const string AiTaskImproveUrl = "/ai/api/v2/task/improve";

	public const string AiRecommendUrl = "/ai/api/v2/task/recommend";

	public const string AiRecommendTaskUrl = "/ai/api/v2/task/recommend/task";

	public const string AiRecommendHistoryUrl = "/ai/api/v2/task/recommend/history";

	public const string AiVoiceTranscriptUrl = "/ai/api/v2/voice/task/transcriptions";

	public const string AiVoiceTranscriptTaskUrl = "/ai/api/v2/voice/task/transcriptions/task";

	public const string AiVoiceSummaryUrl = "/ai/api/v2/voice/task/summary";

	public const string AiQueryGenerResultUrl = "/ai/api/v2/generated";

	public const string AIAgentConversationsUrl = "/api/v1/conversations";

	public const string AIAgentTokenUsageUrl = "/api/v1/token-usage/current";

	public const string AIAgentPrivacySettingUrl = "/api/v1/user-settings/privacy";

	public const string GetLinkTitleUrl = "/pageTitle?url={0}";
}
