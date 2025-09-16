using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ChromeDataReader.Models;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public class SpecificDataExtractor
    {
        public void ExtractAiyzgrData(string userDataPath)
        {
            Console.WriteLine("\n🎯 专门提取aiyzgr-station-adm-web数据");
            Console.WriteLine(new string('=', 60));

            // 尝试多种方法提取数据
            TryExtractFromLocalStorageDB(userDataPath);
            TryExtractFromLevelDB(userDataPath);
            ShowManualExtractionGuide();
        }

        private void TryExtractFromLocalStorageDB(string userDataPath)
        {
            Console.WriteLine("\n📁 方法1: 从LocalStorage数据库文件提取");
            
            var localStorageDir = Path.Combine(userDataPath, "Default", "Local Storage");
            
            if (!Directory.Exists(localStorageDir))
            {
                Console.WriteLine("LocalStorage目录不存在");
                return;
            }

            try
            {
                // 查找所有可能的LocalStorage文件
                var files = Directory.GetFiles(localStorageDir, "*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".localstorage") || f.EndsWith(".db") || f.EndsWith(".sqlite"))
                    .ToArray();

                Console.WriteLine($"找到 {files.Length} 个可能的数据库文件");

                foreach (var file in files)
                {
                    try
                    {
                        Console.WriteLine($"\n检查文件: {Path.GetFileName(file)}");
                        
                        // 检查文件名是否包含目标域名
                        var fileName = Path.GetFileName(file);
                        if (fileName.Contains("aiyzgr") || fileName.Contains("aiyouzhan") || 
                            fileName.Contains("station") || DecodeFileName(fileName).Contains("aiyzgr"))
                        {
                            Console.WriteLine("🎯 文件名匹配目标域名!");
                            ExtractFromSQLiteFile(file);
                        }
                        else
                        {
                            // 即使文件名不匹配，也尝试读取内容
                            ExtractFromSQLiteFile(file, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理文件错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描LocalStorage目录错误: {ex.Message}");
            }
        }

        private void ExtractFromSQLiteFile(string filePath, bool searchOnly = false)
        {
            try
            {
                // 创建临时副本
                var tempFile = Path.GetTempFileName();
                File.Copy(filePath, tempFile, true);

                using var connection = new SQLiteConnection($"Data Source={tempFile};Version=3;");
                connection.Open();

                // 获取所有表名
                var tables = GetTableNames(connection);
                Console.WriteLine($"  数据库包含表: {string.Join(", ", tables)}");

                foreach (var table in tables)
                {
                    try
                    {
                        var query = $"SELECT * FROM {table}";
                        using var command = new SQLiteCommand(query, connection);
                        using var reader = command.ExecuteReader();

                        var columnNames = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames.Add(reader.GetName(i));
                        }

                        Console.WriteLine($"  表 {table} 的列: {string.Join(", ", columnNames)}");

                        while (reader.Read())
                        {
                            var rowData = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowData[reader.GetName(i)] = reader[i] ?? "";
                            }

                            // 检查是否包含目标数据
                            var foundTarget = false;
                            foreach (var kvp in rowData)
                            {
                                var value = kvp.Value.ToString() ?? "";
                                if (value.Contains("aiyzgr-station-adm-web", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("\n🎉 找到目标数据!");
                                    Console.WriteLine($"表: {table}");
                                    Console.WriteLine($"列: {kvp.Key}");
                                    Console.WriteLine($"值: {value}");
                                    foundTarget = true;
                                    
                                    // 尝试解析JSON
                                    TryParseAndDisplayJSON(value);
                                }
                                else if (!searchOnly && (value.Contains("aiyzgr") || value.Contains("aiyouzhan")))
                                {
                                    Console.WriteLine($"\n🔍 找到相关数据:");
                                    Console.WriteLine($"列: {kvp.Key}");
                                    Console.WriteLine($"值: {(value.Length > 200 ? value.Substring(0, 200) + "..." : value)}");
                                }
                            }

                            if (foundTarget) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  读取表 {table} 错误: {ex.Message}");
                    }
                }

                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取SQLite文件错误: {ex.Message}");
            }
        }

        private void TryExtractFromLevelDB(string userDataPath)
        {
            Console.WriteLine("\n📁 方法2: 从LevelDB文件提取");
            
            var levelDbPath = Path.Combine(userDataPath, "Default", "Local Storage", "leveldb");
            
            if (!Directory.Exists(levelDbPath))
            {
                Console.WriteLine("LevelDB目录不存在");
                return;
            }

            try
            {
                var files = Directory.GetFiles(levelDbPath, "*")
                    .Where(f => f.EndsWith(".ldb") || f.EndsWith(".log"))
                    .ToArray();

                Console.WriteLine($"找到 {files.Length} 个LevelDB文件");

                foreach (var file in files)
                {
                    try
                    {
                        Console.WriteLine($"\n检查文件: {Path.GetFileName(file)}");
                        
                        var bytes = File.ReadAllBytes(file);
                        var content = System.Text.Encoding.UTF8.GetString(bytes);

                        if (content.Contains("aiyzgr-station-adm-web"))
                        {
                            Console.WriteLine("🎉 在LevelDB文件中找到目标数据!");
                            
                            // 尝试提取JSON数据
                            var startIndex = content.IndexOf("aiyzgr-station-adm-web");
                            if (startIndex >= 0)
                            {
                                // 查找JSON数据的开始
                                var jsonStart = content.IndexOf("{", startIndex);
                                if (jsonStart >= 0)
                                {
                                    // 尝试找到完整的JSON
                                    var jsonData = ExtractJSONFromString(content.Substring(jsonStart));
                                    if (!string.IsNullOrEmpty(jsonData))
                                    {
                                        Console.WriteLine($"提取的JSON数据: {jsonData}");
                                        TryParseAndDisplayJSON(jsonData);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理LevelDB文件错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描LevelDB目录错误: {ex.Message}");
            }
        }

        private string ExtractJSONFromString(string content)
        {
            try
            {
                var braceCount = 0;
                var startIndex = 0;
                
                for (int i = 0; i < content.Length; i++)
                {
                    if (content[i] == '{')
                    {
                        if (braceCount == 0) startIndex = i;
                        braceCount++;
                    }
                    else if (content[i] == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            return content.Substring(startIndex, i - startIndex + 1);
                        }
                    }
                }
            }
            catch { }
            
            return "";
        }

        private void TryParseAndDisplayJSON(string jsonString)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject(jsonString);
                var formattedJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                
                Console.WriteLine("\n📋 格式化的JSON数据:");
                Console.WriteLine(formattedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON解析错误: {ex.Message}");
                Console.WriteLine("原始数据:");
                Console.WriteLine(jsonString.Length > 500 ? jsonString.Substring(0, 500) + "..." : jsonString);
            }
        }

        private List<string> GetTableNames(SQLiteConnection connection)
        {
            var tables = new List<string>();
            
            try
            {
                var query = "SELECT name FROM sqlite_master WHERE type='table'";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var tableName = reader["name"]?.ToString();
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        tables.Add(tableName);
                    }
                }
            }
            catch { }

            return tables;
        }

        private string DecodeFileName(string fileName)
        {
            try
            {
                // 尝试各种解码方式
                if (fileName.Contains("_"))
                {
                    // 手动替换第一个下划线
                    var firstUnderscoreIndex = fileName.IndexOf('_');
                    if (firstUnderscoreIndex > 0)
                    {
                        var result = fileName.Substring(0, firstUnderscoreIndex) + "://" + fileName.Substring(firstUnderscoreIndex + 1);
                        return result.Replace("_", "/");
                    }
                    return fileName.Replace("_", "/");
                }
                
                return Uri.UnescapeDataString(fileName);
            }
            catch
            {
                return fileName;
            }
        }

        private void ShowManualExtractionGuide()
        {
            Console.WriteLine("\n📖 手动提取指南:");
            Console.WriteLine("如果自动提取失败，您可以:");
            Console.WriteLine("1. 关闭所有Chrome窗口");
            Console.WriteLine("2. 重新运行程序");
            Console.WriteLine("3. 或者在Chrome开发者工具中手动复制数据:");
            Console.WriteLine("   - 按F12打开开发者工具");
            Console.WriteLine("   - 转到Application -> Storage -> Local Storage");
            Console.WriteLine("   - 找到aiyzgr-station-adm-web键");
            Console.WriteLine("   - 复制其值");
            Console.WriteLine("\n从您的截图中，我们已经确认数据存在!");
        }
    }
}
