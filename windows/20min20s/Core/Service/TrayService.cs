using ProjectEye.Core.Models.Options;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;
using System.Windows.Threading;

namespace ProjectEye.Core.Service
{
    /// <summary>
    /// 管理和显示托盘图标
    /// </summary>
    public class TrayService : IService
    {
        //托盘图标
        private System.Windows.Forms.NotifyIcon notifyIcon;

        //Service
        private readonly MainService mainService;
        private readonly ConfigService config;
        private readonly BackgroundWorkerService backgroundWorker;
        private readonly ThemeService theme;
        //托盘菜单项
        private ContextMenu contextMenu;
        private MenuItem menuItem_Status;
        private MenuItem menuItem_NoReset;
        private MenuItem menuItem_Statistic;
        private MenuItem menuItem_Options;
        private MenuItem menuItem_Quit;

        private MenuItem menuItem_NoReset_OneHour;
        private MenuItem menuItem_NoReset_TwoHour;
        private MenuItem menuItem_NoReset_Forver;
        private MenuItem menuItem_NoReset_Off;

        private DispatcherTimer noresetTimer;
        private DateTime? noresetEndsAt;
        private bool trayMenuDeactivateHooked;

        private string lastIcon = string.Empty;

        //event
        /// <summary>
        /// 鼠标单击托盘图标时发生
        /// </summary>
        public event System.Windows.Forms.MouseEventHandler MouseClickTrayIcon;
        /// <summary>
        /// 鼠标停留在托盘图标上时发生
        /// </summary>
        public event System.Windows.Forms.MouseEventHandler MouseMoveTrayIcon;
        public TrayService(
            App app,
            MainService mainService,
            ConfigService config,
            BackgroundWorkerService backgroundWorker,
            ThemeService theme)
        {
            this.mainService = mainService;
            this.config = config;
            this.backgroundWorker = backgroundWorker;
            this.theme = theme;
            this.config.Changed += new EventHandler(config_Changed);
            this.theme.OnChangedTheme += Theme_OnChangedTheme;
            app.Exit += new ExitEventHandler(app_Exit);
            mainService.OnLeaveEvent += MainService_OnLeaveEvent;
            mainService.OnStart += MainService_OnStart;
            mainService.OnLoadedLanguage += MainService_OnLoadedLanguage;
            mainService.OnRuntimeStatusChanged += MainService_OnRuntimeStatusChanged;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.OnCompleted += BackgroundWorker_OnCompleted;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
        }

        private void MainService_OnLoadedLanguage(object service, int msg)
        {
            CreateTrayMenu();
            RefreshRuntimeStatus();
        }

        //主题更改时
        private void Theme_OnChangedTheme(string OldThemeName, string NewThemeName)
        {
            CreateTrayMenu();
            RefreshRuntimeStatus();
        }

        private void MainService_OnStart(object service, int msg)
        {
            RefreshRuntimeStatus();
            if (contextMenu != null && !config.options.General.Noreset)
            {
                menuItem_NoReset_OneHour.IsChecked = false;
                menuItem_NoReset_TwoHour.IsChecked = false;
                menuItem_NoReset_Forver.IsChecked = false;
                menuItem_NoReset.IsChecked = false;
                menuItem_NoReset_Off.IsChecked = true;
            }
        }

        private void MainService_OnLeaveEvent(object service, int msg)
        {
            RefreshRuntimeStatus();
        }

        private void MainService_OnRuntimeStatusChanged(object service, int msg)
        {
            RefreshRuntimeStatus();
        }

        #region Init
        public void Init()
        {
            //托盘菜单
            CreateTrayMenu();


            notifyIcon.Visible = true;
            notifyIcon.MouseMove += NotifyIcon_MouseMove;
            notifyIcon.MouseClick += notifyIcon_MouseClick;
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

            noresetTimer = new DispatcherTimer();
            noresetTimer.Tick += NoresetTimer_Tick;

        }
        #endregion

        #region Events
        private void NotifyIcon_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            RefreshRuntimeStatus();
            MouseMoveTrayIcon?.Invoke(sender, e);
        }

        //有后台工作任务在运行时
        private void BackgroundWorker_DoWork()
        {
            UpdateIcon("overheated", false);
            SetText($"20min20s: {Application.Current.Resources["Lang_TimeconsumingOperation"]}");
            UpdateStatusMenu(
                "20min20s",
                Application.Current.Resources["Lang_TimeconsumingOperation"]?.ToString(),
                System.Windows.Media.Color.FromRgb(239, 68, 68));
        }
        //后台工作任务运行结束时
        private void BackgroundWorker_OnCompleted()
        {
            RefreshRuntimeStatus();
        }

        private void MenuItem_NoReset_Off_Click(object sender, RoutedEventArgs e)
        {
            OnNoResetAction(sender, -1);
        }

        private void MenuItem_NoReset_Forver_Click(object sender, RoutedEventArgs e)
        {
            OnNoResetAction(sender, 0);
        }

        private void MenuItem_NoReset_TwoHour_Click(object sender, RoutedEventArgs e)
        {
            OnNoResetAction(sender, 2);
        }

        private void MenuItem_NoReset_OneHour_Click(object sender, RoutedEventArgs e)
        {
            OnNoResetAction(sender, 1);
        }
        private void menuItem_Statistic_Click(object sender, EventArgs e)
        {
            WindowManager.CreateWindowInScreen("StatisticWindow");
            WindowManager.Show("StatisticWindow");
        }

        private void config_Changed(object sender, EventArgs e)
        {
            menuItem_NoReset.IsChecked = config.options.General.Noreset;
            menuItem_Statistic.Visibility = config.options.General.Data ? Visibility.Visible : Visibility.Collapsed;


            var oldOptions = sender as OptionsModel;
            if (oldOptions.General.IsTomatoMode != config.options.General.IsTomatoMode)
            {
                RefreshRuntimeStatus();
                if (config.options.General.IsTomatoMode)
                {
                    menuItem_NoReset.Visibility = Visibility.Collapsed;
                }
                else
                {
                    menuItem_NoReset.Visibility = Visibility.Visible;
                }
            }
            else
            {
                RefreshRuntimeStatus();
            }
        }

        private void menuItem_Options_Click(object sender, EventArgs e)
        {
            WindowManager.CreateWindowInScreen("OptionsWindow");
            WindowManager.Show("OptionsWindow");
        }
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            theme.HandleDarkMode();
            MouseClickTrayIcon?.Invoke(sender, e);
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (backgroundWorker.IsBusy)
                {
                    return;
                }
                //右键单击弹出托盘菜单
                contextMenu.IsOpen = true;
                //激活主窗口，用于处理关闭托盘菜单
                App.Current.MainWindow?.Activate();

            }
        }

        private void menuItem_Exit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void app_Exit(object sender, ExitEventArgs e)
        {
            mainService.Exit();
            Remove();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && !backgroundWorker.IsBusy)
            {
                //双击托盘图标进入或退出番茄时钟模式
                config.SaveOldOptions();
                config.options.General.IsTomatoMode = !config.options.General.IsTomatoMode;
                config.OnChanged();

            }
        }
        #endregion

        #region Function
        private void CreateTrayMenu()
        {
            contextMenu = new ContextMenu();
            contextMenu.MinWidth = 288;
            if (!trayMenuDeactivateHooked)
            {
                App.Current.Deactivated += (e, c) =>
                {
                    contextMenu.IsOpen = false;
                };
                trayMenuDeactivateHooked = true;
            }

            menuItem_Status = new MenuItem();
            menuItem_Status.Focusable = false;
            menuItem_Status.IsHitTestVisible = false;
            menuItem_Status.StaysOpenOnClick = true;
            menuItem_Status.Header = BuildStatusHeader(
                "20min20s",
                Application.Current.Resources["Lang_EffectiveUsageUntilNextBreak"]?.ToString(),
                System.Windows.Media.Color.FromRgb(203, 166, 247));
            //托盘菜单项
            menuItem_Statistic = new MenuItem();
            menuItem_Statistic.Header = BuildActionHeader(
                Application.Current.Resources["Lang_Statistics"]?.ToString(),
                Application.Current.Resources["Lang_StatisticsWindow"]?.ToString(),
                "\xE9D2",
                System.Windows.Media.Color.FromRgb(203, 166, 247));
            menuItem_Statistic.Visibility = config.options.General.Data ? Visibility.Visible : Visibility.Collapsed;
            menuItem_Statistic.Click += menuItem_Statistic_Click;

            menuItem_Options = new MenuItem();
            menuItem_Options.Header = BuildActionHeader(
                Application.Current.Resources["Lang_Settings"]?.ToString(),
                Application.Current.Resources["Lang_Optionupdated"]?.ToString(),
                "\xE713",
                System.Windows.Media.Color.FromRgb(203, 166, 247));
            menuItem_Options.Click += menuItem_Options_Click;


            menuItem_NoReset = new MenuItem();
            menuItem_NoReset.Header = BuildActionHeader(
                Application.Current.Resources["Lang_Suspendnow"]?.ToString(),
                GetNoResetMenuSummary(),
                "\xE769",
                System.Windows.Media.Color.FromRgb(203, 166, 247),
                true);

            menuItem_NoReset_OneHour = new MenuItem();
            menuItem_NoReset_OneHour.Header = Application.Current.Resources["Lang_Onehours"];
            menuItem_NoReset_OneHour.Icon = CreateMenuGlyph("\xE121");
            menuItem_NoReset_OneHour.Click += MenuItem_NoReset_OneHour_Click;
            menuItem_NoReset_TwoHour = new MenuItem();
            menuItem_NoReset_TwoHour.Header = Application.Current.Resources["Lang_Twohours"];
            menuItem_NoReset_TwoHour.Icon = CreateMenuGlyph("\xE121");
            menuItem_NoReset_TwoHour.Click += MenuItem_NoReset_TwoHour_Click;
            menuItem_NoReset_Forver = new MenuItem();
            menuItem_NoReset_Forver.Header = Application.Current.Resources["Lang_Suspenduntilnextstartup"];
            menuItem_NoReset_Forver.Icon = CreateMenuGlyph("\xE823");
            menuItem_NoReset_Forver.Click += MenuItem_NoReset_Forver_Click;
            menuItem_NoReset_Off = new MenuItem();
            menuItem_NoReset_Off.Header = Application.Current.Resources["Lang_Disabled"];
            menuItem_NoReset_Off.Icon = CreateMenuGlyph("\xE711");
            menuItem_NoReset_Off.IsChecked = true;
            menuItem_NoReset_Off.Click += MenuItem_NoReset_Off_Click;

            menuItem_NoReset.Items.Add(menuItem_NoReset_OneHour);
            menuItem_NoReset.Items.Add(menuItem_NoReset_TwoHour);
            menuItem_NoReset.Items.Add(menuItem_NoReset_Forver);
            menuItem_NoReset.Items.Add(menuItem_NoReset_Off);

            menuItem_Quit = new MenuItem();
            menuItem_Quit.Header = BuildActionHeader(
                Application.Current.Resources["Lang_Quit"]?.ToString(),
                "20min20s",
                "\xE8BB",
                System.Windows.Media.Color.FromRgb(248, 113, 113));
            menuItem_Quit.Click += menuItem_Exit_Click;

            //添加托盘菜单项
            contextMenu.Items.Add(menuItem_Status);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(menuItem_Statistic);
            contextMenu.Items.Add(menuItem_Options);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(menuItem_NoReset);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(menuItem_Quit);

        }
        public void Remove()
        {
            notifyIcon.Visible = false;
        }
        public void UpdateIcon(string name = "", bool save = true)
        {
            name = name == "" ? lastIcon : name;
            if (name == "")
            {
                name = "sunglasses";
            }
            if (notifyIcon != null && name != "")
            {
                Uri iconUri = new Uri("/20min20s;component/Resources/" + name + ".ico", UriKind.RelativeOrAbsolute);
                StreamResourceInfo info = Application.GetResourceStream(iconUri);
                notifyIcon.Icon = new Icon(info.Stream);
                if (save)
                {
                    lastIcon = name;
                }
            }
        }
        /// <summary>
        /// 设置不提醒操作
        /// </summary>
        /// <param name="hour">-1时关闭；0打开；大于0则在到达设定的值（小时）后重新启动</param>
        private void SetNoReset(int hour)
        {
            config.options.General.Noreset = true;
            noresetEndsAt = null;
            menuItem_NoReset_OneHour.IsChecked = false;
            menuItem_NoReset_TwoHour.IsChecked = false;
            menuItem_NoReset_Forver.IsChecked = false;
            menuItem_NoReset_Off.IsChecked = false;
            menuItem_NoReset.IsChecked = true;
            noresetTimer.Stop();
            UpdateIcon("dizzy");
            if (hour == -1)
            {
                //关闭
                config.options.General.Noreset = false;
                noresetEndsAt = null;
                menuItem_NoReset.IsChecked = false;
                mainService.Start();
                UpdateIcon("sunglasses");

            }
            else if (hour == 0)
            {
                //直到下次启动
                noresetEndsAt = null;
                menuItem_NoReset.IsChecked = true;
                mainService.Pause(false);
            }
            else
            {
                //指定计时
                menuItem_NoReset.IsChecked = true;
                mainService.Pause(false);
                noresetEndsAt = DateTime.Now.AddHours(hour);

                noresetTimer.Interval = new TimeSpan(hour, 0, 0);
                noresetTimer.Start();
            }
        }
        private void OnNoResetAction(object sender, int hour)
        {
            var item = sender as MenuItem;
            if (!item.IsChecked)
            {
                SetNoReset(hour);
                item.IsChecked = true;
            }
        }

        /// <summary>
        /// 设置托盘图标文本
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            notifyIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
        }

        private void RefreshRuntimeStatus()
        {
            if (notifyIcon == null || backgroundWorker.IsBusy)
            {
                return;
            }

            string statusTitle = "20min20s";
            string statusDetail = string.Empty;
            System.Windows.Media.Color accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);

            if (config.options.General.IsTomatoMode)
            {
                UpdateIcon("tomato");
                SetText("20min20s");
                statusTitle = Application.Current.Resources["Lang_TomatoTimer"]?.ToString();
                statusDetail = Application.Current.Resources["Lang_Tomato"]?.ToString();
                accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);
                UpdateStatusMenu(statusTitle, statusDetail, accentColor);
                return;
            }

            if (config.options.General.Noreset)
            {
                UpdateIcon("dizzy");
                SetText($"20min20s: {Application.Current.Resources["Lang_Reminderisoff"]}");
                statusTitle = Application.Current.Resources["Lang_Reminderisoff"]?.ToString();
                statusDetail = GetNoResetDetail();
                accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);
                UpdateStatusMenu(statusTitle, statusDetail, accentColor);
                return;
            }

            switch (mainService.GetRuntimeStatus())
            {
                case RuntimeStatus.PausedInactivity:
                    UpdateIcon("sleeping");
                    SetText($"20min20s: {Application.Current.Resources["Lang_PausedDueToInactivity"]}");
                    statusTitle = Application.Current.Resources["Lang_PausedDueToInactivity"]?.ToString();
                    statusDetail = FormatRemainingTime(mainService.GetRestCountdownMinutes());
                    accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);
                    UpdateStatusMenu(statusTitle, statusDetail, accentColor);
                    return;
                case RuntimeStatus.DeferredBreak:
                    UpdateIcon("overheated");
                    SetText($"20min20s: {Application.Current.Resources["Lang_BreakReminderPending"]}");
                    statusTitle = Application.Current.Resources["Lang_BreakReminderPending"]?.ToString();
                    statusDetail = Application.Current.Resources["Lang_Breaktimeisstartingsoon"]?.ToString();
                    accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);
                    UpdateStatusMenu(statusTitle, statusDetail, accentColor);
                    return;
                default:
                    UpdateIcon("sunglasses");
                    statusTitle = Application.Current.Resources["Lang_EffectiveUsageUntilNextBreak"]?.ToString();
                    if (mainService.IsWorkTimerRun())
                    {
                        statusDetail = FormatRemainingTime(mainService.GetRestCountdownMinutes());
                        SetText($"20min20s\r\n{Application.Current.Resources["Lang_EffectiveUsageUntilNextBreak"]}: {statusDetail}");
                    }
                    else
                    {
                        statusDetail = string.Empty;
                        SetText("20min20s");
                    }
                    accentColor = System.Windows.Media.Color.FromRgb(203, 166, 247);
                    UpdateStatusMenu(statusTitle, statusDetail, accentColor);
                    return;
            }
        }

        private void NoresetTimer_Tick(object sender, EventArgs e)
        {
            SetNoReset(-1);
            menuItem_NoReset_Off.IsChecked = true;
            noresetTimer.Stop();
        }

        private string GetNoResetDetail()
        {
            if (noresetEndsAt.HasValue)
            {
                TimeSpan remaining = noresetEndsAt.Value - DateTime.Now;
                if (remaining > TimeSpan.Zero)
                {
                    return FormatDuration(remaining);
                }
            }

            return Application.Current.Resources["Lang_Suspenduntilnextstartup"]?.ToString();
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                int hours = Math.Max(0, (int)duration.TotalHours);
                int minutes = Math.Max(0, duration.Minutes);
                if (minutes == 0)
                {
                    return $"{hours}{Application.Current.Resources["Lang_Hours_n"]}";
                }
                return $"{hours}{Application.Current.Resources["Lang_Hours_n"]} {minutes}{Application.Current.Resources["Lang_Minutes_n"]}";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes))}{Application.Current.Resources["Lang_Minutes_n"]}";
            }

            return $"{Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds))}{Application.Current.Resources["Lang_Seconds_n"]}";
        }

        private void UpdateStatusMenu(string title, string detail, System.Windows.Media.Color accentColor)
        {
            if (menuItem_Status == null)
            {
                return;
            }

            menuItem_Status.Header = BuildStatusHeader(title, detail, accentColor);
            if (menuItem_NoReset != null)
            {
                menuItem_NoReset.Header = BuildActionHeader(
                    Application.Current.Resources["Lang_Suspendnow"]?.ToString(),
                    GetNoResetMenuSummary(),
                    "\xE769",
                    System.Windows.Media.Color.FromRgb(166, 227, 161),
                    true);
            }
        }

        private object BuildStatusHeader(string title, string detail, System.Windows.Media.Color accentColor)
        {
            System.Windows.Media.Brush accentBrush = new System.Windows.Media.SolidColorBrush(accentColor);
            System.Windows.Media.Brush surfaceBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 46));
            System.Windows.Media.Brush titleBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(205, 214, 244));
            System.Windows.Media.Brush detailBrush = GetBrushWithOpacity(titleBrush, 0.72);
            var shell = new Grid
            {
                ClipToBounds = true
            };

            var topStrip = new Grid
            {
                Height = 3,
                Background = accentBrush,
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var titleText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(title) ? "20min20s" : title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = titleBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var content = new StackPanel();
            content.Children.Add(headerGrid);
            content.Children.Add(titleText);

            if (!string.IsNullOrWhiteSpace(detail))
            {
                content.Children.Add(new TextBlock
                {
                    Text = detail,
                    Margin = new Thickness(0, 6, 0, 0),
                    FontSize = 11,
                    Foreground = detailBrush,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var contentContainer = new Border
            {
                Margin = new Thickness(0, 3, 0, 0),
                Padding = new Thickness(14, 12, 14, 12),
                Child = content
            };
            shell.Children.Add(topStrip);
            shell.Children.Add(contentContainer);

            return new Border
            {
                Margin = new Thickness(8, 8, 8, 6),
                CornerRadius = new CornerRadius(16),
                Background = surfaceBrush,
                BorderThickness = new Thickness(0),
                ClipToBounds = true,
                Child = shell
            };
        }

        private object BuildActionHeader(string title, string subtitle, string glyph, System.Windows.Media.Color accentColor, bool showChevron = false)
        {
            var accentBrush = new System.Windows.Media.SolidColorBrush(accentColor);
            var titleBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(205, 214, 244));
            var subtitleBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(166, 173, 200));

            var layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            layout.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            if (showChevron)
            {
                layout.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }

            var iconBox = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(8),
                Background = System.Windows.Media.Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = glyph,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                    FontSize = 15,
                    Foreground = accentBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };

            var textStack = new StackPanel
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = titleBrush
            });

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    Margin = new Thickness(0, 1, 0, 0),
                    FontSize = 11,
                    Foreground = subtitleBrush,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            layout.Children.Add(iconBox);
            Grid.SetColumn(textStack, 1);
            layout.Children.Add(textStack);

            if (showChevron)
            {
                var chevron = new TextBlock
                {
                    Text = "\xE76C",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                    FontSize = 11,
                    Foreground = subtitleBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(chevron, 2);
                layout.Children.Add(chevron);
            }

            return layout;
        }

        private TextBlock CreateMenuGlyph(string glyph)
        {
            return new TextBlock
            {
                Text = glyph,
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(173, 183, 214)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
        }

        private System.Windows.Media.Brush GetBrushResource(string key, System.Windows.Media.Brush fallback)
        {
            return Application.Current.Resources[key] as System.Windows.Media.Brush ?? fallback;
        }

        private System.Windows.Media.Brush GetBrushWithOpacity(System.Windows.Media.Brush source, double opacity)
        {
            System.Windows.Media.Brush cloned = source?.CloneCurrentValue() ?? System.Windows.Media.Brushes.Gray.CloneCurrentValue();
            cloned.Opacity = opacity;
            return cloned;
        }

        private string FormatRemainingTime(double restCountdownMinutes)
        {
            string restStr = Math.Round(restCountdownMinutes, 1) + $"{Application.Current.Resources["Lang_Minutes_n"]}";
            if (restCountdownMinutes < 1)
            {
                restStr = Math.Round(restCountdownMinutes * 60, 0) + $"{Application.Current.Resources["Lang_Seconds_n"]}";
            }
            if (restCountdownMinutes > 60)
            {
                double roundedHours = Math.Round(restCountdownMinutes / 60, 1);
                restStr = $"{roundedHours}{Application.Current.Resources["Lang_Hours_n"]}";
                if (roundedHours.ToString().IndexOf(".") != -1)
                {
                    string[] parts = roundedHours.ToString().Split('.');
                    restStr = $"{parts[0]}{Application.Current.Resources["Lang_Hours_n"]} {parts[1]}{Application.Current.Resources["Lang_Minutes_n"]}";
                }
            }

            return restStr;
        }

        private string GetNoResetMenuSummary()
        {
            return config.options.General.Noreset
                ? GetNoResetDetail()
                : Application.Current.Resources["Lang_Disabled"]?.ToString();
        }

        /// <summary>
        /// 显示气泡或通知（在windows7上是任务栏气泡，win10上是系统通知）
        /// </summary>
        public void BalloonTipIcon(string title, string content, System.Windows.Forms.ToolTipIcon icon = System.Windows.Forms.ToolTipIcon.None)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = content;
            notifyIcon.BalloonTipIcon = icon;
            notifyIcon.ShowBalloonTip(5000);
        }
        #endregion
    }
}
