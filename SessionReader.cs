using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChromeDataReader.Models;
using Newtonsoft.Json;

namespace ChromeDataReader
{
    public class SessionReader
    {
        public List<SessionInfo> ReadSessions(string userDataPath)
        {
            var sessions = new List<SessionInfo>();

            try
            {
                // Chrome的Session相关文件路径
                var sessionPaths = new[]
                {
                    Path.Combine(userDataPath, "Default", "Sessions"),
                    Path.Combine(userDataPath, "Default", "Session Storage"),
                    Path.Combine(userDataPath, "Default", "Current Session"),
                    Path.Combine(userDataPath, "Default", "Last Session")
                };

                foreach (var sessionPath in sessionPaths)
                {
                    if (Directory.Exists(sessionPath))
                    {
                        sessions.AddRange(ReadSessionDirectory(sessionPath));
                    }
                    else if (File.Exists(sessionPath))
                    {
                        sessions.AddRange(ReadSessionFile(sessionPath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取Session错误: {ex.Message}");
            }

            return sessions;
        }

        private List<SessionInfo> ReadSessionDirectory(string sessionPath)
        {
            var sessions = new List<SessionInfo>();

            try
            {
                var files = Directory.GetFiles(sessionPath, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var session = new SessionInfo
                        {
                            SessionId = Path.GetFileNameWithoutExtension(file),
                            Domain = ExtractDomainFromPath(file),
                            CreatedTime = fileInfo.CreationTime,
                            LastAccessTime = fileInfo.LastAccessTime,
                            Data = new Dictionary<string, object>
                            {
                                ["FilePath"] = file,
                                ["FileSize"] = fileInfo.Length,
                                ["FileType"] = fileInfo.Extension
                            }
                        };

                        // 尝试读取文件内容（如果是文本文件）
                        if (fileInfo.Length < 1024 * 1024) // 小于1MB的文件
                        {
                            try
                            {
                                var content = File.ReadAllText(file);
                                if (IsValidJson(content))
                                {
                                    session.Data["Content"] = JsonConvert.DeserializeObject(content);
                                }
                                else if (!string.IsNullOrWhiteSpace(content) && content.All(c => char.IsControl(c) || char.IsWhiteSpace(c) || char.IsLetterOrDigit(c) || char.IsPunctuation(c)))
                                {
                                    session.Data["Content"] = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                                }
                            }
                            catch
                            {
                                session.Data["Content"] = "二进制文件或无法读取";
                            }
                        }

                        sessions.Add(session);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取Session文件错误 ({file}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取Session目录错误 ({sessionPath}): {ex.Message}");
            }

            return sessions;
        }

        private List<SessionInfo> ReadSessionFile(string sessionFile)
        {
            var sessions = new List<SessionInfo>();

            try
            {
                var fileInfo = new FileInfo(sessionFile);
                var session = new SessionInfo
                {
                    SessionId = Path.GetFileNameWithoutExtension(sessionFile),
                    Domain = "Local",
                    CreatedTime = fileInfo.CreationTime,
                    LastAccessTime = fileInfo.LastAccessTime,
                    Data = new Dictionary<string, object>
                    {
                        ["FilePath"] = sessionFile,
                        ["FileSize"] = fileInfo.Length
                    }
                };

                // 尝试读取文件内容
                if (fileInfo.Length < 1024 * 1024) // 小于1MB
                {
                    try
                    {
                        var content = File.ReadAllText(sessionFile);
                        session.Data["Content"] = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
                    }
                    catch
                    {
                        session.Data["Content"] = "无法读取文件内容";
                    }
                }

                sessions.Add(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取Session文件错误 ({sessionFile}): {ex.Message}");
            }

            return sessions;
        }

        private string ExtractDomainFromPath(string filePath)
        {
            try
            {
                var pathParts = filePath.Split(Path.DirectorySeparatorChar);
                // 尝试从路径中提取域名信息
                for (int i = 0; i < pathParts.Length; i++)
                {
                    if (pathParts[i].Contains("http") || pathParts[i].Contains("www") || pathParts[i].Contains(".com") || pathParts[i].Contains(".org"))
                    {
                        return pathParts[i];
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private bool IsValidJson(string content)
        {
            try
            {
                JsonConvert.DeserializeObject(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void DisplaySessions(List<SessionInfo> sessions)
        {
            Console.WriteLine($"\n=== Session信息 (共{sessions.Count}个) ===");

            var groupedSessions = sessions.GroupBy(s => s.Domain).OrderBy(g => g.Key);

            foreach (var domainGroup in groupedSessions)
            {
                Console.WriteLine($"\n域名/来源: {domainGroup.Key}");
                Console.WriteLine(new string('-', 50));

                foreach (var session in domainGroup.OrderBy(s => s.CreatedTime))
                {
                    Console.WriteLine($"  Session ID: {session.SessionId}");
                    Console.WriteLine($"  创建时间: {session.CreatedTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  最后访问: {session.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
                    
                    foreach (var data in session.Data)
                    {
                        Console.WriteLine($"  {data.Key}: {data.Value}");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
