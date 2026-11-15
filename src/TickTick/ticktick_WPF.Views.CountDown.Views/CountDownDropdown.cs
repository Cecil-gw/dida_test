using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountDownDropdown : UserControl, IComponentConnector
{
	internal ListBox CountDownListBox;

	private bool _contentLoaded;

	public ObservableCollection<CountDownItem> CountDownItems { get; set; }

	public event EventHandler<CountDownSelectedEventArgs> CountDownSelected;

	public CountDownDropdown()
		: this(CountDownType.Anniversary)
	{
	}

	public CountDownDropdown(CountDownType type, string key = "", string title = "")
	{
		InitializeComponent();
		CountDownItems = new ObservableCollection<CountDownItem>();
		if (type == CountDownType.Countdown)
		{
			InitializeCountdownItems();
		}
		else
		{
			InitializeAnniversaryItems();
		}
		CountDownListBox.ItemsSource = CountDownItems;
		CountDownItem countDownItem = CountDownItems.FirstOrDefault((CountDownItem it) => it.IconKey == key && it.Text == title);
		if (countDownItem != null)
		{
			CountDownListBox.SelectedItem = countDownItem;
		}
	}

	private void InitializeCountdownItems()
	{
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_trip"), "countdown_trip", "#97E38B"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_concert"), "countdown_concert", "#FF8585"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_payday"), "countdown_payday", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_pay_rent"), "countdown_pay_rent", "#7BC4FA"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_credit_card_bill_due"), "countdown_credit_card_bill_due", "#D7EB63"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_mortgage_payment"), "countdown_mortgage_payment", "#7BC4FA"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_pay_insurance"), "countdown_pay_insurance", "#6E81ED"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_health_check"), "countdown_health_check", "#FDB368"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_pet_health_check"), "countdown_pet_health_check", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_license_renewal_due"), "countdown_license_renewal_due", "#97E38B"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_visa_renewal_due"), "countdown_visa_renewal_due", "#7BC4FA"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_exam"), "countdown_exam", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_school_starts"), "countdown_school_starts", "#D7EB63"));
	}

	private void InitializeAnniversaryItems()
	{
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_graduation"), "countdown_graduation", "#92D0FF"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_work_anniversary"), "countdown_work_anniversary", "#D7EB63"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_dating"), "countdown_dating", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_proposal_day"), "countdown_proposal_day", "#FFF8585"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_marriage_license_day"), "countdown_marriage_license_day", "#FF9BAC"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_wedding_anniversary"), "countdown_wedding_anniversary", "#FDB368"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_100_days_together"), "countdown_100_days_together", "#92D0FF"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_enrollment_anniversary"), "countdown_enrollment_anniversary", "#FDB368"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_speech"), "countdown_first_speech", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_charity_event"), "countdown_first_charity_event", "#FF9BAC"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_road_trip"), "countdown_first_road_trip", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_startup"), "countdown_first_startup", "#6E81ED"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_marathon"), "countdown_first_marathon", "#FDB368"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_pot_of_gold"), "countdown_first_pot_of_gold", "#97E38B"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_car"), "countdown_first_car", "#FEE068"));
		CountDownItems.Add(CreateCountDownItem(Utils.GetString("recommend_first_home"), "countdown_first_home", "#92D0FF"));
	}

	private CountDownItem CreateCountDownItem(string text, string iconKey, string colorHex)
	{
		return new CountDownItem
		{
			Text = text,
			IconKey = iconKey,
			IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/Icons/" + iconKey + ".png", UriKind.Absolute))
		};
	}

	private void CountDownListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (CountDownListBox.SelectedItem is CountDownItem selectedItem)
		{
			this.CountDownSelected?.Invoke(this, new CountDownSelectedEventArgs(selectedItem));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdowndropdown.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			CountDownListBox = (ListBox)target;
			CountDownListBox.SelectionChanged += CountDownListBox_SelectionChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
