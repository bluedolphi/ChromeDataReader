using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using ChromeDataReader.Models;
using System.Diagnostics;

namespace ChromeDataReader
{
    public class ChromePathDetector
    {
        private readonly List<string> _commonPaths = new()
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            @"C:\Users\{0}\AppData\Local\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files\Google\Chrome Beta\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome Beta\Application\chrome.exe",
            @"C:\Program Files\Google\Chrome Dev\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome Dev\Application\chrome.exe"
        };

        public List<ChromeInfo> DetectChromeInstallations()
        {
            var chromeInstallations = new List<ChromeInfo>();

            // 从注册表检测
            chromeInstallations.AddRange(DetectFromRegistry());

            // 从文件系统检测
            chromeInstallations.AddRange(DetectFromFileSystem());

            // 去重并获取详细信息
            var uniqueInstallations = chromeInstallations
                .GroupBy(c => c.ExecutablePath.ToLower())
                .Select(g => g.First())
                .ToList();

            // 获取每个安装的详细信息
            foreach (var chrome in uniqueInstallations)
            {
                EnrichChromeInfo(chrome);
            }

            return uniqueInstallations;
        }

        private List<ChromeInfo> DetectFromRegistry()
        {
            var installations = new List<ChromeInfo>();

            try
            {
                // 检查HKEY_LOCAL_MACHINE
                using var hklm = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Google\Chrome\BLBeacon");
                if (hklm?.GetValue("version") is string version)
                {
                    var path = hklm.GetValue("path") as string;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        installations.Add(new ChromeInfo
                        {
                            Version = version,
                            ExecutablePath = path,
                            InstallPath = Path.GetDirectoryName(path) ?? string.Empty
                        });
                    }
                }

                // 检查HKEY_CURRENT_USER
                using var hkcu = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Google\Chrome\BLBeacon");
                if (hkcu?.GetValue("version") is string userVersion)
                {
                    var userPath = hkcu.GetValue("path") as string;
                    if (!string.IsNullOrEmpty(userPath) && File.Exists(userPath))
                    {
                        installations.Add(new ChromeInfo
                        {
                            Version = userVersion,
                            ExecutablePath = userPath,
                            InstallPath = Path.GetDirectoryName(userPath) ?? string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册表检测错误: {ex.Message}");
            }

            return installations;
        }

        private List<ChromeInfo> DetectFromFileSystem()
        {
            var installations = new List<ChromeInfo>();
            var userName = Environment.UserName;

            foreach (var pathTemplate in _commonPaths)
            {
                try
                {
                    var path = pathTemplate.Contains("{0}") 
                        ? string.Format(pathTemplate, userName) 
                        : pathTemplate;

                    if (File.Exists(path))
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                        installations.Add(new ChromeInfo
                        {
                            Version = versionInfo.FileVersion ?? "Unknown",
                            ExecutablePath = path,
                            InstallPath = Path.GetDirectoryName(path) ?? string.Empty
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"文件系统检测错误 ({pathTemplate}): {ex.Message}");
                }
            }

            return installations;
        }

        private void EnrichChromeInfo(ChromeInfo chrome)
        {
            try
            {
                // 获取用户数据路径
                var userName = Environment.UserName;
                chrome.UserDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data");

                chrome.ProfilePath = Path.Combine(chrome.UserDataPath, "Default");

                // 检查Chrome是否正在运行
                chrome.IsRunning = Process.GetProcessesByName("chrome").Length > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取Chrome详细信息错误: {ex.Message}");
            }
        }
    }
}
