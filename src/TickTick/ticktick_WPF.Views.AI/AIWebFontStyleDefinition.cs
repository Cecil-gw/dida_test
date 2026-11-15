using System.Collections.Generic;
using System.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AIWebFontStyleDefinition
{
	public string CssFontStack { get; }

	public string MappedFolderPath { get; }

	public IReadOnlyList<AIWebFontSource> Sources { get; }

	public AIWebFontStyleDefinition(string cssFontStack, string mappedFolderPath, IEnumerable<AIWebFontSource> sources)
	{
		CssFontStack = cssFontStack;
		MappedFolderPath = mappedFolderPath;
		Sources = (sources ?? Enumerable.Empty<AIWebFontSource>()).ToArray();
	}
}
