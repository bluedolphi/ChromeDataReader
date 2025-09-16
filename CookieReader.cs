using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using ChromeDataReader.Models;

namespace ChromeDataReader
{
    public class CookieReader
    {
        public List<CookieInfo> ReadCookies(string userDataPath)
        {
            var cookies = new List<CookieInfo>();
            var cookiePath = Path.Combine(userDataPath, "Default", "Network", "Cookies");

            if (!File.Exists(cookiePath))
            {
                Console.WriteLine($"Cookie文件不存在: {cookiePath}");
                return cookies;
            }

            try
            {
                // 创建临时副本以避免文件锁定
                var tempCookiePath = Path.GetTempFileName();

                // 尝试多次复制文件，处理文件锁定问题
                bool copySuccess = false;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.Copy(cookiePath, tempCookiePath, true);
                        copySuccess = true;
                        break;
                    }
                    catch (IOException)
                    {
                        if (i == 2) throw; // 最后一次尝试失败则抛出异常
                        Thread.Sleep(1000); // 等待1秒后重试
                    }
                }

                if (!copySuccess)
                {
                    Console.WriteLine("无法访问Cookie文件，Chrome可能正在运行。请关闭Chrome后重试。");
                    return cookies;
                }

                using var connection = new SQLiteConnection($"Data Source={tempCookiePath};Version=3;");
                connection.Open();

                var query = @"
                    SELECT 
                        host_key,
                        name,
                        value,
                        path,
                        expires_utc,
                        creation_utc,
                        last_access_utc,
                        is_secure,
                        is_httponly
                    FROM cookies 
                    ORDER BY host_key, name";

                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var cookie = new CookieInfo
                    {
                        Domain = reader["host_key"]?.ToString() ?? string.Empty,
                        Name = reader["name"]?.ToString() ?? string.Empty,
                        Value = reader["value"]?.ToString() ?? string.Empty,
                        Path = reader["path"]?.ToString() ?? string.Empty,
                        IsSecure = Convert.ToBoolean(reader["is_secure"]),
                        IsHttpOnly = Convert.ToBoolean(reader["is_httponly"])
                    };

                    // 转换Chrome时间戳 (微秒，从1601年1月1日开始)
                    if (long.TryParse(reader["expires_utc"]?.ToString(), out var expiresUtc))
                    {
                        cookie.ExpiresUtc = ConvertChromeTimestamp(expiresUtc);
                    }

                    if (long.TryParse(reader["creation_utc"]?.ToString(), out var creationUtc))
                    {
                        cookie.CreationUtc = ConvertChromeTimestamp(creationUtc);
                    }

                    if (long.TryParse(reader["last_access_utc"]?.ToString(), out var lastAccessUtc))
                    {
                        cookie.LastAccessUtc = ConvertChromeTimestamp(lastAccessUtc);
                    }

                    cookies.Add(cookie);
                }

                // 清理临时文件
                File.Delete(tempCookiePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取Cookie错误: {ex.Message}");
            }

            return cookies;
        }

        private DateTime ConvertChromeTimestamp(long chromeTimestamp)
        {
            if (chromeTimestamp == 0)
                return DateTime.MinValue;

            try
            {
                // Chrome时间戳是从1601年1月1日开始的微秒数
                var epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                // 将微秒转换为毫秒，然后添加到epoch
                return epoch.AddMilliseconds(chromeTimestamp / 1000.0);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public void DisplayCookies(List<CookieInfo> cookies)
        {
            Console.WriteLine($"\n=== Cookie信息 (共{cookies.Count}个) ===");
            
            var groupedCookies = cookies.GroupBy(c => c.Domain).OrderBy(g => g.Key);

            foreach (var domainGroup in groupedCookies)
            {
                Console.WriteLine($"\n域名: {domainGroup.Key}");
                Console.WriteLine(new string('-', 50));

                foreach (var cookie in domainGroup.OrderBy(c => c.Name))
                {
                    Console.WriteLine($"  名称: {cookie.Name}");
                    Console.WriteLine($"  值: {(cookie.Value.Length > 50 ? cookie.Value.Substring(0, 50) + "..." : cookie.Value)}");
                    Console.WriteLine($"  路径: {cookie.Path}");
                    Console.WriteLine($"  过期时间: {(cookie.ExpiresUtc == DateTime.MinValue ? "会话Cookie" : cookie.ExpiresUtc.ToString("yyyy-MM-dd HH:mm:ss"))}");
                    Console.WriteLine($"  安全: {cookie.IsSecure}, HttpOnly: {cookie.IsHttpOnly}");
                    Console.WriteLine($"  创建时间: {cookie.CreationUtc:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine();
                }
            }
        }
    }
}
