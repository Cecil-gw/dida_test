using System.Text.RegularExpressions;

namespace ticktick_WPF.Util.DateParser;

internal class Patterns
{
	public const string GOOD_IRI_CHAR = "a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef";

	public static Regex IP_ADDRESS = new Regex("((25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9])\\.(25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[1-9]|0)\\.(25[0-5]|2[0-4][0-9]|[0-1][0-9]{2}|[1-9][0-9]|[0-9]))");

	private const string IRI = "[a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]([a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef\\-]{0,61}[a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]){0,1}";

	private const string GOOD_GTLD_CHAR = "a-zA-Z\u00a0-\ud7ff豈-﷏ﷰ-\uffef";

	private const string GTLD = "[a-zA-Z\u00a0-\ud7ff豈-﷏ﷰ-\uffef]{2,63}";

	private const string HOST_NAME = "([a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]([a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef\\-]{0,61}[a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]){0,1}\\.)+[a-zA-Z\u00a0-\ud7ff豈-﷏ﷰ-\uffef]{2,63}";

	public static Regex DOMAIN_NAME = new Regex("(([a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]([a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef\\-]{0,61}[a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef]){0,1}\\.)+[a-zA-Z\u00a0-\ud7ff豈-﷏ﷰ-\uffef]{2,63}|" + IP_ADDRESS?.ToString() + ")");

	public static Regex WEB_URL = new Regex("((?:(http|https|Http|Https|rtsp|Rtsp):\\/\\/(?:(?:[a-zA-Z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-fA-F0-9]{2})){1,64}(?:\\:(?:[a-zA-Z0-9\\$\\-\\_\\.\\+\\!\\*\\'\\(\\)\\,\\;\\?\\&\\=]|(?:\\%[a-fA-F0-9]{2})){1,25})?\\@)?)?(?:" + DOMAIN_NAME?.ToString() + ")(?:\\:\\d{1,5})?)(\\/(?:(?:[a-zA-Z0-9\u00a0-\ud7ff豈-﷏ﷰ-\uffef\\;\\/\\?\\:\\@\\&\\=\\#\\~\\-\\.\\+\\!\\*\\'\\(\\)\\,\\_])|(?:\\%[a-fA-F0-9]{2}))*)?(?:\\b|$)");
}
