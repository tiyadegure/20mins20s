using ProjectEye.Core;
using ProjectEye.Core.Service;
using ProjectEye.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ProjectEye.ViewModels
{
    public class ContributorsViewModel
    {

        public Command openurlCommand { get; set; }

        public ContributorsViewModel()
        {

            openurlCommand = new Command(new Action<object>(openurlCommand_action));
        }

     
        private void openurlCommand_action(object obj)
        {
            string url = obj?.ToString();
            if (string.IsNullOrWhiteSpace(url) || string.Equals(url, "NULL", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

    }
}
