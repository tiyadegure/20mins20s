using Newtonsoft.Json;
using Project1.UI.Controls.Models;
using Project1.UI.Cores;
using ProjectEye.Core;
using ProjectEye.Core.Models.Options;
using ProjectEye.Models.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ProjectEye.Core.Service
{
    public class ThemeService : IService
    {
        private readonly ConfigService config;
        private readonly SystemResourcesService systemResources;
        private readonly Theme theme;


        public delegate void ThemeChangedEventHandler(string OldThemeName, string NewThemeName);
        /// <summary>
        /// 当切换主题时发生
        /// </summary>
        public event ThemeChangedEventHandler OnChangedTheme;
        public ThemeService(ConfigService config,
            SystemResourcesService systemResources)
        {
            this.config = config;
            this.systemResources = systemResources;
            theme = new Theme();
        }
        public void Init()
        {
            string themeName = config.options.Style.Theme.ThemeName;
            if (systemResources.Themes.Where(m => m.ThemeName == themeName).Count() == 0)
            {
                themeName = systemResources.Themes[0].ThemeName;
                config.options.Style.Theme = systemResources.Themes[0];
                //config.Save();
            }
            Project1.UI.Cores.UIDefaultSetting.DefaultThemeName = themeName;

            Project1.UI.Cores.UIDefaultSetting.DefaultThemePath = "/20min20s;component/Resources/Themes/";

            HandleDarkMode();
        }
        /// <summary>
        /// 设置主题
        /// </summary>
        /// <param name="themeName"></param>
        public void SetTheme(string themeName)
        {

            if (Project1.UI.Cores.UIDefaultSetting.DefaultThemeName != themeName)
            {
                string oldName = Project1.UI.Cores.UIDefaultSetting.DefaultThemeName;

                Project1.UI.Cores.UIDefaultSetting.DefaultThemeName = themeName;

                Project1.UI.Cores.UIDefaultSetting.DefaultThemePath = "/20min20s;component/Resources/Themes/";

                theme.ApplyTheme();

                OnChangedTheme?.Invoke(oldName, themeName);
            }
        }

        public void HandleDarkMode()
        {
            string darkModeThemeName = "Dark";
            if (config.options.Style.IsAutoDarkMode)
            {
                var darkTheme = systemResources.Themes.Where(m => m.ThemeName == darkModeThemeName).FirstOrDefault();
                if (darkTheme == null)
                {
                    return;
                }
                DateTime startTime = new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    DateTime.Now.Day,
                    config.options.Style.AutoDarkStartH,
                   config.options.Style.AutoDarkStartM,
                    0);
                DateTime endTime = new DateTime(
                    DateTime.Now.Year,
                    DateTime.Now.Month,
                    DateTime.Now.Day,
                    config.options.Style.AutoDarkEndH,
                   config.options.Style.AutoDarkEndM,
                    0);

                bool isOpen = false;

                if (config.options.Style.AutoDarkStartH <= config.options.Style.AutoDarkEndH)
                {
                    isOpen = DateTime.Now >= startTime && DateTime.Now <= endTime;
                }
                else
                {
                    isOpen = DateTime.Now >= startTime || DateTime.Now <= endTime;
                }
                if (isOpen)
                {
                    if (config.options.Style.Theme != darkTheme)
                    {
                        Debug.WriteLine("dark mode open!");
                        config.options.Style.Theme = darkTheme;

                        SetTheme(darkModeThemeName);

                    }
                }
                else
                {
                    var defualtTheme = systemResources.Themes[0];
                    if (config.options.Style.Theme != defualtTheme)
                    {
                        Debug.WriteLine("dark mode close!");
                        config.options.Style.Theme = defualtTheme;

                        SetTheme(defualtTheme.ThemeName);

                    }
                }
            }
        }

        public string GetTipWindowStyleVariant()
        {
            string variant = config.options.Style.TipWindowStyleVariant?.Value?.Trim().ToLowerInvariant();
            switch (variant)
            {
                case "privacy":
                case "minimal":
                    return variant;
                default:
                    return "balanced";
            }
        }

        public string GetTipWindowUIFilePath(string themeName, string screenName)
        {
            return $"UI\\{themeName}_{GetTipWindowStyleVariant()}_{NormalizeScreenName(screenName)}.json";
        }

        public UIDesignModel LoadOrCreateTipWindowUI(string themeName, string screenName)
        {
            string uiFilePath = GetTipWindowUIFilePath(themeName, screenName);
            var data = TryReadTipWindowUI(uiFilePath);
            if (data != null && !IsLegacyTipWindowUI(data))
            {
                return data;
            }

            if (GetTipWindowStyleVariant() == "balanced")
            {
                string legacyPath = GetLegacyTipWindowUIFilePath(themeName, screenName);
                if (!string.Equals(uiFilePath, legacyPath, StringComparison.OrdinalIgnoreCase))
                {
                    var legacyData = TryReadTipWindowUI(legacyPath);
                    if (legacyData != null && !IsLegacyTipWindowUI(legacyData))
                    {
                        FileHelper.Write(uiFilePath, JsonConvert.SerializeObject(legacyData));
                        return legacyData;
                    }
                }
            }

            data = GetCreateDefaultTipWindowUI(themeName, screenName);
            FileHelper.Write(uiFilePath, JsonConvert.SerializeObject(data));
            return data;
        }

        /// <summary>
        /// 创建默认的提示界面布局UI
        /// </summary>
        /// <param name="themeName">主题名</param>
        /// <param name="screenName">屏幕名称</param>
        /// <returns></returns>
        public UIDesignModel GetCreateDefaultTipWindowUI(
            string themeName,
            string screenName)
        {
            switch (GetTipWindowStyleVariant())
            {
                case "privacy":
                    return CreatePrivacyTipWindowUI(themeName, screenName);
                case "minimal":
                    return CreateMinimalTipWindowUI(themeName, screenName);
                default:
                    return CreateBalancedTipWindowUI(themeName, screenName);
            }
        }

        public string GetTipWindowMessage()
        {
            const string legacyDefaultZh = "您已持续用眼{t}分钟，休息一会吧！请将注意力集中在至少6米远的地方20秒！";
            const string balancedDefaultZh = "你已经连续看屏幕 {t} 分钟了。";
            const string balancedDefaultEn = "You've been looking at the screen for {t} minutes.";

            string localizedDefault = GetResourceText(
                "Lang_TipWindowMessage",
                config.options.Style.Language?.Value == "en"
                    ? balancedDefaultEn
                    : balancedDefaultZh);

            if (string.IsNullOrWhiteSpace(config.options.Style.TipContent)
                || config.options.Style.TipContent == legacyDefaultZh
                || config.options.Style.TipContent == balancedDefaultZh
                || config.options.Style.TipContent == balancedDefaultEn)
            {
                return localizedDefault;
            }

            return config.options.Style.TipContent;
        }

        public bool IsLegacyTipWindowUI(UIDesignModel data)
        {
            if (data == null || data.ContainerAttr == null || data.Elements == null)
            {
                return false;
            }

            var background = data.ContainerAttr.Background as SolidColorBrush;
            return background != null
                && background.Color == Colors.White
                && data.ContainerAttr.Opacity >= .95
                && data.Elements.Any(m => m.Type == Project1.UI.Controls.Enums.DesignItemType.Image
                    && !string.IsNullOrWhiteSpace(m.Image)
                    && m.Image.Contains("tipImage.png"))
                && data.Elements.Any(m => m.Type == Project1.UI.Controls.Enums.DesignItemType.Button && m.Command == "rest")
                && data.Elements.Any(m => m.Type == Project1.UI.Controls.Enums.DesignItemType.Button && m.Command == "break");
        }

        private UIDesignModel CreateBalancedTipWindowUI(string themeName, string screenName)
        {
            var screenSize = GetScreenSize(screenName);
            bool isDark = themeName == "Dark";
            double screenWidth = screenSize.Width;
            double screenHeight = screenSize.Height;
            double cardWidth = Math.Min(screenWidth - 48, Clamp(screenWidth * 0.42, 460, 620));
            double cardHeight = Math.Min(screenHeight - 48, Clamp(screenHeight * 0.58, 430, 600));
            double cardLeft = screenWidth / 2 - cardWidth / 2;
            double cardTop = screenHeight / 2 - cardHeight / 2;

            var data = CreateBaseDesignModel(
                isDark ? "#11111B" : "#EEF2F9",
                isDark ? .72 : .88,
                isDark ? "#1E1E2E" : "#FBFCFE",
                isDark ? "#313244" : "#D6DCEA",
                isDark ? .97 : .97,
                cardWidth,
                cardHeight,
                26);

            Brush badgeColor = isDark ? Project1UIColor.Get("#CBA6F7") : Project1UIColor.Get("#6D4BD2");
            Brush titleColor = isDark ? Project1UIColor.Get("#F5F7FF") : Project1UIColor.Get("#243147");
            Brush detailColor = isDark ? Project1UIColor.Get("#A6ADC8") : Project1UIColor.Get("#5F6C81");
            Brush countdownColor = isDark ? Project1UIColor.Get("#89B4FA") : Project1UIColor.Get("#3659D6");

            double badgeY = cardTop + 34;
            double imageY = badgeY + 46;
            double tipY = imageY + 154;
            double detailY = tipY + 104;
            double buttonY = detailY + 96;

            data.Elements.Add(CreateTextElement(
                GetResourceText("Lang_TipWindowBadge", isDark ? "Break reminder" : "护眼休息提醒"),
                cardLeft + 48,
                badgeY,
                cardWidth - 96,
                28,
                badgeColor,
                16,
                true,
                1,
                .96));

            data.Elements.Add(CreateImageElement(
                $"pack://application:,,,/20min20s;component/Resources/Themes/{themeName}/Images/tipImage.png",
                screenWidth / 2 - 64,
                imageY,
                128,
                128,
                isDark ? .94 : .98));

            data.Elements.Add(CreateTextElement(
                "{tipcontent}",
                cardLeft + 46,
                tipY,
                cardWidth - 92,
                92,
                titleColor,
                26,
                true,
                1));

            data.Elements.Add(CreateTextElement(
                GetResourceText(
                    "Lang_TipWindowDetail",
                    isDark
                        ? "Look at something at least 20 feet away and let your eyes refocus for 20 seconds."
                        : "请把视线移到至少 6 米远处，给眼睛 20 秒重新对焦。"),
                cardLeft + 56,
                detailY,
                cardWidth - 112,
                56,
                detailColor,
                15,
                false,
                1,
                .94));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_RestNow", "开始休息"),
                "rest",
                "tip_yes",
                screenWidth / 2 - 143,
                buttonY,
                136,
                46,
                15));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_NotNow", "暂时不"),
                "break",
                "tip_no",
                screenWidth / 2 + 7,
                buttonY,
                136,
                46,
                15));

            data.Elements.Add(CreateTextElement(
                "{countdown}",
                screenWidth / 2 - 90,
                buttonY - 8,
                180,
                90,
                countdownColor,
                74,
                true,
                1));

            return data;
        }

        private UIDesignModel CreatePrivacyTipWindowUI(string themeName, string screenName)
        {
            var screenSize = GetScreenSize(screenName);
            bool isDark = themeName == "Dark";
            double screenWidth = screenSize.Width;
            double screenHeight = screenSize.Height;
            double cardWidth = Math.Min(screenWidth - 60, Clamp(screenWidth * 0.36, 420, 520));
            double cardHeight = Math.Min(screenHeight - 60, Clamp(screenHeight * 0.46, 340, 450));
            double cardLeft = screenWidth / 2 - cardWidth / 2;
            double cardTop = screenHeight / 2 - cardHeight / 2;

            var data = CreateBaseDesignModel(
                isDark ? "#0B1020" : "#17212E",
                .6,
                isDark ? "#181825" : "#F7FAFD",
                isDark ? "#313244" : "#CFD8E5",
                isDark ? .95 : .96,
                cardWidth,
                cardHeight,
                24);

            Brush badgeColor = isDark ? Project1UIColor.Get("#CBA6F7") : Project1UIColor.Get("#2F62C9");
            Brush titleColor = isDark ? Project1UIColor.Get("#F5F8FC") : Project1UIColor.Get("#203048");
            Brush detailColor = isDark ? Project1UIColor.Get("#A6ADC8") : Project1UIColor.Get("#617188");
            Brush countdownColor = isDark ? Project1UIColor.Get("#89B4FA") : Project1UIColor.Get("#315CD1");

            double badgeY = cardTop + 34;
            double imageY = badgeY + 36;
            double titleY = imageY + 100;
            double detailY = titleY + 92;
            double buttonY = detailY + 82;

            data.Elements.Add(CreateTextElement(
                GetResourceText("Lang_TipWindowStylePrivacy", isDark ? "Privacy first" : "隐私优先"),
                cardLeft + 42,
                badgeY,
                cardWidth - 84,
                26,
                badgeColor,
                15,
                true,
                1,
                .95));

            data.Elements.Add(CreateImageElement(
                $"pack://application:,,,/20min20s;component/Resources/Themes/{themeName}/Images/tipImage.png",
                screenWidth / 2 - 42,
                imageY,
                84,
                84,
                isDark ? .88 : .95));

            data.Elements.Add(CreateTextElement(
                "{tipcontent}",
                cardLeft + 40,
                titleY,
                cardWidth - 80,
                82,
                titleColor,
                30,
                true,
                1));

            data.Elements.Add(CreateTextElement(
                GetResourceText(
                    "Lang_TipWindowDetail",
                    isDark
                        ? "Take a short visual break. The background is softened so the screen stays private."
                        : "短暂把目光移开屏幕，界面会更低调，也不容易暴露原来的内容。"),
                cardLeft + 48,
                detailY,
                cardWidth - 96,
                52,
                detailColor,
                14,
                false,
                1,
                .92));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_RestNow", "开始休息"),
                "rest",
                "tip_yes",
                screenWidth / 2 - 131,
                buttonY,
                124,
                42,
                14));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_NotNow", "暂时不"),
                "break",
                "tip_no",
                screenWidth / 2 + 7,
                buttonY,
                124,
                42,
                14));

            data.Elements.Add(CreateTextElement(
                "{countdown}",
                screenWidth / 2 - 70,
                buttonY - 68,
                140,
                56,
                countdownColor,
                42,
                true,
                1));

            return data;
        }

        private UIDesignModel CreateMinimalTipWindowUI(string themeName, string screenName)
        {
            var screenSize = GetScreenSize(screenName);
            bool isDark = themeName == "Dark";
            double screenWidth = screenSize.Width;
            double screenHeight = screenSize.Height;
            double cardWidth = Math.Min(screenWidth - 72, Clamp(screenWidth * 0.34, 360, 460));
            double cardHeight = Math.Min(screenHeight - 72, Clamp(screenHeight * 0.34, 260, 320));
            double cardLeft = screenWidth / 2 - cardWidth / 2;
            double cardTop = screenHeight / 2 - cardHeight / 2;

            var data = CreateBaseDesignModel(
                isDark ? "#0E1116" : "#20252E",
                .48,
                isDark ? "#181825" : "#F8FAFD",
                isDark ? "#313244" : "#D6DDE8",
                isDark ? .88 : .92,
                cardWidth,
                cardHeight,
                22);

            Brush badgeColor = isDark ? Project1UIColor.Get("#CBA6F7") : Project1UIColor.Get("#4962CC");
            Brush titleColor = isDark ? Project1UIColor.Get("#F4F6FA") : Project1UIColor.Get("#223248");
            Brush detailColor = isDark ? Project1UIColor.Get("#A6ADC8") : Project1UIColor.Get("#6A788C");
            Brush countdownColor = isDark ? Project1UIColor.Get("#89B4FA") : Project1UIColor.Get("#3B5FD6");

            double badgeY = cardTop + 24;
            double imageY = badgeY + 28;
            double titleY = imageY + 82;
            double detailY = titleY + 62;
            double buttonY = detailY + 58;

            data.Elements.Add(CreateTextElement(
                GetResourceText("Lang_TipWindowStyleMinimal", isDark ? "Minimal low profile" : "极简低打扰"),
                cardLeft + 34,
                badgeY,
                cardWidth - 68,
                22,
                badgeColor,
                13,
                true,
                1,
                .94));

            data.Elements.Add(CreateImageElement(
                $"pack://application:,,,/20min20s;component/Resources/Themes/{themeName}/Images/tipImage.png",
                screenWidth / 2 - 30,
                imageY,
                60,
                60,
                isDark ? .82 : .92));

            data.Elements.Add(CreateTextElement(
                "{tipcontent}",
                cardLeft + 30,
                titleY,
                cardWidth - 60,
                58,
                titleColor,
                21,
                true,
                1));

            data.Elements.Add(CreateTextElement(
                GetResourceText(
                    "Lang_TipWindowDetail",
                    isDark
                        ? "A small break now will ease eye strain."
                        : "现在休息一会，眼睛会轻松很多。"),
                cardLeft + 34,
                detailY,
                cardWidth - 68,
                36,
                detailColor,
                13,
                false,
                1,
                .9));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_RestNow", "开始休息"),
                "rest",
                "tip_yes",
                screenWidth / 2 - 107,
                buttonY,
                100,
                38,
                13));

            data.Elements.Add(CreateButtonElement(
                GetResourceText("Lang_NotNow", "暂时不"),
                "break",
                "tip_no",
                screenWidth / 2 + 7,
                buttonY,
                100,
                38,
                13));

            data.Elements.Add(CreateTextElement(
                "{countdown}",
                screenWidth / 2 - 46,
                badgeY + 4,
                92,
                30,
                countdownColor,
                24,
                true,
                1));

            return data;
        }

        private static UIDesignModel CreateBaseDesignModel(
            string background,
            double backgroundOpacity,
            string panelBackground,
            string panelBorderBrush,
            double panelOpacity,
            double panelWidth,
            double panelHeight,
            double panelCornerRadius)
        {
            return new UIDesignModel()
            {
                ContainerAttr = new ContainerModel()
                {
                    Background = Project1UIColor.Get(background),
                    Opacity = backgroundOpacity,
                    CenterPanelBackground = Project1UIColor.Get(panelBackground),
                    CenterPanelBorderBrush = Project1UIColor.Get(panelBorderBrush),
                    CenterPanelOpacity = panelOpacity,
                    CenterPanelBorderThickness = 1,
                    CenterPanelCornerRadius = panelCornerRadius,
                    CenterPanelWidth = panelWidth,
                    CenterPanelHeight = panelHeight
                },
                Elements = new List<ElementModel>()
            };
        }

        private static ElementModel CreateTextElement(
            string text,
            double x,
            double y,
            double width,
            double height,
            Brush textColor,
            double fontSize,
            bool isBold = false,
            int textAlignment = 0,
            double opacity = 1)
        {
            return new ElementModel()
            {
                Type = Project1.UI.Controls.Enums.DesignItemType.Text,
                Text = text,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                TextColor = textColor,
                FontSize = fontSize,
                IsTextBold = isBold,
                TextAlignment = textAlignment,
                Opacity = opacity
            };
        }

        private static ElementModel CreateButtonElement(
            string text,
            string command,
            string style,
            double x,
            double y,
            double width,
            double height,
            double fontSize,
            double opacity = 1)
        {
            return new ElementModel()
            {
                Type = Project1.UI.Controls.Enums.DesignItemType.Button,
                Text = text,
                Command = command,
                Style = style,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                FontSize = fontSize,
                Opacity = opacity
            };
        }

        private static ElementModel CreateImageElement(
            string image,
            double x,
            double y,
            double width,
            double height,
            double opacity = 1)
        {
            return new ElementModel()
            {
                Type = Project1.UI.Controls.Enums.DesignItemType.Image,
                Image = image,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Opacity = opacity
            };
        }

        private static UIDesignModel TryReadTipWindowUI(string uiFilePath)
        {
            if (!FileHelper.Exists(uiFilePath))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<UIDesignModel>(FileHelper.Read(uiFilePath));
            }
            catch
            {
                return null;
            }
        }

        private static WindowManager.Size GetScreenSize(string screenName)
        {
            screenName = NormalizeScreenName(screenName);
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (!string.IsNullOrEmpty(screenName))
            {
                foreach (var item in System.Windows.Forms.Screen.AllScreens)
                {
                    string itemScreenName = NormalizeScreenName(item.DeviceName);
                    if (itemScreenName == screenName)
                    {
                        screen = item;
                        break;
                    }
                }
            }

            return WindowManager.GetSize(screen);
        }

        private static string NormalizeScreenName(string screenName)
        {
            return string.IsNullOrWhiteSpace(screenName) ? string.Empty : screenName.Replace("\\", "");
        }

        private static string GetLegacyTipWindowUIFilePath(string themeName, string screenName)
        {
            return $"UI\\{themeName}_{NormalizeScreenName(screenName)}.json";
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static string GetResourceText(string key, string fallback)
        {
            return Application.Current?.Resources[key]?.ToString() ?? fallback;
        }
    }
}
