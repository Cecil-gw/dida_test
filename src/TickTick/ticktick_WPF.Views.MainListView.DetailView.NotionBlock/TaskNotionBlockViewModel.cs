using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class TaskNotionBlockViewModel
{
	public ObservableCollection<TaskNotionBlockItemViewModel> Items { get; set; }

	public string pageUrl { get; set; }

	public static TaskNotionBlockViewModel Build(string taskNotionBlock)
	{
		TaskNotionBlockViewModel taskNotionBlockViewModel = new TaskNotionBlockViewModel();
		ObservableCollection<TaskNotionBlockItemViewModel> observableCollection = (taskNotionBlockViewModel.Items = new ObservableCollection<TaskNotionBlockItemViewModel>());
		JArray jArray = JArray.Parse(taskNotionBlock);
		if (jArray.Count > 0)
		{
			foreach (JToken item in jArray)
			{
				if (!(item is JObject jObject) || !jObject.TryGetValue("key", out JToken value) || !jObject.TryGetValue("value", out JToken value2) || !(value2 is JObject jObject2))
				{
					continue;
				}
				TaskNotionBlockItemViewModel taskNotionBlockItemViewModel = new TaskNotionBlockItemViewModel();
				taskNotionBlockItemViewModel.key = value.ToString();
				if (jObject2.TryGetValue("type", out JToken value3))
				{
					taskNotionBlockItemViewModel.type = value3.ToString();
				}
				if (taskNotionBlockItemViewModel.type == null || !jObject2.TryGetValue(taskNotionBlockItemViewModel.type, out JToken value4) || (observableCollection.Count >= 4 && taskNotionBlockItemViewModel.type != "url"))
				{
					continue;
				}
				switch (taskNotionBlockItemViewModel.type)
				{
				case "status":
				case "select":
				{
					if (!(value4 is JObject jObject5) || !jObject5.TryGetValue("name", out JToken value8) || !jObject5.TryGetValue("color", out JToken value9))
					{
						continue;
					}
					string text = value8.ToString();
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					TaskNotionBlockItemSelectViewModel taskNotionBlockItemSelectViewModel = new TaskNotionBlockItemSelectViewModel
					{
						name = text,
						color = value9.ToString()
					};
					if (taskNotionBlockItemViewModel.type == "status")
					{
						taskNotionBlockItemViewModel.status = taskNotionBlockItemSelectViewModel;
					}
					else
					{
						taskNotionBlockItemViewModel.select = taskNotionBlockItemSelectViewModel;
					}
					break;
				}
				case "multi_select":
				{
					if (!(value4 is JArray jArray3))
					{
						continue;
					}
					List<TaskNotionBlockItemSelectViewModel> list = new List<TaskNotionBlockItemSelectViewModel>();
					foreach (JToken item2 in jArray3)
					{
						if (item2 is JObject jObject4 && jObject4.TryGetValue("name", out JToken value6) && jObject4.TryGetValue("color", out JToken value7))
						{
							list.Add(new TaskNotionBlockItemSelectViewModel
							{
								name = value6.ToString(),
								color = value7.ToString()
							});
						}
					}
					if (list.Count == 0)
					{
						continue;
					}
					taskNotionBlockItemViewModel.multi_select = list;
					break;
				}
				case "rich_text":
				{
					if (!(value4 is JArray { Count: >0 } jArray2))
					{
						continue;
					}
					StringBuilder stringBuilder = new StringBuilder();
					foreach (JToken item3 in jArray2)
					{
						if (item3 is JObject jObject3 && jObject3.TryGetValue("plain_text", out JToken value5))
						{
							stringBuilder.Append(value5);
						}
					}
					taskNotionBlockItemViewModel.text = stringBuilder.ToString();
					if (string.IsNullOrWhiteSpace(taskNotionBlockItemViewModel.text))
					{
						continue;
					}
					break;
				}
				case "date":
				{
					if (!(value4 is JObject jObject8))
					{
						continue;
					}
					bool isAllDay = !jObject8.ToString().Contains("T");
					TaskNotionBlockItemDateViewModel taskNotionBlockItemDateViewModel = new TaskNotionBlockItemDateViewModel
					{
						isAllDay = isAllDay
					};
					if (jObject8.TryGetValue("start", out JToken value14))
					{
						DateTime result;
						if (value14.Type == JTokenType.Date)
						{
							taskNotionBlockItemDateViewModel.start = value14.Value<DateTime>();
						}
						else if (DateTime.TryParseExact(value14.ToString(), "yyyy'-'MM'-'dd", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
						{
							taskNotionBlockItemDateViewModel.start = result;
						}
					}
					if (!taskNotionBlockItemDateViewModel.start.HasValue)
					{
						continue;
					}
					if (jObject8.TryGetValue("end", out JToken value15))
					{
						DateTime result2;
						if (value15.Type == JTokenType.Date)
						{
							taskNotionBlockItemDateViewModel.end = value15.Value<DateTime>();
						}
						else if (DateTime.TryParseExact(value15.ToString(), "yyyy'-'MM'-'dd", CultureInfo.CurrentCulture, DateTimeStyles.None, out result2))
						{
							taskNotionBlockItemDateViewModel.end = result2;
						}
					}
					if (jObject8.TryGetValue("time_zone", out JToken value16))
					{
						taskNotionBlockItemDateViewModel.timeZone = value16.ToString();
					}
					taskNotionBlockItemViewModel.date = taskNotionBlockItemDateViewModel;
					break;
				}
				case "people":
				{
					if (!(value4 is JArray { Count: >0 } jArray4))
					{
						continue;
					}
					List<TaskNotionBlockItemPeopleViewModel> list2 = new List<TaskNotionBlockItemPeopleViewModel>();
					foreach (JToken item4 in jArray4)
					{
						if (item4 is JObject jObject6)
						{
							TaskNotionBlockItemPeopleViewModel taskNotionBlockItemPeopleViewModel = new TaskNotionBlockItemPeopleViewModel();
							if (jObject6.TryGetValue("name", out JToken value10))
							{
								taskNotionBlockItemPeopleViewModel.name = value10.ToString();
							}
							if (jObject6.TryGetValue("avatar_url", out JToken value11))
							{
								taskNotionBlockItemPeopleViewModel.avatar_url = value11.ToString();
							}
							if (jObject6.TryGetValue("person", out JToken value12) && value12 is JObject jObject7 && jObject7.TryGetValue("email", out JToken value13))
							{
								taskNotionBlockItemPeopleViewModel.email = value13.ToString();
							}
							list2.Add(taskNotionBlockItemPeopleViewModel);
						}
					}
					if (list2.Count == 0)
					{
						continue;
					}
					taskNotionBlockItemViewModel.People = list2;
					break;
				}
				case "number":
					if (value4.Type == JTokenType.Float)
					{
						taskNotionBlockItemViewModel.number = value4.Value<double>();
						break;
					}
					if (value4.Type != JTokenType.Integer)
					{
						continue;
					}
					taskNotionBlockItemViewModel.number = value4.Value<int>();
					break;
				case "url":
					taskNotionBlockViewModel.pageUrl = value4.ToString();
					continue;
				}
				observableCollection.Add(taskNotionBlockItemViewModel);
			}
		}
		return taskNotionBlockViewModel;
	}
}
