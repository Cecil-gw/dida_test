namespace ticktick_WPF.Views.QuickAdd;

public class QuickAddToken
{
	public TokenType TokenType { get; set; }

	public string Value { get; set; }

	public bool Exclusive { get; set; }

	public static QuickAddToken BuildTag(string tag)
	{
		return new QuickAddToken
		{
			TokenType = TokenType.Tag,
			Exclusive = false,
			Value = tag
		};
	}

	public static QuickAddToken BuildProject(string projectTitle)
	{
		return new QuickAddToken
		{
			TokenType = TokenType.Project,
			Exclusive = true,
			Value = projectTitle
		};
	}

	public static QuickAddToken BuildPriority(string priority)
	{
		return new QuickAddToken
		{
			TokenType = TokenType.Priority,
			Exclusive = true,
			Value = priority
		};
	}

	public static QuickAddToken BuildQuickDate(string date)
	{
		return new QuickAddToken
		{
			TokenType = TokenType.QuickDate,
			Exclusive = true,
			Value = date
		};
	}

	public static QuickAddToken BuildAssignee(string name)
	{
		return new QuickAddToken
		{
			TokenType = TokenType.Assignee,
			Exclusive = true,
			Value = name
		};
	}

	private bool Equals(QuickAddToken other)
	{
		if (other == null)
		{
			return false;
		}
		if (TokenType == other.TokenType)
		{
			return Value == other.Value;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as QuickAddToken);
	}
}
