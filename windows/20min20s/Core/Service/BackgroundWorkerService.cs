using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ProjectEye.Core.Service
{
    /// <summary>
    /// 管理整个程序的后台工作任务
    /// </summary>
    public class BackgroundWorkerService : IService
    {
        /// <summary>
        /// 获取当前是否正在执行后台任务
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return backgroundWorker != null ? backgroundWorker.IsBusy : false;
            }
        }
        public delegate void EventHandler();
        public event EventHandler OnCompleted, DoWork;

        private BackgroundWorker backgroundWorker;
        private List<Action> actions;
        private readonly object actionsLock = new object();

        public BackgroundWorkerService()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            actions = new List<Action>();
        }
        public void Init()
        {
            DoWork?.Invoke();
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnCompleted?.Invoke();
            Run();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Action> pendingActions;
            lock (actionsLock)
            {
                pendingActions = new List<Action>(actions);
                actions.Clear();
            }

            foreach (var action in pendingActions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    ProjectEye.Core.LogHelper.Error(ex.ToString());
                }
            }
        }

        public void AddAction(Action action)
        {
            lock (actionsLock)
            {
                actions.Add(action);
            }
        }

        public void Run()
        {
            bool hasActions;
            lock (actionsLock)
            {
                hasActions = actions.Count > 0;
            }

            if (hasActions && !backgroundWorker.IsBusy)
            {
                DoWork?.Invoke();
                backgroundWorker.RunWorkerAsync();
            }
        }
    }
}
