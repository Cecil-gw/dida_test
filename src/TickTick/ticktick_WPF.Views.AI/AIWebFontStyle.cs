using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TickTickUtils;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.AI;

internal static class AIWebFontStyle
{
	internal const string VirtualHostName = "ticktick-app-font.local";

	internal const string StyleElementId = "ticktick-native-font-style";

	internal const string WebFontFamilyName = "TickTick Native App Font";

	private const string DefaultFontStack = "\"Microsoft YaHei UI\", \"Microsoft YaHei\", Arial, sans-serif";

	internal static AIWebFontStyleDefinition ResolveCurrent()
	{
		return Resolve(LocalSettings.Settings.ExtraSettings.AppFontFamily, AppPaths.TTFDir, AppPaths.ENTTFDir, File.Exists);
	}

	internal static AIWebFontStyleDefinition Resolve(string fontKey, string downloadedFontDirectory, string bundledFontDirectory, Func<string, bool> fileExists)
	{
		if (fileExists == null)
		{
			throw new ArgumentNullException("fileExists");
		}
		return fontKey switch
		{
			"Arial" => CreateSystemFont("Arial, \"Microsoft YaHei UI\", sans-serif"), 
			"Inter" => CreateLocalFont(bundledFontDirectory, fileExists, new AIWebFontSource("Inter-Regular.ttf", "400"), new AIWebFontSource("Inter-Bold.ttf", "700")), 
			"Poppins" => CreateLocalFont(bundledFontDirectory, fileExists, new AIWebFontSource("Poppins-Regular.ttf", "400"), new AIWebFontSource("Poppins-Bold.ttf", "700")), 
			"Nunito" => CreateLocalFont(bundledFontDirectory, fileExists, new AIWebFontSource("Nunito-Regular.ttf", "400"), new AIWebFontSource("Nunito-Bold.ttf", "700")), 
			"Roboto" => CreateLocalFont(bundledFontDirectory, fileExists, new AIWebFontSource("Roboto-Regular.ttf", "400"), new AIWebFontSource("Roboto-Bold.ttf", "700")), 
			"HarmonyOS" => CreateLocalFont(downloadedFontDirectory, fileExists, new AIWebFontSource("HarmonyOS_Sans_SC_Regular.ttf", "400"), new AIWebFontSource("HarmonyOS_Sans_SC_Bold.ttf", "700")), 
			"SourceHansansSC_CN" => CreateLocalFont(downloadedFontDirectory, fileExists, new AIWebFontSource("SourceHanSansSC-VF.ttf", "100 900")), 
			"LXGW_CN" => CreateLocalFont(downloadedFontDirectory, fileExists, new AIWebFontSource("LXGW_GB.ttf", "400")), 
			"Yozai_CN" => CreateLocalFont(downloadedFontDirectory, fileExists, new AIWebFontSource("Yozai.ttf", "400")), 
			"975Maru_CN" => CreateLocalFont(downloadedFontDirectory, fileExists, new AIWebFontSource("975Maru.ttf", "400")), 
			_ => CreateSystemFont("\"Microsoft YaHei UI\", \"Microsoft YaHei\", Arial, sans-serif"), 
		};
	}

	internal static string BuildInjectionScript(AIWebFontStyleDefinition definition, string trustedOrigin)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		string value = BuildCss(definition);
		string text = JsonConvert.SerializeObject(trustedOrigin ?? string.Empty);
		string text2 = JsonConvert.SerializeObject("ticktick-native-font-style");
		string text3 = JsonConvert.SerializeObject(definition.CssFontStack);
		string text4 = JsonConvert.SerializeObject(value);
		return $"\r\n(function () {{\r\n    if (window.top !== window || window.location.origin !== {text}) return;\r\n    var styleId = {text2};\r\n    var fontStack = {text3};\r\n    var css = {text4};\r\n    function applyNativeFont() {{\r\n        var root = document.documentElement;\r\n        if (!root) return;\r\n        root.style.setProperty('--ticktick-native-font-family', fontStack);\r\n        var style = document.getElementById(styleId);\r\n        if (!style) {{\r\n            style = document.createElement('style');\r\n            style.id = styleId;\r\n            style.type = 'text/css';\r\n            (document.head || root).appendChild(style);\r\n        }}\r\n        if (style.textContent !== css) style.textContent = css;\r\n    }}\r\n    applyNativeFont();\r\n    if (document.readyState === 'loading') {{\r\n        document.addEventListener('DOMContentLoaded', applyNativeFont, {{ once: true }});\r\n    }}\r\n}})();";
	}

	internal static string BuildCss(AIWebFontStyleDefinition definition)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (AIWebFontSource source in definition.Sources)
		{
			stringBuilder.Append("@font-face{font-family:\"").Append("TickTick Native App Font").Append("\";src:url(\"https://")
				.Append("ticktick-app-font.local")
				.Append('/')
				.Append(Uri.EscapeDataString(source.FileName))
				.Append("\") format(\"truetype\");font-style:normal;font-weight:")
				.Append(source.FontWeight)
				.Append(";font-display:swap;}");
		}
		return stringBuilder.ToString();
	}

	private static AIWebFontStyleDefinition CreateSystemFont(string cssFontStack)
	{
		return new AIWebFontStyleDefinition(cssFontStack, null, null);
	}

	private static AIWebFontStyleDefinition CreateLocalFont(string fontDirectory, Func<string, bool> fileExists, params AIWebFontSource[] sources)
	{
		if (string.IsNullOrWhiteSpace(fontDirectory) || sources.Any((AIWebFontSource source) => !fileExists(Path.Combine(fontDirectory, source.FileName))))
		{
			return CreateSystemFont("\"Microsoft YaHei UI\", \"Microsoft YaHei\", Arial, sans-serif");
		}
		return new AIWebFontStyleDefinition("\"TickTick Native App Font\", \"Microsoft YaHei UI\", \"Microsoft YaHei\", Arial, sans-serif", Path.GetFullPath(fontDirectory), sources);
	}
}
