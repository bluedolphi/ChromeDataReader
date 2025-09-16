using System;
using System.Diagnostics;
using System.Windows;

namespace ChromeDataReader
{
    /// <summary>
    /// .NET安装引导窗口
    /// </summary>
    public partial class DotNetInstallGuideWindow : Window
    {
        private string _recommendedDownloadUrl;
        private string _officialDownloadUrl;
        
        public bool ShouldContinue { get; private set; } = false;
        
        public DotNetInstallGuideWindow()
        {
            InitializeComponent();
            LoadSystemInfo();
            SetupDownloadUrls();
        }
        
        /// <summary>
        /// 加载系统信息
        /// </summary>
        private void LoadSystemInfo()
        {
            try
            {
                txtOSInfo.Text = $"操作系统: {SystemInfoDetector.GetOperatingSystemInfo()}";
                txtArchitecture.Text = $"系统架构: {SystemInfoDetector.GetSystemArchitecture()}";
                txtCurrentDotNet.Text = $"当前.NET: {SystemInfoDetector.GetCurrentDotNetInfo()}";
            }
            catch (Exception ex)
            {
                txtOSInfo.Text = $"操作系统: 获取失败 - {ex.Message}";
                txtArchitecture.Text = "系统架构: 获取失败";
                txtCurrentDotNet.Text = "当前.NET: 获取失败";
            }
        }
        
        /// <summary>
        /// 设置下载链接
        /// </summary>
        private void SetupDownloadUrls()
        {
            try
            {
                _recommendedDownloadUrl = SystemInfoDetector.GetRecommendedDotNet6RuntimeUrl();
                _officialDownloadUrl = SystemInfoDetector.GetDotNet6DownloadUrl();
                
                txtDownloadUrl.Text = _recommendedDownloadUrl;
            }
            catch (Exception ex)
            {
                _recommendedDownloadUrl = "https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0";
                _officialDownloadUrl = "https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0";
                txtDownloadUrl.Text = _recommendedDownloadUrl;
            }
        }
        
        /// <summary>
        /// 下载推荐的运行时
        /// </summary>
        private void BtnDownloadRecommended_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenUrl(_recommendedDownloadUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开下载链接: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 访问官方下载页面
        /// </summary>
        private void BtnDownloadOfficial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenUrl(_officialDownloadUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开官方页面: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 重新检测.NET运行时
        /// </summary>
        private void BtnRecheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SystemInfoDetector.IsDotNet6RuntimeInstalled())
                {
                    MessageBox.Show("检测到 .NET 6.0 运行时已安装！程序将继续运行。", "检测成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ShouldContinue = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("仍未检测到 .NET 6.0 运行时。请确保已正确安装并重启计算机。", "检测失败", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // 刷新系统信息
                    LoadSystemInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检测过程中发生错误: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 忽略并继续运行
        /// </summary>
        private void BtnIgnore_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "警告：在没有正确安装 .NET 6.0 运行时的情况下继续运行可能会导致程序出现错误或崩溃。\n\n确定要继续吗？", 
                "警告", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                ShouldContinue = true;
                this.Close();
            }
        }
        
        /// <summary>
        /// 退出程序
        /// </summary>
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            ShouldContinue = false;
            this.Close();
        }
        
        /// <summary>
        /// 打开URL
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // 如果直接打开失败，尝试使用默认浏览器
                try
                {
                    Process.Start("cmd", $"/c start {url}");
                }
                catch
                {
                    throw new Exception($"无法打开链接: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 如果用户没有选择继续，则退出应用程序
            if (!ShouldContinue)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
