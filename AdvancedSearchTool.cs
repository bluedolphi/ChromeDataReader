using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChromeDataReader.Models;

namespace ChromeDataReader
{
    public class AdvancedSearchTool
    {
        public void SearchForTargetData(string userDataPath)
        {
            Console.WriteLine("\n🔍 高级搜索工具 - 查找aiyzgr相关数据");
            Console.WriteLine(new string('=', 60));

            // 1. 搜索所有可能的存储位置
            SearchInLocalStorageFiles(userDataPath);
            SearchInSessionStorageFiles(userDataPath);
            SearchInCookieFiles(userDataPath);
            SearchInIndexedDBFiles(userDataPath);
            SearchInWebDataFiles(userDataPath);
        }

        private void SearchInLocalStorageFiles(string userDataPath)
        {
            Console.WriteLine("\n📁 搜索LocalStorage文件...");
            
            var localStorageDir = Path.Combine(userDataPath, "Default", "Local Storage");
            if (!Directory.Exists(localStorageDir))
            {
                Console.WriteLine("LocalStorage目录不存在");
                return;
            }

            try
            {
                // 搜索所有可能的文件
                var allFiles = Directory.GetFiles(localStorageDir, "*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        
                        // 检查文件名是否包含目标关键词
                        if (ContainsTargetKeywords(fileName))
                        {
                            Console.WriteLine($"🎯 找到相关文件: {file}");
                            AnalyzeFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法访问的文件
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索LocalStorage错误: {ex.Message}");
            }
        }

        private void SearchInSessionStorageFiles(string userDataPath)
        {
            Console.WriteLine("\n📁 搜索SessionStorage文件...");
            
            var sessionStorageDir = Path.Combine(userDataPath, "Default", "Session Storage");
            if (!Directory.Exists(sessionStorageDir))
            {
                Console.WriteLine("SessionStorage目录不存在");
                return;
            }

            try
            {
                var allFiles = Directory.GetFiles(sessionStorageDir, "*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        
                        if (ContainsTargetKeywords(fileName))
                        {
                            Console.WriteLine($"🎯 找到相关文件: {file}");
                            AnalyzeFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法访问的文件
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索SessionStorage错误: {ex.Message}");
            }
        }

        private void SearchInCookieFiles(string userDataPath)
        {
            Console.WriteLine("\n📁 搜索Cookie文件...");
            
            var cookieFile = Path.Combine(userDataPath, "Default", "Network", "Cookies");
            if (File.Exists(cookieFile))
            {
                Console.WriteLine($"Cookie文件存在: {cookieFile}");
                Console.WriteLine("注意: Cookie文件被Chrome锁定，需要关闭Chrome后才能读取");
            }
        }

        private void SearchInIndexedDBFiles(string userDataPath)
        {
            Console.WriteLine("\n📁 搜索IndexedDB文件...");
            
            var indexedDBDir = Path.Combine(userDataPath, "Default", "IndexedDB");
            if (!Directory.Exists(indexedDBDir))
            {
                Console.WriteLine("IndexedDB目录不存在");
                return;
            }

            try
            {
                var allDirs = Directory.GetDirectories(indexedDBDir, "*", SearchOption.AllDirectories);
                
                foreach (var dir in allDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    
                    if (ContainsTargetKeywords(dirName))
                    {
                        Console.WriteLine($"🎯 找到相关IndexedDB: {dir}");
                        
                        // 列出目录中的文件
                        try
                        {
                            var files = Directory.GetFiles(dir);
                            foreach (var file in files.Take(5))
                            {
                                Console.WriteLine($"  文件: {Path.GetFileName(file)}");
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索IndexedDB错误: {ex.Message}");
            }
        }

        private void SearchInWebDataFiles(string userDataPath)
        {
            Console.WriteLine("\n📁 搜索Web Data文件...");
            
            var webDataFile = Path.Combine(userDataPath, "Default", "Web Data");
            if (File.Exists(webDataFile))
            {
                Console.WriteLine($"Web Data文件存在: {webDataFile}");
                Console.WriteLine("这个文件可能包含表单数据和其他Web存储信息");
            }
        }

        private bool ContainsTargetKeywords(string text)
        {
            var keywords = new[] { "aiyzgr", "aiyouzhan", "station", "adm", "web" };
            
            return keywords.Any(keyword => 
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private void AnalyzeFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                Console.WriteLine($"  文件大小: {fileInfo.Length} 字节");
                Console.WriteLine($"  创建时间: {fileInfo.CreationTime}");
                Console.WriteLine($"  修改时间: {fileInfo.LastWriteTime}");

                // 如果文件不太大，尝试读取内容
                if (fileInfo.Length < 1024 * 1024 && fileInfo.Length > 0) // 小于1MB且不为空
                {
                    try
                    {
                        var content = File.ReadAllText(filePath);
                        
                        // 搜索目标关键词
                        var targetKeywords = new[] { "aiyzgr-station-adm-web", "aiyzgr", "aiyouzhan", "station" };
                        
                        foreach (var keyword in targetKeywords)
                        {
                            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"  ✅ 找到关键词: {keyword}");
                                
                                // 提取关键词周围的内容
                                var index = content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                                if (index >= 0)
                                {
                                    var start = Math.Max(0, index - 50);
                                    var length = Math.Min(100, content.Length - start);
                                    var context = content.Substring(start, length);
                                    Console.WriteLine($"  上下文: ...{context}...");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  无法读取文件内容: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  分析文件错误: {ex.Message}");
            }
        }

        public void ShowSearchTips()
        {
            Console.WriteLine("\n💡 搜索建议:");
            Console.WriteLine("1. 关闭所有Chrome窗口后重新运行程序");
            Console.WriteLine("2. 确保已经访问过目标网站 https://station.aiyzgr.aiyouzhan.cn/");
            Console.WriteLine("3. 在网站上执行一些操作，确保数据被存储到LocalStorage");
            Console.WriteLine("4. 检查浏览器开发者工具中的Application -> Local Storage");
            Console.WriteLine("5. 可能数据存储在其他键名下，如 'token', 'user', 'auth' 等");
        }
    }
}
