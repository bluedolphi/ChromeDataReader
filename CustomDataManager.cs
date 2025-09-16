using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ChromeDataReader.Models;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public class CustomDataManager
    {
        public string? ReadCustomData(string userDataPath, string targetUrl, string targetKey)
        {
            try
            {
                // 方法1: 从LocalStorage读取
                var localStorageResult = ReadFromLocalStorage(userDataPath, targetUrl, targetKey);
                if (!string.IsNullOrEmpty(localStorageResult))
                {
                    return localStorageResult;
                }

                // 方法2: 从LevelDB读取
                var levelDbResult = ReadFromLevelDB(userDataPath, targetUrl, targetKey);
                if (!string.IsNullOrEmpty(levelDbResult))
                {
                    return levelDbResult;
                }

                // 方法3: 从SessionStorage读取
                var sessionResult = ReadFromSessionStorage(userDataPath, targetUrl, targetKey);
                if (!string.IsNullOrEmpty(sessionResult))
                {
                    return sessionResult;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"读取自定义数据失败: {ex.Message}");
            }
        }

        private string? ReadFromLocalStorage(string userDataPath, string targetUrl, string targetKey)
        {
            try
            {
                var localStorageDir = Path.Combine(userDataPath, "Default", "Local Storage");
                if (!Directory.Exists(localStorageDir))
                {
                    return null;
                }

                var files = Directory.GetFiles(localStorageDir, "*.localstorage", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (IsMatchingOrigin(fileName, targetUrl))
                        {
                            var result = ReadFromSQLiteFile(file, targetKey);
                            if (!string.IsNullOrEmpty(result))
                            {
                                return result;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略单个文件的错误
                    }
                }
            }
            catch
            {
                // 忽略目录访问错误
            }

            return null;
        }

        private string? ReadFromLevelDB(string userDataPath, string targetUrl, string targetKey)
        {
            try
            {
                var levelDbPath = Path.Combine(userDataPath, "Default", "Local Storage", "leveldb");
                if (!Directory.Exists(levelDbPath))
                {
                    return null;
                }

                var files = Directory.GetFiles(levelDbPath, "*")
                    .Where(f => f.EndsWith(".ldb") || f.EndsWith(".log"))
                    .ToArray();

                foreach (var file in files)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        if (content.Contains(targetKey, StringComparison.OrdinalIgnoreCase))
                        {
                            var extractedValue = ExtractValueFromLevelDB(content, targetKey);
                            if (!string.IsNullOrEmpty(extractedValue))
                            {
                                return extractedValue;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略文件读取错误
                    }
                }
            }
            catch
            {
                // 忽略目录访问错误
            }

            return null;
        }

        private string? ReadFromSessionStorage(string userDataPath, string targetUrl, string targetKey)
        {
            try
            {
                var sessionStoragePath = Path.Combine(userDataPath, "Default", "Session Storage");
                if (!Directory.Exists(sessionStoragePath))
                {
                    return null;
                }

                var files = Directory.GetFiles(sessionStoragePath, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        if (file.EndsWith(".ldb"))
                        {
                            var content = File.ReadAllText(file);
                            if (content.Contains(targetKey, StringComparison.OrdinalIgnoreCase))
                            {
                                var extractedValue = ExtractValueFromLevelDB(content, targetKey);
                                if (!string.IsNullOrEmpty(extractedValue))
                                {
                                    return extractedValue;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略文件读取错误
                    }
                }
            }
            catch
            {
                // 忽略目录访问错误
            }

            return null;
        }

        private string? ReadFromSQLiteFile(string filePath, string targetKey)
        {
            try
            {
                var tempFile = Path.GetTempFileName();
                File.Copy(filePath, tempFile, true);

                using var connection = new SQLiteConnection($"Data Source={tempFile};Version=3;");
                connection.Open();

                var tables = GetTableNames(connection);
                
                foreach (var table in tables)
                {
                    try
                    {
                        var query = $"SELECT * FROM {table}";
                        using var command = new SQLiteCommand(query, connection);
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = reader[i]?.ToString() ?? "";

                                if (columnName.Equals("key", StringComparison.OrdinalIgnoreCase) && 
                                    value.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                                {
                                    // 找到匹配的键，查找对应的值
                                    for (int j = 0; j < reader.FieldCount; j++)
                                    {
                                        var valueColumnName = reader.GetName(j);
                                        if (valueColumnName.Equals("value", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var result = reader[j]?.ToString();
                                            File.Delete(tempFile);
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略表读取错误
                    }
                }

                File.Delete(tempFile);
            }
            catch
            {
                // 忽略SQLite读取错误
            }

            return null;
        }

        private string? ExtractValueFromLevelDB(string content, string targetKey)
        {
            try
            {
                var keyIndex = content.IndexOf(targetKey, StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) return null;

                // 查找键后面的JSON数据
                var searchStart = keyIndex + targetKey.Length;
                var jsonStart = content.IndexOf("{", searchStart);
                
                if (jsonStart >= 0)
                {
                    var jsonData = ExtractJSONFromString(content.Substring(jsonStart));
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        return jsonData;
                    }
                }

                // 如果没有找到JSON，尝试查找其他格式的值
                var valueStart = searchStart;
                var valueEnd = content.IndexOfAny(new char[] { '\0', '\n', '\r' }, valueStart);
                if (valueEnd > valueStart)
                {
                    return content.Substring(valueStart, valueEnd - valueStart).Trim();
                }
            }
            catch
            {
                // 忽略提取错误
            }

            return null;
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

        private bool IsMatchingOrigin(string fileName, string targetUrl)
        {
            try
            {
                var uri = new Uri(targetUrl);
                var domain = uri.Host;
                
                return fileName.Contains(domain, StringComparison.OrdinalIgnoreCase) ||
                       fileName.Contains(targetUrl, StringComparison.OrdinalIgnoreCase) ||
                       DecodeFileName(fileName).Contains(domain, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string DecodeFileName(string fileName)
        {
            try
            {
                if (fileName.Contains("_"))
                {
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

        public bool WriteCustomData(string userDataPath, string targetUrl, string targetKey, string value)
        {
            // 写入功能需要Chrome扩展或特殊API支持
            // 这里只是一个占位符实现
            throw new NotImplementedException("写入功能需要Chrome扩展支持");
        }
    }
}
