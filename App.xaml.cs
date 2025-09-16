using System;
using System.Windows;

namespace ChromeDataReader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 设置全局异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 检测.NET 6.0运行时
            CheckDotNetRuntime();
        }

        /// <summary>
        /// 检测.NET 6.0运行时是否已安装
        /// </summary>
        private void CheckDotNetRuntime()
        {
            try
            {
                // 检测.NET 6.0运行时
                if (!SystemInfoDetector.IsDotNet6RuntimeInstalled())
                {
                    // 显示安装引导窗口
                    var guideWindow = new DotNetInstallGuideWindow();
                    guideWindow.ShowDialog();

                    // 如果用户选择不继续，则退出程序
                    if (!guideWindow.ShouldContinue)
                    {
                        this.Shutdown();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果检测过程出现异常，显示警告但允许程序继续运行
                MessageBox.Show(
                    $"检测.NET运行时时发生错误: {ex.Message}\n\n程序将尝试继续运行，但可能会出现问题。",
                    "运行时检测警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"应用程序错误: {e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
        
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show($"严重错误: {exception?.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
