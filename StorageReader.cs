using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using ChromeDataReader.Models;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public class StorageReader
    {
        public List<StorageInfo> ReadLocalStorage(string userDataPath)
        {
            var storageData = ReadAllStorage(userDataPath);
            return storageData;
        }

        public StorageInfo? FindSpecificStorage(string userDataPath, string targetOrigin, string targetKey)
        {
            var allStorage = ReadAllStorage(userDataPath);

            // 查找特定的键值对
            var result = allStorage.FirstOrDefault(s =>
                (s.Origin.Contains(targetOrigin, StringComparison.OrdinalIgnoreCase) ||
                 s.Key.Contains(targetKey, StringComparison.OrdinalIgnoreCase)) &&
                s.Key.Contains(targetKey, StringComparison.OrdinalIgnoreCase));

            return result;
        }

        private List<StorageInfo> ReadAllStorage(string userDataPath)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                // Local Storage路径
                var localStoragePath = Path.Combine(userDataPath, "Default", "Local Storage", "leveldb");

                if (Directory.Exists(localStoragePath))
                {
                    storageData.AddRange(ReadLevelDbStorage(localStoragePath, "LocalStorage"));
                }

                // Session Storage路径
                var sessionStoragePath = Path.Combine(userDataPath, "Default", "Session Storage");

                if (Directory.Exists(sessionStoragePath))
                {
                    storageData.AddRange(ReadSessionStorageDirectory(sessionStoragePath));
                }

                // 专门查找特定网站的LocalStorage文件
                var localStorageDir = Path.Combine(userDataPath, "Default", "Local Storage");
                if (Directory.Exists(localStorageDir))
                {
                    storageData.AddRange(ReadSpecificLocalStorageFiles(localStorageDir));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取Storage错误: {ex.Message}");
            }

            return storageData;
        }

        private List<StorageInfo> ReadSpecificLocalStorageFiles(string localStorageDir)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                // 查找所有.localstorage文件
                var localStorageFiles = Directory.GetFiles(localStorageDir, "*.localstorage", SearchOption.AllDirectories);

                foreach (var file in localStorageFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var origin = DecodeLocalStorageFileName(fileName);

                        // 检查是否是目标网站
                        if (origin.Contains("aiyzgr") || origin.Contains("aiyouzhan") || fileName.Contains("aiyzgr"))
                        {
                            Console.WriteLine($"找到目标网站的LocalStorage文件: {file}");
                            var specificData = ReadSqliteStorage(file, origin);
                            storageData.AddRange(specificData);

                            // 专门查找aiyzgr-station-adm-web键
                            var targetData = specificData.FirstOrDefault(s =>
                                s.Key.Contains("aiyzgr-station-adm-web", StringComparison.OrdinalIgnoreCase));

                            if (targetData != null)
                            {
                                Console.WriteLine($"🎯 找到目标数据: {targetData.Key} = {targetData.Value}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取LocalStorage文件错误 ({file}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描LocalStorage文件错误: {ex.Message}");
            }

            return storageData;
        }

        private string DecodeLocalStorageFileName(string fileName)
        {
            try
            {
                // Chrome LocalStorage文件名通常是URL编码的
                if (fileName.StartsWith("https_") || fileName.StartsWith("http_"))
                {
                    // 手动替换第一个下划线为://
                    var firstUnderscoreIndex = fileName.IndexOf('_');
                    if (firstUnderscoreIndex > 0)
                    {
                        var result = fileName.Substring(0, firstUnderscoreIndex) + "://" + fileName.Substring(firstUnderscoreIndex + 1);
                        return result.Replace("_", "/");
                    }
                    return fileName.Replace("_", "/");
                }

                // 尝试解码Base64或其他编码
                if (fileName.Length > 10)
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(fileName);
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        // 不是Base64编码
                    }
                }

                return fileName;
            }
            catch
            {
                return fileName;
            }
        }

        private List<StorageInfo> ReadLevelDbStorage(string levelDbPath, string storageType)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                var files = Directory.GetFiles(levelDbPath, "*.ldb")
                    .Concat(Directory.GetFiles(levelDbPath, "*.log"))
                    .ToArray();

                foreach (var file in files)
                {
                    try
                    {
                        var data = ReadLevelDbFile(file, storageType);
                        storageData.AddRange(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取LevelDB文件错误 ({file}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取LevelDB目录错误 ({levelDbPath}): {ex.Message}");
            }

            return storageData;
        }

        private List<StorageInfo> ReadLevelDbFile(string filePath, string storageType)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var content = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 10000)); // 只读取前10KB

                // 尝试提取键值对信息
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.Contains("http") && line.Length > 10)
                    {
                        try
                        {
                            var parts = line.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var origin = ExtractOrigin(parts[0]);
                                var key = parts.Length > 1 ? parts[1] : "Unknown";
                                var value = parts.Length > 2 ? parts[2] : "Unknown";

                                if (!string.IsNullOrWhiteSpace(origin))
                                {
                                    storageData.Add(new StorageInfo
                                    {
                                        Origin = origin,
                                        Key = key.Length > 100 ? key.Substring(0, 100) + "..." : key,
                                        Value = value.Length > 200 ? value.Substring(0, 200) + "..." : value,
                                        StorageType = storageType
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // 忽略解析错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析LevelDB文件错误: {ex.Message}");
            }

            return storageData;
        }

        private List<StorageInfo> ReadSessionStorageDirectory(string sessionStoragePath)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                var files = Directory.GetFiles(sessionStoragePath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        
                        // 跳过太大的文件
                        if (fileInfo.Length > 1024 * 1024) continue;

                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var origin = ExtractOriginFromFileName(fileName);

                        if (fileInfo.Extension.ToLower() == ".localstorage")
                        {
                            // 尝试读取SQLite格式的LocalStorage文件
                            var sqliteData = ReadSqliteStorage(file, origin);
                            storageData.AddRange(sqliteData);
                        }
                        else
                        {
                            // 尝试读取其他格式的文件
                            var content = File.ReadAllText(file);
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                storageData.Add(new StorageInfo
                                {
                                    Origin = origin,
                                    Key = fileName,
                                    Value = content.Length > 200 ? content.Substring(0, 200) + "..." : content,
                                    StorageType = "SessionStorage"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取SessionStorage文件错误 ({file}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取SessionStorage目录错误: {ex.Message}");
            }

            return storageData;
        }

        private List<StorageInfo> ReadSqliteStorage(string filePath, string origin)
        {
            var storageData = new List<StorageInfo>();

            try
            {
                using var connection = new SQLiteConnection($"Data Source={filePath};Version=3;");
                connection.Open();

                // 检查表结构
                var tables = GetTableNames(connection);
                
                foreach (var table in tables)
                {
                    try
                    {
                        var query = $"SELECT * FROM {table} LIMIT 100";
                        using var command = new SQLiteCommand(query, connection);
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            var storage = new StorageInfo
                            {
                                Origin = origin,
                                StorageType = "LocalStorage"
                            };

                            // 尝试获取键值对
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var columnName = reader.GetName(i);
                                var value = reader[i]?.ToString() ?? "";

                                if (columnName.ToLower().Contains("key") && string.IsNullOrEmpty(storage.Key))
                                {
                                    storage.Key = value;
                                }
                                else if (columnName.ToLower().Contains("value") && string.IsNullOrEmpty(storage.Value))
                                {
                                    storage.Value = value.Length > 200 ? value.Substring(0, 200) + "..." : value;
                                }
                            }

                            if (!string.IsNullOrEmpty(storage.Key))
                            {
                                storageData.Add(storage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取SQLite表错误 ({table}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取SQLite Storage错误: {ex.Message}");
            }

            return storageData;
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
            catch (Exception ex)
            {
                Console.WriteLine($"获取表名错误: {ex.Message}");
            }

            return tables;
        }

        private string ExtractOrigin(string input)
        {
            try
            {
                if (input.Contains("http://") || input.Contains("https://"))
                {
                    var uri = new Uri(input);
                    return $"{uri.Scheme}://{uri.Host}";
                }
                
                // 尝试从字符串中提取域名
                var parts = input.Split('_', '.', '/', '\\');
                foreach (var part in parts)
                {
                    if (part.Contains(".com") || part.Contains(".org") || part.Contains(".net") || part.Contains("www"))
                    {
                        return part;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return "Unknown";
        }

        private string ExtractOriginFromFileName(string fileName)
        {
            try
            {
                // Chrome的LocalStorage文件名通常包含编码的域名
                if (fileName.Contains("_"))
                {
                    var parts = fileName.Split('_');
                    foreach (var part in parts)
                    {
                        if (part.Length > 5 && (part.Contains("http") || part.All(c => char.IsLetterOrDigit(c))))
                        {
                            return part;
                        }
                    }
                }
                
                return fileName;
            }
            catch
            {
                return fileName;
            }
        }

        public void DisplayStorage(List<StorageInfo> storageData)
        {
            Console.WriteLine($"\n=== Storage信息 (共{storageData.Count}个) ===");

            var groupedStorage = storageData.GroupBy(s => new { s.Origin, s.StorageType })
                .OrderBy(g => g.Key.Origin)
                .ThenBy(g => g.Key.StorageType);

            foreach (var group in groupedStorage)
            {
                Console.WriteLine($"\n来源: {group.Key.Origin} ({group.Key.StorageType})");
                Console.WriteLine(new string('-', 50));

                foreach (var storage in group.Take(10)) // 限制显示数量
                {
                    Console.WriteLine($"  键: {storage.Key}");
                    Console.WriteLine($"  值: {storage.Value}");
                    Console.WriteLine();
                }

                if (group.Count() > 10)
                {
                    Console.WriteLine($"  ... 还有 {group.Count() - 10} 个项目");
                    Console.WriteLine();
                }
            }
        }
    }
}
