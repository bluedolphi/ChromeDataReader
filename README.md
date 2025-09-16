# Chrome数据读取工具 (ChromeDataReader)

这是一个使用C# WPF开发的图形界面应用程序，用于读取和管理Chrome浏览器的各种本地数据。

## 功能特性

### 🔍 Chrome路径检测
- 自动检测系统中安装的Chrome浏览器路径
- 支持多个Chrome版本（稳定版、Beta版、Dev版等）
- 通过注册表和文件系统扫描相结合的方式
- 图形界面显示Chrome版本、安装路径、用户数据路径等信息

### 🍪 Cookie数据读取
- 读取Chrome浏览器存储的所有网站Cookie
- 解析SQLite数据库格式的Cookie文件
- 图形界面展示Cookie的域名、名称、值、过期时间等详细信息
- 自动处理Chrome时间戳格式转换
- 支持Cookie数据的搜索和筛选

### 📊 Session数据读取
- 读取Chrome本地存储的Session相关文件
- 扫描Sessions目录和Session Storage目录
- 解析各种Session文件格式
- 在专用窗口显示Session ID、域名、创建时间等信息

### 💾 Storage数据读取
- 读取Chrome的LocalStorage和SessionStorage数据
- 解析LevelDB格式的存储文件
- 处理SQLite格式的LocalStorage文件
- 提取键值对信息并按来源分组显示

### 🔧 高级功能
- 特定数据提取工具
- 自定义数据管理器
- 高级搜索工具
- 用户友好的图形界面

## 系统要求

- Windows 10/11 操作系统 (x64/x86)
- .NET 6.0 运行时（程序会自动检测并引导安装）
- 已安装Chrome浏览器
- 管理员权限（推荐，用于访问某些系统文件）

## 🚀 新功能特性

### ✨ 自动.NET框架检测
- 程序启动时自动检测.NET 6.0运行时是否已安装
- 如果未安装，显示友好的安装引导界面
- 自动识别用户系统版本和架构（x86/x64）
- 提供对应系统的.NET运行时下载链接
- 支持一键下载和安装指导

### 📦 最小化单文件打包
- 支持打包为依赖框架的最小单文件可执行程序
- 文件大小仅约1MB（相比自包含版本大幅减小）
- 需要用户预先安装.NET 6.0运行时
- 程序会自动引导用户安装缺失的依赖
- 自动化打包脚本，一键生成x64和x86版本

## 依赖包

- `System.Data.SQLite` - 用于读取SQLite数据库
- `Microsoft.WindowsDesktop.App` - WPF框架支持

## 安装和运行

### 1. 克隆或下载项目文件

确保所有文件都在同一个目录中：
```
ChromeDataReader/
├── ChromeDataReader.csproj
├── TEMP.sln
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── SessionWindow.xaml
├── SessionWindow.xaml.cs
├── ChromePathDetector.cs
├── CookieReader.cs
├── SessionReader.cs
├── StorageReader.cs
├── SpecificDataExtractor.cs
├── CustomDataManager.cs
├── AdvancedSearchTool.cs
├── Models/
│   └── ChromeInfo.cs
└── README.md
```

### 2. 安装依赖

在项目目录中打开命令提示符或PowerShell，运行：

```bash
dotnet restore
```

### 3. 编译项目

```bash
dotnet build TEMP.sln
```

### 4. 运行程序

**开发环境运行：**
```bash
dotnet run --project ChromeDataReader.csproj
```

**编译后运行：**
```bash
dotnet build -c Release
cd bin/Release/net6.0-windows
ChromeDataReader.exe
```

### 5. 自动化打包（推荐）

**使用自动化打包脚本：**
```bash
build-single-file.bat
```

这个脚本会：
- 自动检测.NET SDK是否安装
- 清理之前的构建文件
- 还原NuGet包
- 构建项目
- 生成x64和x86两个版本的最小化单文件可执行程序
- 显示文件大小信息
- 自动重命名输出文件

**手动打包命令：**

创建x64最小化版本：
```bash
dotnet publish ChromeDataReader.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

创建x86最小化版本：
```bash
dotnet publish ChromeDataReader.csproj -c Release -r win-x86 --self-contained false -p:PublishSingleFile=true
```

生成的文件位于：
- x64版本：`publish-minimal/x64/ChromeDataReader-Minimal-x64.exe` (~1MB)
- x86版本：`publish-minimal/x86/ChromeDataReader-Minimal-x86.exe` (~1MB)

**注意**：这些是依赖框架的构建版本，用户需要先安装.NET 6.0运行时。

## 使用说明

### 图形界面操作

1. **启动应用**：运行程序后会打开主窗口
2. **Chrome检测**：程序自动检测并在界面中显示所有Chrome安装信息
3. **数据读取**：
   - 点击"读取Cookie"按钮读取Cookie数据
   - 点击"读取Session"按钮读取Session数据（会打开新窗口）
   - 点击"读取Storage"按钮读取存储数据
4. **数据查看**：
   - 在主界面的数据网格中查看Cookie和Storage数据
   - 使用搜索功能快速定位特定数据
   - 在Session窗口中查看会话相关信息
5. **高级功能**：
   - 使用特定数据提取工具进行定制化数据提取
   - 通过自定义数据管理器管理提取的数据
   - 使用高级搜索工具进行复杂查询

## 注意事项

### 安全和隐私
- 本程序会读取Chrome浏览器的本地数据文件
- 请确保在合法和授权的环境中使用
- 读取的数据可能包含敏感信息，请妥善处理

### 技术限制
- Chrome正在运行时，某些文件可能被锁定，程序会创建临时副本进行读取
- 加密的Cookie值可能无法直接解密（需要DPAPI解密）
- 某些Storage文件格式可能无法完全解析

### 兼容性
- 主要针对Windows系统上的Chrome浏览器
- 支持Chrome的标准安装路径和用户数据目录结构
- 不同Chrome版本的数据格式可能略有差异

## 故障排除

### 常见问题

1. **找不到Chrome安装**
   - 确保Chrome已正确安装
   - 检查是否有非标准安装路径
   - 尝试以管理员权限运行

2. **无法读取Cookie文件**
   - **推荐**：关闭所有Chrome窗口后再运行程序
   - 文件锁定错误是正常现象（Chrome运行时）
   - 检查用户数据目录是否存在
   - 确认SQLite依赖包已正确安装

3. **Session/Storage数据为空**
   - 这是正常现象，取决于Chrome的使用情况
   - 某些数据可能存储在不同的位置
   - 检查Chrome的用户配置文件路径

### 最佳实践

1. **获取完整数据**：关闭Chrome浏览器后运行程序
2. **权限问题**：以管理员身份运行命令提示符
3. **数据安全**：读取的数据包含敏感信息，请妥善处理

### 错误处理

程序包含完善的错误处理机制：
- 文件访问错误会显示具体的错误信息
- 数据解析错误不会中断整个程序运行
- 每个功能模块都有独立的异常处理

## 开发信息

- **开发语言**：C# (.NET 6.0)
- **UI框架**：WPF (Windows Presentation Foundation)
- **架构模式**：MVVM模式，模块化设计，每个功能独立的类
- **数据模型**：使用强类型模型类定义数据结构
- **错误处理**：多层次异常处理和用户友好的错误提示

### 项目结构

- **MainWindow.xaml/cs** - 主窗口界面和逻辑
- **SessionWindow.xaml/cs** - Session数据查看窗口
- **DotNetInstallGuideWindow.xaml/cs** - .NET安装引导窗口
- **App.xaml.cs** - 应用程序启动和.NET框架检测
- **SystemInfoDetector.cs** - 系统信息检测工具
- **ChromePathDetector.cs** - Chrome路径检测模块
- **CookieReader.cs** - Cookie数据读取模块
- **SessionReader.cs** - Session数据读取模块
- **StorageReader.cs** - Storage数据读取模块
- **SpecificDataExtractor.cs** - 特定数据提取工具
- **CustomDataManager.cs** - 自定义数据管理器
- **AdvancedSearchTool.cs** - 高级搜索工具
- **Models/ChromeInfo.cs** - 数据模型定义
- **build-single-file.bat** - 自动化打包脚本

## 许可证

本项目仅用于学习和测试目的。使用时请遵守相关法律法规和Chrome浏览器的使用条款。

---

**创建时间**：2025-09-16
**版本**：1.0
**类型**：WPF桌面应用程序
**作者**：Augment Agent

## 更新日志

### v1.2 (2025-09-16) - 最小化打包策略
- ✅ 改为依赖框架的最小化单文件打包
- ✅ 文件大小从150MB减小到约1MB
- ✅ 保留.NET框架检测和安装引导功能
- ✅ 更新打包脚本支持最小化构建
- ✅ 更新文档说明新的打包策略

### v1.1 (2025-09-16) - 新增.NET框架检测功能
- ✅ 新增程序启动时.NET 6.0运行时自动检测
- ✅ 创建友好的.NET安装引导界面
- ✅ 自动识别用户系统版本和架构（x86/x64）
- ✅ 提供对应系统的.NET运行时下载链接
- ✅ 支持一键下载和安装指导
- ✅ 创建自动化打包脚本（build-single-file.bat）
- ✅ 支持同时生成x64和x86版本
- ✅ 优化项目配置，支持单文件发布
- ✅ 更新README文档，添加详细使用说明

### v1.0 (2025-09-16)
- ✅ 修复了所有编译错误
- ✅ 添加了缺失的using语句
- ✅ 项目可以正常构建和运行
- ✅ WPF图形界面正常显示
- ✅ 所有核心功能模块正常工作
- ✅ 成功打包为独立可执行文件
- ✅ 创建了发布版本和压缩包
- ✅ 创建了真正的单文件版本（155MB）
- ✅ 优化了打包和分发流程

## 发布文件

项目已打包为以下格式：

### 🚀 最小化单文件版本（推荐）

1. **ChromeDataReader-Minimal-x64.exe** - 64位最小化版本
   - 文件大小：约 1MB
   - 依赖框架的单文件版本
   - 需要预先安装.NET 6.0运行时
   - 程序会自动引导安装缺失依赖

2. **ChromeDataReader-Minimal-x86.exe** - 32位最小化版本
   - 文件大小：约 1MB
   - 适用于32位系统
   - 同样需要.NET 6.0运行时支持

### 📁 多文件版本

3. **ChromeDataReader-Release/** - 发布文件夹
   - 包含 ChromeDataReader.exe 和所有依赖文件
   - 包含使用说明.txt 文件
   - 可直接运行，无需安装 .NET

4. **ChromeDataReader-v1.0.zip** - 多文件压缩包
   - 包含完整的发布文件夹内容
   - 文件大小约 64MB
   - 便于分发和部署
