using Project1.UI.Controls.Models;
using Project1.UI.Cores;
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
            screenName = screenName.Replace("\\", "");

            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screenName != string.Empty)
            {
                foreach (var item in System.Windows.Forms.Screen.AllScreens)
                {
                    string itemScreenName = item.DeviceName.Replace("\\", "");
                    if (itemScreenName == screenName)
                    {
                        screen = item;
                        break;
                    }
                }
            }

            var screenSize = WindowManager.GetSize(screen);

            //创建默认布局
            var data = new UIDesignModel();
            bool isDark = themeName == "Dark";
            double screenWidth = screenSize.Width;
            double screenHeight = screenSize.Height;
            double cardWidth = Math.Min(screenWidth - 48, Clamp(screenWidth * 0.42, 460, 620));
            double cardHeight = Math.Min(screenHeight - 48, Clamp(screenHeight * 0.58, 430, 600));
            double cardLeft = screenWidth / 2 - cardWidth / 2;
            double cardTop = screenHeight / 2 - cardHeight / 2;

            data.ContainerAttr = new ContainerModel()
            {
                Background = isDark ? Project1UIColor.Get("#14161A") : Project1UIColor.Get("#F1F4F8"),
                Opacity = isDark ? .84 : .9,
                CenterPanelBackground = isDark ? Project1UIColor.Get("#1E232D") : Project1UIColor.Get("#FCFDFE"),
                CenterPanelBorderBrush = isDark ? Project1UIColor.Get("#31394A") : Project1UIColor.Get("#D9E1EC"),
                CenterPanelOpacity = isDark ? .94 : .96,
                CenterPanelBorderThickness = 1,
                CenterPanelCornerRadius = 28,
                CenterPanelWidth = cardWidth,
                CenterPanelHeight = cardHeight
            };

            var elements = new List<ElementModel>();
            Brush badgeColor = isDark ? Project1UIColor.Get("#8EA8FF") : Project1UIColor.Get("#4568D1");
            Brush titleColor = isDark ? Project1UIColor.Get("#F3F6FB") : Project1UIColor.Get("#243147");
            Brush detailColor = isDark ? Project1UIColor.Get("#B7C0D1") : Project1UIColor.Get("#5F6C81");
            Brush countdownColor = isDark ? Project1UIColor.Get("#8EA8FF") : Project1UIColor.Get("#3659D6");

            var badge = new ElementModel();
            badge.Type = Project1.UI.Controls.Enums.DesignItemType.Text;
            badge.Text = GetResourceText("Lang_TipWindowBadge", isDark ? "Break reminder" : "护眼休息提醒");
            badge.Opacity = .96;
            badge.TextColor = badgeColor;
            badge.Width = cardWidth - 96;
            badge.Height = 28;
            badge.X = cardLeft + 48;
            badge.Y = cardTop + 34;
            badge.FontSize = 16;
            badge.IsTextBold = true;
            badge.TextAlignment = 1;

            var tipimage = new ElementModel();
            tipimage.Type = Project1.UI.Controls.Enums.DesignItemType.Image;
            tipimage.Width = 156;
            tipimage.Opacity = 1;
            tipimage.Height = 156;
            tipimage.Image = $"pack://application:,,,/20min20s;component/Resources/Themes/{themeName}/Images/tipImage.png";
            tipimage.X = screenWidth / 2 - tipimage.Width / 2;
            tipimage.Y = badge.Y + badge.Height + 18;

            var tipText = new ElementModel();
            tipText.Type = Project1.UI.Controls.Enums.DesignItemType.Text;
            tipText.Text = GetDefaultTipMessage();
            tipText.Opacity = 1;
            tipText.TextColor = titleColor;
            tipText.Width = cardWidth - 92;
            tipText.Height = 92;
            tipText.X = cardLeft + 46;
            tipText.Y = tipimage.Y + tipimage.Height + 22;
            tipText.FontSize = 26;
            tipText.IsTextBold = true;
            tipText.TextAlignment = 1;

            var detailText = new ElementModel();
            detailText.Type = Project1.UI.Controls.Enums.DesignItemType.Text;
            detailText.Text = GetResourceText(
                "Lang_TipWindowDetail",
                isDark
                    ? "Look at something at least 20 feet away and let your eyes refocus for 20 seconds."
                    : "请把视线移到至少 6 米远处，给眼睛 20 秒重新对焦。");
            detailText.Opacity = .94;
            detailText.TextColor = detailColor;
            detailText.Width = cardWidth - 112;
            detailText.Height = 56;
            detailText.X = cardLeft + 56;
            detailText.Y = tipText.Y + tipText.Height + 4;
            detailText.FontSize = 15;
            detailText.TextAlignment = 1;

            var restBtn = new ElementModel();
            restBtn.Type = Project1.UI.Controls.Enums.DesignItemType.Button;
            restBtn.Width = 136;
            restBtn.Height = 46;
            restBtn.FontSize = 15;
            restBtn.Text = GetResourceText("Lang_RestNow", "开始休息");
            restBtn.Opacity = 1;
            restBtn.Command = "rest";
            restBtn.Style = "tip_yes";
            restBtn.X = screenWidth / 2 - (restBtn.Width * 2 + 14) / 2;
            restBtn.Y = detailText.Y + detailText.Height + 40;

            var breakBtn = new ElementModel();
            breakBtn.Type = Project1.UI.Controls.Enums.DesignItemType.Button;
            breakBtn.Width = 136;
            breakBtn.Height = 46;
            breakBtn.FontSize = 15;
            breakBtn.Text = GetResourceText("Lang_NotNow", "暂时不");
            breakBtn.Style = "tip_no";
            breakBtn.Command = "break";
            breakBtn.Opacity = 1;
            breakBtn.X = restBtn.X + restBtn.Width + 14;
            breakBtn.Y = restBtn.Y;

            var countDownText = new ElementModel();
            countDownText.Text = "{countdown}";
            countDownText.FontSize = 74;
            countDownText.IsTextBold = true;
            countDownText.Type = Project1.UI.Controls.Enums.DesignItemType.Text;
            countDownText.TextColor = countdownColor;
            countDownText.Opacity = 1;
            countDownText.Width = 180;
            countDownText.Height = 90;
            countDownText.X = screenWidth / 2 - countDownText.Width / 2;
            countDownText.Y = restBtn.Y - 8;
            countDownText.TextAlignment = 1;

            elements.Add(badge);
            elements.Add(tipimage);
            elements.Add(tipText);
            elements.Add(detailText);
            elements.Add(restBtn);
            elements.Add(breakBtn);
            elements.Add(countDownText);


            data.Elements = elements;

            return data;
        }

        private string GetDefaultTipMessage()
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
