using System;
using System.IO;
using System.Windows;

namespace PortKiller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogError("UnhandledException", args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogError("DispatcherUnhandledException", args.Exception);
                MessageBox.Show($"Lỗi ứng dụng: {args.Exception.Message}\n\nChi tiết:\n{args.Exception}", "Lỗi PortKiller", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }

        private static void LogError(string source, Exception? ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n----------------------------------------\n";
                File.AppendAllText(logPath, log);
            }
            catch { }
        }
    }
}
