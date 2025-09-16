using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ChromeDataReader
{
    /// <summary>
    /// 系统信息检测工具类
    /// </summary>
    public static class SystemInfoDetector
    {
        /// <summary>
        /// 获取操作系统信息
        /// </summary>
        /// <returns>操作系统信息字符串</returns>
        public static string GetOperatingSystemInfo()
        {
            try
            {
                var osVersion = Environment.OSVersion;
                var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var osName = GetWindowsVersionName();
                
                return $"{osName} ({architecture})";
            }
            catch (Exception ex)
            {
                return $"无法获取系统信息: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取Windows版本名称
        /// </summary>
        /// <returns>Windows版本名称</returns>
        private static string GetWindowsVersionName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString();
                        var displayVersion = key.GetValue("DisplayVersion")?.ToString();
                        
                        if (!string.IsNullOrEmpty(productName))
                        {
                            if (!string.IsNullOrEmpty(displayVersion))
                            {
                                return $"{productName} {displayVersion}";
                            }
                            return productName;
                        }
                    }
                }
            }
            catch
            {
                // 如果无法从注册表获取，使用Environment.OSVersion
            }

            var version = Environment.OSVersion.Version;
            return version.Major switch
            {
                10 => version.Build >= 22000 ? "Windows 11" : "Windows 10",
                6 => version.Minor switch
                {
                    3 => "Windows 8.1",
                    2 => "Windows 8",
                    1 => "Windows 7",
                    0 => "Windows Vista",
                    _ => "Windows"
                },
                _ => $"Windows {version.Major}.{version.Minor}"
            };
        }

        /// <summary>
        /// 检测.NET 6.0运行时是否已安装
        /// </summary>
        /// <returns>如果已安装返回true，否则返回false</returns>
        public static bool IsDotNet6RuntimeInstalled()
        {
            try
            {
                // 方法1：检查运行时版本
                var runtimeVersion = RuntimeInformation.FrameworkDescription;
                if (runtimeVersion.Contains(".NET 6.") || runtimeVersion.Contains(".NET Core 6."))
                {
                    return true;
                }

                // 方法2：检查注册表中的.NET安装信息
                return CheckDotNetFromRegistry();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从注册表检查.NET安装信息
        /// </summary>
        /// <returns>如果找到.NET 6.0安装信息返回true</returns>
        private static bool CheckDotNetFromRegistry()
        {
            try
            {
                // 检查64位注册表
                if (Environment.Is64BitOperatingSystem)
                {
                    if (CheckRegistryPath(@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost") ||
                        CheckRegistryPath(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost"))
                    {
                        return true;
                    }
                }

                // 检查32位注册表
                if (CheckRegistryPath(@"SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedhost"))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查指定注册表路径
        /// </summary>
        /// <param name="registryPath">注册表路径</param>
        /// <returns>如果找到.NET 6.0版本返回true</returns>
        private static bool CheckRegistryPath(string registryPath)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        var version = key.GetValue("Version")?.ToString();
                        if (!string.IsNullOrEmpty(version) && version.StartsWith("6."))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // 忽略异常
            }
            return false;
        }

        /// <summary>
        /// 获取.NET 6.0下载链接
        /// </summary>
        /// <returns>.NET 6.0下载链接</returns>
        public static string GetDotNet6DownloadUrl()
        {
            var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            return $"https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0";
        }

        /// <summary>
        /// 获取推荐的.NET 6.0运行时下载链接
        /// </summary>
        /// <returns>推荐的下载链接</returns>
        public static string GetRecommendedDotNet6RuntimeUrl()
        {
            var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            return $"https://download.microsoft.com/download/6/0/f/60fc8c9b-2c8b-4b8e-b5e8-b5e8b5e8b5e8/dotnet-runtime-6.0.25-win-{architecture}.exe";
        }

        /// <summary>
        /// 获取系统架构信息
        /// </summary>
        /// <returns>系统架构字符串</returns>
        public static string GetSystemArchitecture()
        {
            return Environment.Is64BitOperatingSystem ? "64位 (x64)" : "32位 (x86)";
        }

        /// <summary>
        /// 获取当前.NET运行时信息
        /// </summary>
        /// <returns>.NET运行时信息</returns>
        public static string GetCurrentDotNetInfo()
        {
            try
            {
                return RuntimeInformation.FrameworkDescription;
            }
            catch
            {
                return "无法获取.NET运行时信息";
            }
        }
    }
}
