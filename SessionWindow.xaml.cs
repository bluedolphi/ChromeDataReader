using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ChromeDataReader.Models;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public partial class SessionWindow : Window
    {
        private List<SessionInfo> _sessions;
        
        public SessionWindow(List<SessionInfo> sessions)
        {
            InitializeComponent();
            _sessions = sessions ?? new List<SessionInfo>();
            LoadSessions();
        }
        
        private void LoadSessions()
        {
            LstSessions.ItemsSource = _sessions;
            TxtCount.Text = $"共 {_sessions.Count} 个Session";
            TxtStatus.Text = "Session数据加载完成";
        }
        
        private void LstSessions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstSessions.SelectedItem is SessionInfo session)
            {
                DisplaySessionDetails(session);
            }
        }
        
        private void DisplaySessionDetails(SessionInfo session)
        {
            try
            {
                TxtSessionId.Text = session.SessionId;
                TxtDomain.Text = session.Domain;
                TxtCreatedTime.Text = session.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");
                TxtLastAccessTime.Text = session.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 显示Session数据
                var dataText = "";
                foreach (var kvp in session.Data)
                {
                    dataText += $"{kvp.Key}: {kvp.Value}\n";
                }
                TxtSessionData.Text = dataText;
                
                // 尝试格式化JSON
                TryFormatJson(session);
                
                TxtStatus.Text = $"显示Session: {session.SessionId}";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"显示Session详情错误: {ex.Message}";
            }
        }
        
        private void TryFormatJson(SessionInfo session)
        {
            try
            {
                var jsonContent = "";
                
                // 查找可能的JSON内容
                foreach (var kvp in session.Data)
                {
                    var value = kvp.Value?.ToString() ?? "";
                    if (IsJsonString(value))
                    {
                        try
                        {
                            var jsonObject = JsonConvert.DeserializeObject(value);
                            var formattedJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                            jsonContent += $"=== {kvp.Key} ===\n{formattedJson}\n\n";
                        }
                        catch
                        {
                            // 不是有效的JSON
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    jsonContent = "未找到有效的JSON数据";
                }
                
                TxtFormattedJson.Text = jsonContent;
            }
            catch (Exception ex)
            {
                TxtFormattedJson.Text = $"JSON格式化错误: {ex.Message}";
            }
        }
        
        private bool IsJsonString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            
            str = str.Trim();
            return (str.StartsWith("{") && str.EndsWith("}")) || 
                   (str.StartsWith("[") && str.EndsWith("]"));
        }
        
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON文件|*.json|文本文件|*.txt|所有文件|*.*",
                    FileName = $"Session数据_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    var json = JsonConvert.SerializeObject(_sessions, Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    TxtStatus.Text = $"Session数据已导出到: {saveDialog.FileName}";
                    MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"导出失败: {ex.Message}";
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSessions();
        }
    }
}
