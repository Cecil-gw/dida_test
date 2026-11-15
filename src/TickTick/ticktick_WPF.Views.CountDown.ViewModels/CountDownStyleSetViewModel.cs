using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Views.CountDown.Views;
using ticktick_WPF.Views.Theme;

namespace ticktick_WPF.Views.CountDown.ViewModels;

internal class CountDownStyleSetViewModel : INotifyPropertyChanged
{
	private bool _isWidgetCard;

	private string _styleType;

	private CountDownViewModel _previewViewModel;

	private static readonly Dictionary<string, string> ColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "#546382", "#CBD6EB" },
		{ "#7287BD", "#CFD6E4" },
		{ "#57908C", "#BED4D3" },
		{ "#659C68", "#C9DEC9" },
		{ "#718C69", "#D3DEC9" },
		{ "#DC8B34", "#E8E0CA" },
		{ "#D46F30", "#E8D6C3" },
		{ "#D65A48", "#EBD0CC" },
		{ "#825BA3", "#CFBFDC" },
		{ "#393B64", "#C1C2D9" },
		{ "#66443D", "#DECDC8" },
		{ "#3A3A3A", "#BCBCBC" },
		{ "#92ACFF", "#94E1F6" },
		{ "#95C3FF", "#C3EA69" },
		{ "#71DFDB", "#ACA8FF" },
		{ "#5CE2A1", "#8EA5FF" },
		{ "#AEEB68", "#46DB93" },
		{ "#F9CC52", "#95DBFF" },
		{ "#FFA958", "#F3E76A" },
		{ "#FFBCE9", "#B597F4" },
		{ "#C3ABF6", "#9FF1FC" },
		{ "#21213D", "#424664" },
		{ "#242424", "#43464D" },
		{ "#5F85FF", "#FFC5C6" },
		{ "#76A9FF", "#C9ECFF" },
		{ "#4EDBCC", "#FFF8C9" },
		{ "#56E8A1", "#D8FFBF" },
		{ "#C0E575", "#BCFFEA" },
		{ "#FFB15E", "#FFF0BE" },
		{ "#F9966F", "#FFDDC8" },
		{ "#FF7E7E", "#FFD8E0" },
		{ "#8B8AF1", "#FFC2E1" },
		{ "#243269", "#D6E5F4" },
		{ "#5D423B", "#FAE5D4" },
		{ "#2B2B2B", "#E4E4E4" }
	};

	public bool IsWidgetCard
	{
		get
		{
			return _isWidgetCard;
		}
		set
		{
			_isWidgetCard = value;
			NotifyPropertyChanged("IsWidgetCard");
		}
	}

	public string StyleType
	{
		get
		{
			return _styleType;
		}
		set
		{
			_styleType = value;
			NotifyPropertyChanged("StyleType");
		}
	}

	public CountDownViewModel PreviewViewModel
	{
		get
		{
			return _previewViewModel;
		}
		set
		{
			_previewViewModel = value;
			NotifyPropertyChanged("PreviewViewModel");
		}
	}

	public ObservableCollection<CountDownViewModel> StylePreviews { get; set; } = new ObservableCollection<CountDownViewModel>();

	public Action<StyleSelectionResult> StyleSelected { get; set; }

	public Action OnCountdownChanged { get; set; }

	private StyleSelectionResult lastStyle { get; set; } = new StyleSelectionResult();

	public string SelectedBackgroundPath { get; private set; }

	public string SelectedColorHex { get; private set; }

	public StyleSelectionResult Result { get; set; }

	public CountdownModel Model { get; internal set; }

	private SelectImageWindow selectImageWindow { get; set; }

	private Window parentWindow { get; set; }

	public List<string> _normalColors { get; } = new List<string>
	{
		"#4772FA", "#3FA2FF", "#43DCBD", "#59CE80", "#9ED03A", "#FFA646", "#FF763F", "#EB5374", "#9174F7", "#303E75",
		"#763928", "#191919"
	};

	public List<string> _fullscreenColors { get; } = new List<string>
	{
		"#FFFFFF", "#9EB8FF", "#92D5FF", "#81E8DB", "#83F0BB", "#B0F183", "#FFD87C", "#FFB179", "#FF978C", "#FF90AC",
		"#FF9CE1", "#C8A8FF", "#ABA7FF"
	};

	public List<string> _calendarColors { get; } = new List<string>
	{
		"#1C42BE", "#0D61A5", "#116C6F", "#11783A", "#4E7619", "#CA6800", "#D4582E", "#CA393B", "#6734CD", "#2E4499",
		"#763928", "#191919"
	};

	public List<string> _twoColorColors { get; } = new List<string>
	{
		"#546382", "#7287BD", "#57908C", "#659C68", "#718C69", "#DC8B34", "#D46F30", "#D65A48", "#825BA3", "#393B64",
		"#66443D", "#3A3A3A"
	};

	public List<string> _dopamineColors { get; } = new List<string>
	{
		"#92ACFF", "#95C3FF", "#71DFDB", "#5CE2A1", "#AEEB68", "#F9CC52", "#FFA958", "#FFBCE9", "#C3ABF6", "#21213D",
		"#242424"
	};

	public List<string> _cartoonColors { get; } = new List<string>
	{
		"#5F85FF", "#76A9FF", "#4EDBCC", "#56E8A1", "#C0E575", "#FFB15E", "#F9966F", "#FF7E7E", "#8B8AF1", "#243269",
		"#5D423B", "#2B2B2B"
	};

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public void Init(CountdownModel model, bool fromAdd = false)
	{
		Model = model;
		if (fromAdd)
		{
			CountdownDao.SetCoundownStyle(model, lastStyle);
		}
		InitializePreviewViewModel();
		StyleType = model.Style;
		OnCountdownChanged?.Invoke();
	}

	private void InitializePreviewViewModel()
	{
		if (Model == null)
		{
			return;
		}
		PreviewViewModel = new CountDownViewModel(Model, Model.Style, IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = (IsWidgetCard ? 0.7 : 0.8),
			IgnoreRatio = !IsWidgetCard
		};
		double scaleRatio = (IsWidgetCard ? 0.45 : 0.3);
		StylePreviews.Clear();
		StylePreviews.Add(new CountDownViewModel(Model, "normal", IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = scaleRatio,
			IgnoreRatio = !IsWidgetCard,
			StyleColors = new string[0]
		});
		StylePreviews.Add(new CountDownViewModel(Model, "fullscreen_image", IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = scaleRatio,
			IgnoreRatio = !IsWidgetCard,
			StyleColors = new string[1] { _fullscreenColors[0] }
		});
		if (!Utils.IsTickPackage())
		{
			StylePreviews.Add(new CountDownViewModel(Model, "perpetual", IsWidgetCard)
			{
				ShowActionButton = false,
				ScaleRatio = scaleRatio,
				IgnoreRatio = !IsWidgetCard,
				StyleColors = new string[0]
			});
		}
		StylePreviews.Add(new CountDownViewModel(Model, "two_color_card", IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = scaleRatio,
			IgnoreRatio = !IsWidgetCard,
			StyleColors = new string[1] { _twoColorColors[0] }
		});
		StylePreviews.Add(new CountDownViewModel(Model, "cartoon", IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = scaleRatio,
			IgnoreRatio = !IsWidgetCard,
			StyleColors = new string[1] { _cartoonColors[0] }
		});
		StylePreviews.Add(new CountDownViewModel(Model, "dopamine", IsWidgetCard)
		{
			ShowActionButton = false,
			ScaleRatio = scaleRatio,
			IgnoreRatio = !IsWidgetCard,
			StyleColors = new string[1] { _dopamineColors[0] }
		});
		string selectedStyle = Model.Style;
		string style = Model.Style;
		if (!(style == "calendar"))
		{
			if (style == "polaroid")
			{
				selectedStyle = "fullscreen_image";
			}
		}
		else
		{
			selectedStyle = "normal";
		}
		CountDownViewModel countDownViewModel = StylePreviews.FirstOrDefault((CountDownViewModel it) => it.StyleType == selectedStyle);
		if (countDownViewModel != null)
		{
			countDownViewModel.StyleColors = Model.StyleColors;
		}
	}

	public void SetParentWindow(Window window)
	{
		parentWindow = window;
	}

	public async Task Apply()
	{
		if (!UserDao.IsPro() && PreviewViewModel.IsProStyle)
		{
			ProChecker.ShowUpgradeDialog(ProType.CountdownPremiumThemes, parentWindow);
			return;
		}
		if (PreviewViewModel.IsTwoColorStyle && PreviewViewModel.StyleColors.Length == 1)
		{
			string correspondingColor = GetCorrespondingColor(PreviewViewModel.StyleColors[0]);
			if (correspondingColor != null)
			{
				PreviewViewModel.StyleColors = new string[2]
				{
					PreviewViewModel.StyleColors[0],
					correspondingColor
				};
			}
		}
		string attachmentId = "";
		if (!string.IsNullOrEmpty(PreviewViewModel.CustomImagePath))
		{
			if (!Utils.IsNetworkAvailable())
			{
				Utils.Toast(Utils.GetString("UploadAttachmentNetworkError"));
				return;
			}
			attachmentId = Utils.GetGuid();
			await AttachmentUploadUtils.UploadCountdownImage(attachmentId, PreviewViewModel.CustomImagePath);
		}
		Result = new StyleSelectionResult
		{
			Style = StyleType,
			StyleColors = PreviewViewModel.StyleColors,
			PresetImage = (string.IsNullOrEmpty(SelectedBackgroundPath) ? PreviewViewModel.GetPresetImageKeyWithDefault() : string.Empty),
			LocalImagePath = PreviewViewModel.CustomImagePath,
			ClipRect = PreviewViewModel.ClipRect,
			AttachmentId = attachmentId
		};
		lastStyle = new StyleSelectionResult
		{
			Style = Result.Style,
			StyleColors = Result.StyleColors
		};
		StyleSelected?.Invoke(Result);
	}

	private static string GetCorrespondingColor(string firstColor)
	{
		string key = (firstColor.StartsWith("#") ? firstColor : ("#" + firstColor));
		if (ColorMap.TryGetValue(key, out var value))
		{
			return value;
		}
		if (ColorMap.TryGetValue(firstColor, out value))
		{
			return value;
		}
		return null;
	}

	public void ShowCropWindow()
	{
		string text = PreviewViewModel.CustomImagePath;
		bool flag = false;
		if (!File.Exists(text))
		{
			flag = true;
			string text2 = PreviewViewModel.PresetImageKey.Replace("preset_", "");
			if (string.IsNullOrEmpty(text2))
			{
				text2 = Model.Type switch
				{
					CountDownType.Birthday => "birthday", 
					CountDownType.Holiday => "holiday", 
					CountDownType.Anniversary => "anniversary", 
					_ => "countdown", 
				};
			}
			string text3 = "pack://application:,,,/Assets/CountDown/preset_" + text2 + ".jpg";
			string text4 = Path.Combine(AppPaths.CountdownDir, "preset_" + text2 + ".jpg");
			if (!File.Exists(text4))
			{
				try
				{
					if (!Directory.Exists(AppPaths.CountdownDir))
					{
						Directory.CreateDirectory(AppPaths.CountdownDir);
					}
					BitmapImage source = new BitmapImage(new Uri(text3));
					JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder();
					jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(source));
					using FileStream stream = new FileStream(text4, FileMode.Create);
					jpegBitmapEncoder.Save(stream);
				}
				catch (Exception)
				{
					text = text3;
				}
			}
			text = text4;
		}
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return;
		}
		Rect clipRect = PreviewViewModel.ClipRect;
		Rect rect;
		if (clipRect.Width <= 0.0)
		{
			if (flag)
			{
				rect = new Rect(new Point(0.0, 0.23), new Point(1.0, 0.58));
			}
			else
			{
				double num = 1.3333333333333333;
				double num2 = PreviewViewModel.ImageWidth / PreviewViewModel.ImageHeight;
				double x = 0.0;
				double y = 0.0;
				double width = 1.0;
				double height = 1.0;
				if (num2 > num)
				{
					double num3 = num / num2;
					width = num3;
					x = (1.0 - num3) / 2.0;
				}
				else
				{
					double num4 = num2 / num;
					height = num4;
					y = (1.0 - num4) / 2.0;
				}
				rect = new Rect(x, y, width, height);
			}
		}
		else
		{
			rect = new Rect(clipRect.X / PreviewViewModel.ImageWidth, clipRect.Y / PreviewViewModel.ImageHeight, clipRect.Width / PreviewViewModel.ImageWidth, clipRect.Height / PreviewViewModel.ImageHeight);
		}
		string location = rect.Left + "," + rect.Top + "," + rect.Width + "," + rect.Height;
		CustomThemeDialog customThemeDialog = new CustomThemeDialog(text, location, useForCountdown: true);
		customThemeDialog.Owner = parentWindow;
		customThemeDialog.OnRectClip -= OnImageClip;
		customThemeDialog.OnRectClip += OnImageClip;
		customThemeDialog.ShowDialog();
	}

	private void OnImageClip(object sender, Rect clipRect)
	{
		Rect absoluteRect = GetAbsoluteRect(PreviewViewModel.ImageWidth, PreviewViewModel.ImageHeight, clipRect);
		PreviewViewModel.ClipRect = absoluteRect;
		foreach (CountDownViewModel stylePreview in StylePreviews)
		{
			if (stylePreview.StyleType == "fullscreen_image" || stylePreview.StyleType == "polaroid")
			{
				stylePreview.ClipRect = absoluteRect;
				break;
			}
		}
		if (IsWidgetCard)
		{
			Apply();
		}
	}

	private Rect GetAbsoluteRect(double width, double height, Rect relativeRect)
	{
		double x = relativeRect.Left * width;
		double y = relativeRect.Top * height;
		double width2 = relativeRect.Width * width;
		double height2 = relativeRect.Height * height;
		return new Rect(x, y, width2, height2);
	}

	private bool ImageAvailable(string saveImagePath)
	{
		return Utils.LoadBitmap(saveImagePath) != null;
	}

	public void ShowSelectImageWindow()
	{
		selectImageWindow = new SelectImageWindow(PreviewViewModel.CustomImagePath, PreviewViewModel.PresetImageKey, PreviewViewModel.ClipRect);
		selectImageWindow.SetCountdownId(Model.Id);
		selectImageWindow.SetSelectedImage(PreviewViewModel.PresetImageKey);
		selectImageWindow.WindowStartupLocation = WindowStartupLocation.Manual;
		selectImageWindow.Owner = parentWindow;
		selectImageWindow.ShowInTaskbar = false;
		selectImageWindow.Closed += delegate
		{
			selectImageWindow = null;
			parentWindow?.Activate();
		};
		double num = 0.0;
		double num2 = 0.0;
		num = selectImageWindow.Owner.Left + (selectImageWindow.Owner.Width - 300.0) / 2.0;
		num2 = selectImageWindow.Owner.Top + (selectImageWindow.Owner.Height - 400.0) / 2.0 + (double)((!IsWidgetCard) ? 235 : 96);
		selectImageWindow.Left = num;
		selectImageWindow.Top = num2;
		selectImageWindow.SetSelectedImage(PreviewViewModel.PresetImageKey);
		selectImageWindow.SetCountdownId(Model.Id);
		selectImageWindow.OpenImageFinished += delegate(object sender, OpenImageFinishedEventArgs path)
		{
			if (path != null && !string.IsNullOrEmpty(path.Path))
			{
				PreviewViewModel.PresetImageKey = string.Empty;
				PreviewViewModel.CustomImagePath = path.Path;
				PreviewViewModel.ClipRect = GetAbsoluteRect(PreviewViewModel.ImageWidth, PreviewViewModel.ImageHeight, path.ClipRect);
				foreach (CountDownViewModel stylePreview in StylePreviews)
				{
					if (stylePreview.StyleType == "fullscreen_image")
					{
						stylePreview.PresetImageKey = string.Empty;
						stylePreview.CustomImagePath = path.Path;
						stylePreview.ClipRect = GetAbsoluteRect(stylePreview.ImageWidth, stylePreview.ImageHeight, path.ClipRect);
						break;
					}
				}
			}
			if (IsWidgetCard)
			{
				Apply();
			}
		};
		selectImageWindow.PresetImageSelected += delegate(object sender, string img)
		{
			PreviewViewModel.CustomImagePath = string.Empty;
			PreviewViewModel.PresetImageKey = img;
			PreviewViewModel.ClipRect = default(Rect);
			foreach (CountDownViewModel stylePreview2 in StylePreviews)
			{
				if (stylePreview2.StyleType == "fullscreen_image")
				{
					stylePreview2.CustomImagePath = string.Empty;
					stylePreview2.PresetImageKey = PreviewViewModel.PresetImageKey;
					stylePreview2.ClipRect = default(Rect);
					break;
				}
			}
			if (IsWidgetCard)
			{
				Apply();
			}
		};
		selectImageWindow.Show();
	}

	public void CloseSelectImageWindow()
	{
		if (selectImageWindow != null)
		{
			selectImageWindow.Close();
		}
	}

	public void OnColorSelect(string styleType, string color)
	{
		CountDownViewModel countDownViewModel = StylePreviews.FirstOrDefault((CountDownViewModel it) => it.StyleType == styleType);
		if (countDownViewModel == null)
		{
			return;
		}
		if (color == "transparent")
		{
			countDownViewModel.StyleColors = new string[0];
			PreviewViewModel.StyleColors = new string[0];
		}
		else
		{
			string correspondingColor = GetCorrespondingColor(color);
			if (correspondingColor != null)
			{
				countDownViewModel.StyleColors = new string[2] { color, correspondingColor };
				PreviewViewModel.StyleColors = new string[2] { color, correspondingColor };
			}
			else
			{
				countDownViewModel.StyleColors = new string[1] { color };
				PreviewViewModel.StyleColors = new string[1] { color };
			}
		}
		if (IsWidgetCard)
		{
			Apply();
		}
	}

	public void OnStyleCardSelect(CountDownViewModel model)
	{
		StyleType = model.StyleType;
		PreviewViewModel = new CountDownViewModel(model.Model, model.StyleType, IsWidgetCard)
		{
			ScaleRatio = PreviewViewModel.ScaleRatio,
			StyleColors = model.StyleColors,
			ShowActionButton = false,
			ClipRect = model.ClipRect,
			PresetImageKey = model.PresetImageKey,
			CustomImagePath = model.CustomImagePath
		};
		if (IsWidgetCard)
		{
			Apply();
		}
	}
}
