using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace ticktick_WPF.Views.MarkDown.Colorizer;

internal sealed class ThemedHighlightingDefinitionResolver : IHighlightingDefinitionReferenceResolver
{
	private const string ResourcePrefix = "ticktick_WPF.Views.MarkDown.Colorizer.Resources.";

	private static readonly object CacheLock = new object();

	private static readonly Dictionary<string, IHighlightingDefinition> Cache = new Dictionary<string, IHighlightingDefinition>();

	private readonly Assembly _assembly;

	private readonly string _themeKey;

	public ThemedHighlightingDefinitionResolver(string themeKey)
	{
		_themeKey = themeKey;
		_assembly = typeof(CodeBlockLanguageCatalog).Assembly;
	}

	public IHighlightingDefinition GetDefinition(string name)
	{
		if (CodeBlockLanguageCatalog.TryGetHighlightingResourceKey(name, out var resourceKey))
		{
			IHighlightingDefinition highlightingDefinition = TryLoadThemedDefinition(resourceKey);
			if (highlightingDefinition != null)
			{
				return highlightingDefinition;
			}
		}
		return HighlightingManager.Instance.GetDefinition(name);
	}

	private IHighlightingDefinition TryLoadThemedDefinition(string resourceKey)
	{
		string key = _themeKey + ":" + resourceKey.ToLowerInvariant();
		lock (CacheLock)
		{
			if (Cache.TryGetValue(key, out var value))
			{
				return value;
			}
		}
		string name = "ticktick_WPF.Views.MarkDown.Colorizer.Resources." + resourceKey + "_" + _themeKey + ".xshd";
		using Stream stream = _assembly.GetManifestResourceStream(name);
		if (stream == null)
		{
			return null;
		}
		IHighlightingDefinition highlightingDefinition;
		using (XmlReader reader = new XmlTextReader(stream))
		{
			highlightingDefinition = HighlightingLoader.Load(reader, this);
		}
		lock (CacheLock)
		{
			if (Cache.TryGetValue(key, out var value2))
			{
				return value2;
			}
			Cache[key] = highlightingDefinition;
			return highlightingDefinition;
		}
	}
}
