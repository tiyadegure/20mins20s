using Microsoft.Win32;
using ProjectEye.Core.Models.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace ProjectEye.Core.Service
{
    public enum RuntimeStatus
    {
        Running,
        PausedInactivity,
        DeferredBreak
    }

    /// <summary>
    /// Main Service
    /// </summary>
    public class MainService : IService
    {
        /// <summary>
        /// 用眼计时器
        /// </summary>
        private DispatcherTimer work_timer;
        System.Diagnostics.Stopwatch workTimerStopwatch;
        private int configuredWarnMinutes;
        private TimeSpan workRemaining;
        private DateTime? deferredReminderReadyAt;
        private bool isBreakDeferred;
        private DispatcherTimer deferred_break_timer;
        /// <summary>
        /// 离开检测计时器
        /// </summary>
        private DispatcherTimer leave_timer;
        /// <summary>
        /// 回来检测计时器
        /// </summary>
        private DispatcherTimer back_timer;
        /// <summary>
        /// 繁忙检测，用于检测用户在休息提示界面是否超时不操作
        /// </summary>
        private DispatcherTimer busy_timer;
        /// <summary>
        /// 用眼计时，用于定时统计和保存用户的用眼时长
        /// </summary>
        private DispatcherTimer useeye_timer;
        /// <summary>
        /// 日期更改计时，用于处理日期变化
        /// </summary>
        private DispatcherTimer date_timer;
        /// <summary>
        /// 日期更改计时重置标记
        /// </summary>
        private bool isDateTimerReset;
        private bool isUserAway;
        /// <summary>
        /// 预提醒操作
        /// </summary>
        private PreAlertAction preAlertAction;
        #region Service
        private readonly ScreenService screen;
        private readonly ConfigService config;
        private readonly StatisticService statistic;
        private readonly ThemeService theme;
        private readonly SystemResourcesService systemResources;
        #endregion

        #region win32
        //[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        //public static extern IntPtr GetForegroundWindow();
        //[DllImport("user32", SetLastError = true)]
        //public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        #endregion

        #region Event
        public delegate void MainEventHandler(object service, int msg);
        /// <summary>
        /// 用户离开时发生
        /// </summary>
        public event MainEventHandler OnLeaveEvent;
        ///// <summary>
        ///// 用户回来时发生
        ///// </summary>
        //public event MainEventHandler OnComeBackEvent;
        /// <summary>
        /// 计时器重启时发生
        /// </summary>
        public event MainEventHandler OnReStartTimer;
        /// <summary>
        /// 到达休息时间时发生（不管是否进入休息状态）
        /// </summary>
        public event MainEventHandler OnReset;
        /// <summary>
        /// 计时器停止时发生
        /// </summary>
        public event MainEventHandler OnPause;
        /// <summary>
        /// 计时器启动时发生
        /// </summary>
        public event MainEventHandler OnStart;
        /// <summary>
        /// 加载语言完成时发生
        /// </summary>
        public event MainEventHandler OnLoadedLanguage;
        /// <summary>
        /// 提示休息后超时未处理时发生
        /// </summary>
        public event MainEventHandler OnHandleTimeout;
        /// <summary>
        /// 运行状态发生变化时发生
        /// </summary>
        public event MainEventHandler OnRuntimeStatusChanged;
        #endregion
        public MainService(App app,
            ScreenService screen,
            ConfigService config,
            StatisticService statistic,
            ThemeService theme,
            SystemResourcesService systemResources)
        {
            this.screen = screen;
            this.config = config;
            this.statistic = statistic;
            this.theme = theme;
            this.systemResources = systemResources;

            app.Exit += new ExitEventHandler(app_Exit);
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnPowerModeChanged);
        }

        #region 初始化
        public void Init()
        {
            //关闭暂不提醒
            config.options.General.Noreset = false;
            //关闭番茄时钟模式
            config.options.General.IsTomatoMode = false;

            //初始化用眼计时器
            work_timer = new DispatcherTimer();
            work_timer.Tick += new EventHandler(timer_Tick);
            configuredWarnMinutes = config.options.General.WarnTime;
            work_timer.Interval = GetConfiguredWorkDuration();
            workTimerStopwatch = new Stopwatch();
            workRemaining = GetConfiguredWorkDuration();
            //初始化离开检测计时器
            leave_timer = new DispatcherTimer();
            leave_timer.Tick += new EventHandler(leave_timer_Tick);
            leave_timer.Interval = new TimeSpan(0, 0, 1);
            //初始化回来检测计时器
            back_timer = new DispatcherTimer();
            back_timer.Tick += new EventHandler(back_timer_Tick);
            back_timer.Interval = new TimeSpan(0, 0, 1);
            //初始化繁忙计时器
            busy_timer = new DispatcherTimer();
            busy_timer.Tick += new EventHandler(busy_timer_Tick);
            busy_timer.Interval = new TimeSpan(0, 0, 30);
            //初始化延迟休息提醒检测计时器
            deferred_break_timer = new DispatcherTimer();
            deferred_break_timer.Tick += new EventHandler(deferred_break_timer_Tick);
            deferred_break_timer.Interval = new TimeSpan(0, 0, 1);
            //初始化用眼统计计时器
            useeye_timer = new DispatcherTimer();
            useeye_timer.Tick += new EventHandler(useeye_timer_Tick);
            useeye_timer.Interval = new TimeSpan(0, 10, 0);

            date_timer = new DispatcherTimer();
            date_timer.Tick += new EventHandler(date_timer_Tick);
            /****调试模式代码****/
#if DEBUG
            //1秒轮询离开/回来
            leave_timer.Interval = new TimeSpan(0, 0, 1);
            back_timer.Interval = new TimeSpan(0, 0, 1);
            useeye_timer.Interval = new TimeSpan(0, 1, 0);
#endif

            CreateTipWindows();

            UpdateDateTimer();

            Start();

            config.Changed += Config_Changed;


            //加载语言
            HandleLanguageChanged();
        }
        #endregion

        private void HandleLanguageChanged()
        {
            var language = new ResourceDictionary { Source = new Uri($"/20min20s;component/Resources/Language/{config.options.Style.Language.Value}.xaml", UriKind.RelativeOrAbsolute) };

            var mds = System.Windows.Application.Current.Resources.MergedDictionaries;
            var loadedLanguage = mds.Where(m => m.Source.OriginalString.Contains("Language")).FirstOrDefault();
            if (loadedLanguage != null)
            {
                mds.Remove(loadedLanguage);
            }
            mds.Add(language);
            systemResources.Init();
            OnLoadedLanguage?.Invoke(this, 0);
        }

        private void Config_Changed(object sender, EventArgs e)
        {
            var oldOptions = sender as OptionsModel;
            if (oldOptions.Style.IsThruTipWindow != config.options.Style.IsThruTipWindow)
            {
                //鼠标穿透被打开
                CreateTipWindows();
                Debug.WriteLine("鼠标穿透更改，重新创建窗口");
            }
            if (oldOptions.General.IsTomatoMode != config.options.General.IsTomatoMode)
            {
                if (config.options.General.IsTomatoMode)
                {
                    //番茄时钟模式打开
                    DoStop(false);
                    Debug.WriteLine("番茄模式已启动，关闭计时休息提醒模式");
                }
                else
                {
                    DoStart(false);
                    Debug.WriteLine("番茄模式已关闭，恢复计时休息提醒模式");
                }
            }
            if (oldOptions.General.WarnTime != config.options.General.WarnTime)
            {
                configuredWarnMinutes = config.options.General.WarnTime;
            }
            HandleLanguageChanged();
        }
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            LogRuntimeDebug($"PowerModeChanged: {e.Mode}");
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    //电脑休眠
                    Pause();
                    break;
                case PowerModes.Resume:
                    //电脑恢复
                    Start();
                    break;
            }
        }

        private void busy_timer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("用户超过20秒未处理");
            //用户超过20秒未处理
            busy_timer.Stop();
            LogRuntimeDebug("Busy timeout reached while tip window is waiting for user input.");

            OnHandleTimeout?.Invoke(this, 0);
           
        }

        private void back_timer_Tick(object sender, EventArgs e)
        {
            if (!IsUserLeave())
            {
                Debug.WriteLine("用户回来了");
                LogRuntimeDebug("User returned from away state. Resuming work countdown.");
                back_timer.Stop();
                isUserAway = false;
                DoStart(false);
                NotifyRuntimeStatusChanged();
            }
            TryShowDeferredBreakReminder();
        }

        private void leave_timer_Tick(object sender, EventArgs e)
        {
            if (IsUserLeave())
            {
                //用户离开了电脑
                OnLeave();
            }
            else
            {
                TryShowDeferredBreakReminder();
            }
        }

        private void deferred_break_timer_Tick(object sender, EventArgs e)
        {
            TryShowDeferredBreakReminder();
        }

        private void date_timer_Tick(object sender, EventArgs e)
        {

            date_timer.Stop();
            if (!isDateTimerReset)
            {
                //重置统计时间
                statistic.StatisticUseEyeData();
                //延迟2分钟后重置timer
                isDateTimerReset = true;
                date_timer.Interval = new TimeSpan(0, 2, 0);
                date_timer.Start();
            }
            else
            {
                UpdateDateTimer();
            }
        }

        #region 获取下一次休息剩余分钟数
        public double GetRestCountdownMinutes()
        {
            return GetCurrentWorkRemaining().TotalMinutes;
        }
        #endregion

        #region 获取提醒计时是否在运行
        public bool IsWorkTimerRun()
        {
            return (work_timer.IsEnabled || isBreakDeferred) && !config.options.General.Noreset;
        }
        #endregion

        public RuntimeStatus GetRuntimeStatus()
        {
            if (isBreakDeferred)
            {
                return RuntimeStatus.DeferredBreak;
            }

            if (isUserAway)
            {
                return RuntimeStatus.PausedInactivity;
            }

            return RuntimeStatus.Running;
        }

        #region 到达统计时间
        private void useeye_timer_Tick(object sender, EventArgs e)
        {
            StatisticData();
        }
        #endregion

        #region 统计数据
        private void StatisticData()
        {
            if (config.options.General.Data)
            {
                //更新用眼时长
                statistic.StatisticUseEyeData();
                //数据持久化
                statistic.Save();
            }
        }
        #endregion

        #region 结束繁忙超时监听
        /// <summary>
        /// 结束繁忙超时监听
        /// </summary>
        public void StopBusyListener()
        {
            if (busy_timer.IsEnabled)
            {
                busy_timer.Stop();
            }
        }
        #endregion

        #region 进入离开状态
        /// <summary>
        /// 进入离开状态
        /// </summary>
        public void OnLeave()
        {
            if (isUserAway)
            {
                return;
            }

            Debug.WriteLine("用户离开了");
            LogRuntimeDebug($"User marked away. Idle={Win32APIHelper.GetIdleTime().TotalSeconds:F1}s");
            isUserAway = true;
            deferredReminderReadyAt = null;
            WindowManager.Hide("TipWindow");
            //停止所有服务
            DoStop();
            //启动back timer监听鼠标状态
            back_timer.Start();
            //事件响应
            OnLeaveEvent?.Invoke(this, 0);
            NotifyRuntimeStatusChanged();
        }
        #endregion

        #region 停止主进程。退出程序时调用
        /// <summary>
        /// 停止主进程。退出程序时调用
        /// </summary>
        public void Exit()
        {
            if (config.options.General.Data)
            {
                //更新用眼时长
                statistic.StatisticUseEyeData();
                //数据持久化
                statistic.Save();
            }

            screen.Dispose();
            DoStop();
            WindowManager.Close("TipWindow");
        }
        #endregion

        #region 启动主服务
        public void Start()
        {
            DoStart();
        }
        #endregion

        #region 暂停主服务
        /// <summary>
        /// 暂停主服务
        /// </summary>
        /// <param name="isStopStatistic">是否停止用眼统计，默认true</param>
        public void Pause(bool isStopStatistic = true)
        {
            DoStop(isStopStatistic);
            OnPause?.Invoke(this, 0);
        }
        #endregion

        #region 设置提醒间隔时间
        /// <summary>
        /// 设置提醒间隔时间，如果与当前计时器不一致时将重启计时
        /// </summary>
        /// <param name="minutes">间隔时间（分钟）</param>
        /// <returns>重启时返回TRUE</returns>
        public bool SetWarnTime(int minutes)
        {
            if (configuredWarnMinutes != minutes)
            {
                Debug.WriteLine(configuredWarnMinutes + "," + minutes);
                configuredWarnMinutes = minutes;
                ApplyWorkRemaining(GetConfiguredWorkDuration());
                if (!config.options.General.Noreset)
                {
                    ReStart();
                    return true;
                }
                return false;
            }
            return false;
        }
        #endregion

        #region 重新启动计时
        /// <summary>
        /// 重新启动休息计时
        /// </summary>
        public void ReStart()
        {
            if (!config.options.General.Noreset)
            {
                Debug.WriteLine("重新启动休息计时");
                DoStop();
                isBreakDeferred = false;
                deferredReminderReadyAt = null;
                ApplyWorkRemaining(GetConfiguredWorkDuration());
                DoStart();
                OnReStartTimer?.Invoke(this, 0);
                NotifyRuntimeStatusChanged();
            }
        }

        #endregion

        #region 启动计时实际操作
        private void DoStart(bool isHard = true)
        {
            //允许硬启动，否则只有在关闭暂不提醒和番茄时钟模式时才允许启动工作计时
            if ((isHard || !config.options.General.Noreset && !config.options.General.IsTomatoMode) && !isBreakDeferred)
            {
                ResumeWorkCountdown();
            }
            //离开监听
            leave_timer.Start();
            //数据统计
            if (config.options.General.Data && !isBreakDeferred)
            {
                //重置用眼计时
                statistic.ResetStatisticTime();
                //用眼统计
                useeye_timer.Start();
            }
            OnStart?.Invoke(this, 0);
            NotifyRuntimeStatusChanged();
        }
        #endregion

        #region 停止计时实际操作
        private void DoStop(bool isHard = true)
        {
            //统计数据
            StatisticData();
            PauseWorkCountdown();
            if (isHard)
            {
                useeye_timer.Stop();
                leave_timer.Stop();
                back_timer.Stop();
                deferred_break_timer.Stop();
            }
            busy_timer.Stop();
        }
        #endregion

        #region 显示休息提示窗口
        /// <summary>
        /// 显示休息提示窗口
        /// </summary>
        private void ShowTipWindow()
        {
            if (config.options.Style.IsPreAlert && preAlertAction == PreAlertAction.Break)
            {
                //预提醒设置跳过
                LogRuntimeDebug("Skipping reminder because pre-alert action is set to Break.");
                statistic.Add(StatisticType.SkipCount, 1);
                ReStart();
            }
            else
            {
                if (ShouldDeferBreakReminder(out string deferReason))
                {
                    LogRuntimeDebug($"Deferring break reminder at due time. Reason={deferReason}");
                    BeginDeferredBreakReminder();
                }
                else
                {
                    LogRuntimeDebug("Showing tip window immediately. " + DescribeDeferInputs());
                    busy_timer.Start();
                    WindowManager.Show("TipWindow");
                }
            }
        }
        #endregion

        #region 提示窗口显示时 Event
        private void isVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window.IsVisible)
            {
                //显示提示窗口时停止计时
                PauseWorkCountdown();

                window.Focus();
            }
            else
            {
                //隐藏时继续计时
                if (GetCurrentWorkRemaining() > TimeSpan.Zero && !isBreakDeferred)
                {
                    ResumeWorkCountdown();
                }
            }
        }
        #endregion

        #region 用眼到达设定时间 Event
        private void timer_Tick(object sender, EventArgs e)
        {
            PauseWorkCountdown();
            LogRuntimeDebug($"Work timer elapsed. Remaining={GetCurrentWorkRemaining().TotalSeconds:F1}s");
            ShowTipWindow();
            OnReset?.Invoke(this, 0);
        }
        #endregion

        #region 程序退出 Event
        private void app_Exit(object sender, ExitEventArgs e)
        {
            Exit();
        }
        #endregion

        #region 指示用户是否离开了电脑
        /// <summary>
        /// 指示用户是否离开了电脑
        /// </summary>
        /// <returns></returns>
        private bool IsUserLeave()
        {
            return Win32APIHelper.GetIdleTime() >= TimeSpan.FromSeconds(config.options.Behavior.IdlePauseSeconds);
        }

        #endregion

        #region 设置预提醒状态
        public void SetPreAlertAction(PreAlertAction preAlertAction)
        {
            this.preAlertAction = preAlertAction;
        }
        #endregion

        #region 是否跳过本次休息
        /// <summary>
        /// 是否跳过本次休息
        /// </summary>
        /// <returns>true跳过，false不跳过</returns>
        public bool ShouldDeferBreakReminder()
        {
            return ShouldDeferBreakReminder(out _);
        }

        private bool ShouldDeferBreakReminder(out string reason)
        {
            if (!config.options.General.Noreset)
            {
                //深色主题切换判断
                theme.HandleDarkMode();

                bool shouldInspectFocusWindow =
                    config.options.Behavior.IsFullScreenBreak ||
                    config.options.Behavior.IsBreakProgressList;
                Win32APIHelper.WindowInfo focusWindowInfo = default(Win32APIHelper.WindowInfo);
                if (shouldInspectFocusWindow)
                {
                    focusWindowInfo = Win32APIHelper.GetFocusWindowInfo();
                }

                //0.全屏跳过判断
                if (config.options.Behavior.IsFullScreenBreak)
                {
                    if (focusWindowInfo.IsFullScreen)
                    {
                        reason = "Fullscreen foreground window: " + DescribeWindowInfo(focusWindowInfo);
                        return true;
                    }
                }

                //1.进程跳过判断
                if (config.options.Behavior.IsBreakProgressList)
                {
                    if (!string.IsNullOrEmpty(focusWindowInfo.ProcessName))
                    {
                        foreach (var processName in config.options.Behavior.BreakProgressList)
                        {
                            if (string.Equals(processName, focusWindowInfo.ProcessName, StringComparison.OrdinalIgnoreCase))
                            {
                                reason = $"Process '{focusWindowInfo.ProcessName}' matched break list. {DescribeWindowInfo(focusWindowInfo)}";
                                return true;
                            }
                        }
                    }
                }

                reason = "No defer condition matched.";
                return false;
            }

            reason = "Noreset is enabled.";
            return true;
        }
        #endregion

        #region 重置测量时间
        public void ReStartWorkTimerWatch()
        {
            workTimerStopwatch.Restart();
        }
        #endregion

        private void ApplyWorkRemaining(TimeSpan remaining)
        {
            workRemaining = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            work_timer.Interval = workRemaining <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : workRemaining;
        }

        private TimeSpan GetConfiguredWorkDuration()
        {
#if DEBUG
            return TimeSpan.FromSeconds(30);
#else
            return new TimeSpan(0, configuredWarnMinutes, 0);
#endif
        }

        private TimeSpan GetCurrentWorkRemaining()
        {
            if (!work_timer.IsEnabled)
            {
                return workRemaining;
            }

            TimeSpan remaining = workRemaining - workTimerStopwatch.Elapsed;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        private void PauseWorkCountdown()
        {
            ApplyWorkRemaining(GetCurrentWorkRemaining());
            work_timer.Stop();
            workTimerStopwatch.Stop();
        }

        private void ResumeWorkCountdown()
        {
            if (workRemaining <= TimeSpan.Zero)
            {
                return;
            }

            work_timer.Interval = workRemaining;
            workTimerStopwatch.Restart();
            work_timer.Start();
        }

        private void BeginDeferredBreakReminder()
        {
            isBreakDeferred = true;
            deferredReminderReadyAt = null;
            LogRuntimeDebug("Entering deferred break state.");
            if (config.options.General.Data)
            {
                StatisticData();
                useeye_timer.Stop();
            }
            ApplyWorkRemaining(TimeSpan.Zero);
            PauseWorkCountdown();
            deferred_break_timer.Start();
            NotifyRuntimeStatusChanged();
        }

        private void TryShowDeferredBreakReminder()
        {
            if (!isBreakDeferred)
            {
                deferred_break_timer.Stop();
                return;
            }

            if (IsUserLeave())
            {
                if (deferredReminderReadyAt.HasValue)
                {
                    LogRuntimeDebug("Deferred reminder countdown cleared because user is away again.");
                }
                deferredReminderReadyAt = null;
                return;
            }

            if (ShouldDeferBreakReminder(out string deferReason))
            {
                if (deferredReminderReadyAt.HasValue)
                {
                    LogRuntimeDebug($"Deferred reminder countdown cleared because defer condition still applies. Reason={deferReason}");
                }
                deferredReminderReadyAt = null;
                return;
            }

            if (!deferredReminderReadyAt.HasValue)
            {
                deferredReminderReadyAt = DateTime.Now.Add(TimeSpan.FromSeconds(config.options.Behavior.DeferredBreakReminderDelaySeconds));
                LogRuntimeDebug($"Deferred reminder countdown started. ReadyAt={deferredReminderReadyAt.Value:HH:mm:ss}");
                return;
            }

            if (DateTime.Now < deferredReminderReadyAt.Value)
            {
                return;
            }

            isBreakDeferred = false;
            deferredReminderReadyAt = null;
            deferred_break_timer.Stop();
            LogRuntimeDebug("Deferred reminder countdown finished. Showing tip window now.");
            busy_timer.Start();
            WindowManager.Show("TipWindow");
            NotifyRuntimeStatusChanged();
        }

        private static string DescribeWindowInfo(Win32APIHelper.WindowInfo info)
        {
            return $"Process={info.ProcessName}, Title={info.Title}, Class={info.ClassName}, FullScreen={info.IsFullScreen}, Zoomed={info.IsZoomed}, Size={info.Width}x{info.Height}";
        }

        private string DescribeDeferInputs()
        {
            string configSummary =
                $"Noreset={config.options.General.Noreset}, " +
                $"IsFullScreenBreak={config.options.Behavior.IsFullScreenBreak}, " +
                $"IsBreakProgressList={config.options.Behavior.IsBreakProgressList}, " +
                $"IdlePauseSeconds={config.options.Behavior.IdlePauseSeconds}, " +
                $"DeferredBreakReminderDelaySeconds={config.options.Behavior.DeferredBreakReminderDelaySeconds}";

            if (!config.options.Behavior.IsFullScreenBreak && !config.options.Behavior.IsBreakProgressList)
            {
                return configSummary;
            }

            return configSummary + ", " + DescribeWindowInfo(Win32APIHelper.GetFocusWindowInfo());
        }

        private static void LogRuntimeDebug(string message)
        {
            LogHelper.Debug("[MainService] " + message, true, true);
        }

        private void NotifyRuntimeStatusChanged()
        {
            OnRuntimeStatusChanged?.Invoke(this, 0);
        }

        #region 创建全屏提示窗口
        /// <summary>
        /// 创建全屏提示窗口
        /// </summary>
        public void CreateTipWindows()
        {
            //关闭
            WindowManager.Close("TipWindow");
            //在所有屏幕上创建全屏提示窗口
            var tipWindow = WindowManager.GetCreateWindow("TipWindow", true);

            foreach (var window in tipWindow)
            {
                window.IsVisibleChanged += new DependencyPropertyChangedEventHandler(isVisibleChanged);
            }
        }
        #endregion

        #region 更新日期更改计时时间
        /// <summary>
        /// 更新日期更改计时时间
        /// </summary>
        private void UpdateDateTimer()
        {

            DateTime now = DateTime.Now;
            DateTime morrow = new DateTime(now.Year, now.Month, now.Day, 23, 59, 0);
            int diffseconds = (int)morrow.Subtract(now).TotalSeconds;
            if (diffseconds < 0)
            {
                diffseconds = 0;
            }
            date_timer.Interval = new TimeSpan(0, 0, diffseconds);
            date_timer.Start();
            isDateTimerReset = false;
        }
        #endregion
    }
}
