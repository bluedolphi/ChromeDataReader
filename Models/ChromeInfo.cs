using System;
using System.Collections.Generic;

namespace ChromeDataReader.Models
{
    public class ChromeInfo
    {
        public string Version { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string UserDataPath { get; set; } = string.Empty;
        public string ProfilePath { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
    }

    public class CookieInfo
    {
        public string Domain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public DateTime CreationUtc { get; set; }
        public DateTime LastAccessUtc { get; set; }
        public bool IsSecure { get; set; }
        public bool IsHttpOnly { get; set; }
    }

    public class SessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }

    public class StorageInfo
    {
        public string Origin { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string StorageType { get; set; } = string.Empty; // LocalStorage or SessionStorage
    }
}
