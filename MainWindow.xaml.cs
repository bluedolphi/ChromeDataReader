using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChromeDataReader.Models;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public partial class MainWindow : Window
    {
        private ChromePathDetector _pathDetector;
        private CookieReader _cookieReader;
        private SessionReader _sessionReader;
        private StorageReader _storageReader;
        private SpecificDataExtractor _specificExtractor;
        private CustomDataManager _customDataManager;
        
        private List<ChromeInfo> _chromeInstallations;
        private List<CookieInfo> _allCookies;
        private List<StorageInfo> _allStorage;
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeComponents();
            LogMessage("Chrome数据读取工具已启动");
        }
        
        private void InitializeComponents()
        {
            _pathDetector = new ChromePathDetector();
            _cookieReader = new CookieReader();
            _sessionReader = new SessionReader();
            _storageReader = new StorageReader();
            _specificExtractor = new SpecificDataExtractor();
            _customDataManager = new CustomDataManager();
            
            _chromeInstallations = new List<ChromeInfo>();
            _allCookies = new List<CookieInfo>();
            _allStorage = new List<StorageInfo>();
            
            // 自动检测Chrome
            DetectChromeInstallations();
        }
        
        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            TxtLog.AppendText($"[{timestamp}] {message}\n");
            TxtLog.ScrollToEnd();
        }
        
        private void UpdateStatus(string status)
        {
            TxtStatus.Text = status;
        }
        
        private void UpdateProgress(string progress)
        {
            TxtProgress.Text = progress;
        }
        
        private void DetectChromeInstallations()
        {
            try
            {
                UpdateStatus("正在检测Chrome安装...");
                _chromeInstallations = _pathDetector.DetectChromeInstallations();
                
                CmbChromeInstalls.Items.Clear();
                foreach (var chrome in _chromeInstallations)
                {
                    CmbChromeInstalls.Items.Add($"{chrome.Version} - {chrome.ExecutablePath}");
                }
                
                if (_chromeInstallations.Count > 0)
                {
                    CmbChromeInstalls.SelectedIndex = 0;
                    DgChromeInfo.ItemsSource = _chromeInstallations;
                }
                
                LogMessage($"检测到 {_chromeInstallations.Count} 个Chrome安装");
                UpdateStatus($"找到 {_chromeInstallations.Count} 个Chrome安装");
            }
            catch (Exception ex)
            {
                LogMessage($"检测Chrome安装错误: {ex.Message}");
                UpdateStatus("Chrome检测失败");
            }
        }
        
        private ChromeInfo? GetSelectedChrome()
        {
            if (CmbChromeInstalls.SelectedIndex >= 0 && CmbChromeInstalls.SelectedIndex < _chromeInstallations.Count)
            {
                return _chromeInstallations[CmbChromeInstalls.SelectedIndex];
            }
            return null;
        }
        
        private void BtnDetectChrome_Click(object sender, RoutedEventArgs e)
        {
            DetectChromeInstallations();
        }
        
        private void BtnRefreshChrome_Click(object sender, RoutedEventArgs e)
        {
            DetectChromeInstallations();
        }
        
        private async void BtnReadCookies_Click(object sender, RoutedEventArgs e)
        {
            var chrome = GetSelectedChrome();
            if (chrome == null)
            {
                MessageBox.Show("请先选择Chrome安装", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("正在读取Cookie数据...");
                LogMessage("开始读取Cookie数据");
                
                await Task.Run(() =>
                {
                    _allCookies = _cookieReader.ReadCookies(chrome.UserDataPath);
                });
                
                DgCookies.ItemsSource = _allCookies;
                LogMessage($"读取到 {_allCookies.Count} 个Cookie");
                UpdateStatus($"Cookie读取完成，共 {_allCookies.Count} 个");
            }
            catch (Exception ex)
            {
                LogMessage($"读取Cookie错误: {ex.Message}");
                UpdateStatus("Cookie读取失败");
                MessageBox.Show($"读取Cookie失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnReadSessions_Click(object sender, RoutedEventArgs e)
        {
            var chrome = GetSelectedChrome();
            if (chrome == null)
            {
                MessageBox.Show("请先选择Chrome安装", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("正在读取Session数据...");
                LogMessage("开始读取Session数据");
                
                List<SessionInfo> sessions = null;
                await Task.Run(() =>
                {
                    sessions = _sessionReader.ReadSessions(chrome.UserDataPath);
                });
                
                // 创建一个新的窗口显示Session数据
                var sessionWindow = new SessionWindow(sessions);
                sessionWindow.Show();
                
                LogMessage($"读取到 {sessions.Count} 个Session");
                UpdateStatus($"Session读取完成，共 {sessions.Count} 个");
            }
            catch (Exception ex)
            {
                LogMessage($"读取Session错误: {ex.Message}");
                UpdateStatus("Session读取失败");
                MessageBox.Show($"读取Session失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnReadStorage_Click(object sender, RoutedEventArgs e)
        {
            var chrome = GetSelectedChrome();
            if (chrome == null)
            {
                MessageBox.Show("请先选择Chrome安装", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("正在读取Storage数据...");
                LogMessage("开始读取Storage数据");
                
                await Task.Run(() =>
                {
                    _allStorage = _storageReader.ReadLocalStorage(chrome.UserDataPath);
                });
                
                DgStorage.ItemsSource = _allStorage;
                LogMessage($"读取到 {_allStorage.Count} 个Storage项目");
                UpdateStatus($"Storage读取完成，共 {_allStorage.Count} 个");
            }
            catch (Exception ex)
            {
                LogMessage($"读取Storage错误: {ex.Message}");
                UpdateStatus("Storage读取失败");
                MessageBox.Show($"读取Storage失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnSearchTarget_Click(object sender, RoutedEventArgs e)
        {
            var chrome = GetSelectedChrome();
            if (chrome == null)
            {
                MessageBox.Show("请先选择Chrome安装", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                UpdateStatus("正在搜索目标数据...");
                LogMessage("开始搜索aiyzgr-station-adm-web数据");
                
                var targetData = await Task.Run(() =>
                {
                    return _storageReader.FindSpecificStorage(chrome.UserDataPath, "aiyzgr.aiyouzhan.cn", "aiyzgr-station-adm-web");
                });
                
                if (targetData != null)
                {
                    var result = $"找到目标数据!\n来源: {targetData.Origin}\n键: {targetData.Key}\n值: {targetData.Value}";
                    TxtReadResult.Text = result;
                    LogMessage("成功找到目标数据");
                    UpdateStatus("目标数据搜索成功");
                    MessageBox.Show("找到目标数据！请查看自定义操作标签页的读取结果。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogMessage("未找到目标数据");
                    UpdateStatus("未找到目标数据");
                    MessageBox.Show("未找到目标数据。请确保已访问过目标网站并且Chrome已关闭。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"搜索目标数据错误: {ex.Message}");
                UpdateStatus("目标数据搜索失败");
                MessageBox.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnFilterCookies_Click(object sender, RoutedEventArgs e)
        {
            if (_allCookies == null || _allCookies.Count == 0)
            {
                MessageBox.Show("请先读取Cookie数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var filter = TxtCookieFilter.Text.Trim();
            if (string.IsNullOrEmpty(filter))
            {
                DgCookies.ItemsSource = _allCookies;
            }
            else
            {
                var filtered = _allCookies.Where(c => c.Domain.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                DgCookies.ItemsSource = filtered;
                LogMessage($"Cookie过滤结果: {filtered.Count} 个");
            }
        }
        
        private void BtnSearchStorage_Click(object sender, RoutedEventArgs e)
        {
            if (_allStorage == null || _allStorage.Count == 0)
            {
                MessageBox.Show("请先读取Storage数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var search = TxtStorageSearch.Text.Trim();
            if (string.IsNullOrEmpty(search))
            {
                DgStorage.ItemsSource = _allStorage;
            }
            else
            {
                var filtered = _allStorage.Where(s => 
                    s.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Origin.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Value.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                DgStorage.ItemsSource = filtered;
                LogMessage($"Storage搜索结果: {filtered.Count} 个");
            }
        }
        
        private void BtnCustomRead_Click(object sender, RoutedEventArgs e)
        {
            var chrome = GetSelectedChrome();
            if (chrome == null)
            {
                MessageBox.Show("请先选择Chrome安装", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var url = TxtReadUrl.Text.Trim();
            var key = TxtReadKey.Text.Trim();
            
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                MessageBox.Show("请输入网站URL和键名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                var result = _customDataManager.ReadCustomData(chrome.UserDataPath, url, key);
                TxtReadResult.Text = result ?? "未找到数据";
                LogMessage($"自定义读取: {url} - {key}");
            }
            catch (Exception ex)
            {
                TxtReadResult.Text = $"读取失败: {ex.Message}";
                LogMessage($"自定义读取错误: {ex.Message}");
            }
        }
        
        private void BtnCustomWrite_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("写入功能需要Chrome扩展或特殊权限支持。\n当前版本主要支持数据读取。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtWriteResult.Text = "写入功能暂未实现，需要Chrome扩展支持";
        }
        
        private void BtnExportCookies_Click(object sender, RoutedEventArgs e)
        {
            ExportData(_allCookies, "Cookie数据");
        }
        
        private void BtnExportStorage_Click(object sender, RoutedEventArgs e)
        {
            ExportData(_allStorage, "Storage数据");
        }
        
        private void ExportData<T>(List<T> data, string dataType)
        {
            if (data == null || data.Count == 0)
            {
                MessageBox.Show($"没有{dataType}可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*",
                FileName = $"{dataType}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);
                    LogMessage($"导出{dataType}到: {saveDialog.FileName}");
                    MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogMessage($"导出{dataType}错误: {ex.Message}");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            DgChromeInfo.ItemsSource = null;
            DgCookies.ItemsSource = null;
            DgStorage.ItemsSource = null;
            TxtReadResult.Clear();
            TxtWriteResult.Clear();
            
            _allCookies?.Clear();
            _allStorage?.Clear();
            
            LogMessage("界面数据已清空");
            UpdateStatus("数据已清空");
        }
        
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }
        
        private void BtnSaveLog_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = $"操作日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, TxtLog.Text);
                    MessageBox.Show("日志保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
