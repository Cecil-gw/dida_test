using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.AI;

internal sealed class AIConversationListItem : INotifyPropertyChanged, IDraggable, ICloneable
{
	private DragVisualState _visualState;

	private string _title;

	private bool _isEditing;

	private bool _isMenuOpen;

	private AIConversationPinMetadata _metadata;

	[JsonIgnore]
	public DragVisualState VisualState
	{
		get
		{
			return _visualState;
		}
		set
		{
			if (_visualState != value)
			{
				_visualState = value;
				OnPropertyChanged("VisualState");
			}
		}
	}

	internal static string TodayGroupTitle => Utils.GetString("Today");

	internal static string LastSevenDaysGroupTitle => Utils.GetString("Last7Days");

	internal static string EarlierGroupTitle => Utils.GetString("Earlier");

	[JsonProperty("meta_data")]
	public AIConversationPinMetadata Metadata
	{
		get
		{
			return _metadata;
		}
		set
		{
			if (_metadata != value)
			{
				_metadata = value;
				OnPropertyChanged("Metadata");
				OnPropertyChanged("IsPinned");
				OnPropertyChanged("PinSortOrder");
				OnPropertyChanged("PinTimeSortKey");
				OnPropertyChanged("GroupTitle");
			}
		}
	}

	[JsonIgnore]
	public bool IsPinned
	{
		get
		{
			if (!IsLocalOnly)
			{
				return Metadata?.IsPinned ?? false;
			}
			return false;
		}
	}

	[JsonIgnore]
	public long PinSortOrder
	{
		get
		{
			if (!IsPinned)
			{
				return 0L;
			}
			return Metadata.SortOrder.GetValueOrDefault();
		}
	}

	[JsonIgnore]
	public decimal PinTimeSortKey
	{
		get
		{
			if (!IsPinned)
			{
				return 0m;
			}
			return Metadata.TimeSortKey;
		}
	}

	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("conversation_id")]
	public string ConversationId { get; set; }

	[JsonProperty("title")]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (!string.Equals(_title, value, StringComparison.Ordinal))
			{
				_title = value;
				OnPropertyChanged("Title");
				OnPropertyChanged("DisplayTitle");
			}
		}
	}

	[JsonProperty("created_at")]
	public DateTimeOffset? CreatedAt { get; set; }

	[JsonProperty("updated_at")]
	public DateTimeOffset? UpdatedAt { get; set; }

	[JsonProperty("is_archived")]
	public bool IsArchived { get; set; }

	[JsonIgnore]
	public bool IsLocalOnly { get; set; }

	[JsonIgnore]
	public bool IsEditing
	{
		get
		{
			return _isEditing;
		}
		set
		{
			if (_isEditing != value)
			{
				_isEditing = value;
				OnPropertyChanged("IsEditing");
			}
		}
	}

	[JsonIgnore]
	public bool IsMenuOpen
	{
		get
		{
			return _isMenuOpen;
		}
		set
		{
			if (_isMenuOpen != value)
			{
				_isMenuOpen = value;
				OnPropertyChanged("IsMenuOpen");
			}
		}
	}

	[JsonIgnore]
	public string EffectiveConversationId
	{
		get
		{
			if (string.IsNullOrWhiteSpace(ConversationId))
			{
				return Id?.Trim();
			}
			return ConversationId.Trim();
		}
	}

	[JsonIgnore]
	public string DisplayTitle
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Title))
			{
				return Title.Trim();
			}
			return Utils.GetString("AIAgentNewChat");
		}
	}

	[JsonIgnore]
	public DateTimeOffset SortTime => UpdatedAt ?? CreatedAt ?? DateTimeOffset.MaxValue;

	[JsonIgnore]
	public string GroupTitle
	{
		get
		{
			if (IsPinned)
			{
				return Utils.GetString("Pinned");
			}
			return ResolveGroupTitle((UpdatedAt ?? CreatedAt)?.LocalDateTime, DateTime.Today);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public object Clone()
	{
		AIConversationListItem obj = (AIConversationListItem)MemberwiseClone();
		obj.PropertyChanged = null;
		return obj;
	}

	internal static string ResolveGroupTitle(DateTime? modifiedAt, DateTime today)
	{
		DateTime date = today.Date;
		DateTime dateTime = modifiedAt?.Date ?? date;
		if (dateTime >= date)
		{
			return TodayGroupTitle;
		}
		if (!(dateTime >= date.AddDays(-7.0)))
		{
			return EarlierGroupTitle;
		}
		return LastSevenDaysGroupTitle;
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
