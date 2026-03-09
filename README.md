# ShortVideo.AutoPublisher 短视频自动发布创作者工具

> 基于 WPF 的多平台短视频一键发布解决方案

### 完整源码: [https://shop.unitos.cn/item/8](https://shop.unitos.cn/item/8)

## 一、产品概述

**ShortVideo.AutoPublisher** 是一款专为内容创作者设计的桌面应用程序，通过集成 OpenClaw AI 代理机器人技术，实现视频内容到多个主流短视频平台的自动化发布。

### 核心价值

- **效率提升**：一次编辑，多平台同步发布，节省重复操作时间
- **智能自动化**：AI驱动的浏览器自动化，模拟真实用户操作
- **稳定可靠**：网络监控与智能重试机制，确保发布成功率
- **统一管理**：集中管理多平台账号、视频内容和发布任务

### 支持平台

| 平台 | 状态 | 功能支持 |
|------|------|----------|
| 抖音 | ✅ 已支持 | 视频上传、标题/标签/封面设置、发布 |
| 小红书 | ✅ 已支持 | 视频上传、笔记编辑、标签设置、发布 |
| 百家号 | ✅ 已支持 | 视频上传、文章编辑、发布 |
| 微信视频号 | ✅ 已支持 | 视频上传、描述编辑、发布 |
| 今日头条 | ✅ 已支持 | 视频上传、标题/标签设置、发布 |

---

### 屏幕

<img src="Screen/1%20(1).jpg"/>

<img src="Screen/1%20(1).png"/>

<img src="Screen/1%20(2).png"/>

<img src="Screen/1%20(3).png"/>

<img src="Screen/1%20(4).png"/>

<img src="Screen/1%20(5).png"/>

<img src="Screen/1%20(6).png"/>

## 二、系统架构

### 2.1 技术栈

```
┌─────────────────────────────────────────────────────────┐
│                    表现层 (Views)                        │
│         WPF-UI 3.0.4 · Fluent Design · MVVM            │
├─────────────────────────────────────────────────────────┤
│                  视图模型层 (ViewModels)                  │
│           CommunityToolkit.Mvvm · 数据绑定              │
├─────────────────────────────────────────────────────────┤
│                   服务层 (Services)                      │
│      VideoContentService · PublishService · ...        │
├─────────────────────────────────────────────────────────┤
│              OpenClaw AI 代理模块                        │
│    IAiAgent · 平台代理 · 任务调度器 · 浏览器管理          │
├─────────────────────────────────────────────────────────┤
│                 基础设施层 (Infrastructure)              │
│   SQLite/Dapper · NetworkMonitor · FileDownloader      │
├─────────────────────────────────────────────────────────┤
│                    领域层 (Domain)                       │
│         实体 · 枚举 · 仓储接口 · 领域事件                 │
└─────────────────────────────────────────────────────────┘
```

### 2.2 核心依赖

| 组件 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时框架 |
| WPF-UI | 3.0.4 | 现代化 UI 组件库 |
| Microsoft.Playwright | 1.40.0 | 浏览器自动化 |
| SQLite | 1.0.118 | 本地数据存储 |
| Dapper | 2.1.35 | 轻量级 ORM |
| Polly | 8.2.0 | 弹性和瞬态故障处理 |
| SixLabors.ImageSharp | 3.1.6 | 图片处理（封面裁切） |
| Serilog | 3.1.1 | 结构化日志 |

### 2.3 项目结构

```
src/ShortVideo.AutoPublisher/
├── Core/                           # 核心基础设施
│   └── Configuration/              # 应用配置管理
│
├── Domain/                         # 领域层
│   ├── Entities/                   # 实体类
│   │   ├── VideoContent.cs         # 视频内容
│   │   ├── PublishTask.cs          # 发布任务
│   │   ├── CookieSession.cs        # Cookie会话
│   │   └── CoverImage.cs           # 封面图片
│   └── Enums/                      # 枚举定义
│       ├── PlatformType.cs         # 平台类型
│       └── PublishTaskStatus.cs    # 任务状态
│
├── Infrastructure/                 # 基础设施层
│   ├── Data/                       # 数据访问
│   │   ├── AppDbContext.cs         # SQLite上下文
│   │   └── Repositories/           # 仓储实现
│   ├── Network/                    # 网络服务
│   │   └── NetworkMonitor.cs       # 网络状态监控
│   └── FileSystem/                 # 文件系统
│       └── FileDownloader.cs       # 大文件下载器
│
├── OpenClaw/                       # AI代理模块
│   ├── Abstractions/               # 抽象接口
│   │   └── IAiAgent.cs             # AI代理接口
│   ├── Agents/                     # 平台代理实现
│   │   ├── AgentBase.cs            # 代理基类
│   │   ├── DouyinAgent.cs          # 抖音代理
│   │   ├── XiaohongshuAgent.cs     # 小红书代理
│   │   ├── BaijiahaoAgent.cs       # 百家号代理
│   │   ├── WeixinChannelAgent.cs   # 视频号代理
│   │   └── ToutiaoAgent.cs         # 头条代理
│   ├── TaskScheduler/              # 任务调度
│   │   └── PublishTaskScheduler.cs # 任务调度器
│   └── Browser/                    # 浏览器控制
│       └── BrowserManager.cs       # Playwright管理器
│
├── Services/                       # 应用服务层
│   ├── VideoContentService.cs      # 视频内容服务
│   ├── PublishService.cs           # 发布服务
│   └── CookieSessionService.cs     # Cookie会话服务
│
├── ViewModels/                     # 视图模型层
│   └── Pages/
│       ├── DashboardViewModel.cs   # 仪表盘
│       ├── VideoManageViewModel.cs # 视频管理
│       ├── PublishTaskViewModel.cs # 发布任务
│       └── SettingsViewModel.cs    # 系统设置
│
└── Views/                          # 视图层
    ├── Windows/
    │   └── MainWindow.xaml         # 主窗口
    └── Pages/
        ├── DashboardPage.xaml      # 仪表盘页面
        ├── VideoManagePage.xaml    # 视频管理页面
        ├── PublishTaskPage.xaml    # 发布任务页面
        ├── CookieSessionPage.xaml  # 账号管理页面
        └── SettingsPage.xaml       # 设置页面
```

---

## 三、核心功能

### 3.1 仪表盘

统一的系统概览界面，提供：

- **统计卡片**：视频总数、今日发布数、待处理任务、失败任务
- **平台状态**：实时显示各平台账号连接状态
- **快捷操作**：一键添加视频、创建任务、管理账号
- **最近任务**：显示最近5条发布任务及其状态

### 3.2 视频库管理

完整的视频内容 CRUD 功能：

- **视频列表**：支持搜索、筛选、排序
- **视频详情**：标题、描述、标签、时长、文件大小
- **封面管理**：为不同平台设置不同比例的封面
- **批量操作**：批量删除、批量发布

### 3.3 发布任务管理

任务队列化管理与监控：

- **任务创建**：选择视频、目标平台、发布时间
- **实时进度**：显示上传、处理、发布各阶段进度
- **状态筛选**：按待处理/进行中/已完成/失败筛选
- **失败重试**：一键重试失败任务
- **任务取消**：支持取消排队中或进行中的任务

### 3.4 账号管理

多平台 Cookie 会话统一管理：

- **账号列表**：显示所有已添加的平台账号
- **状态监控**：实时检测 Cookie 有效性
- **自动刷新**：定期检查并提醒过期会话
- **Cookie导入**：支持从浏览器导出的 Cookie JSON 导入

### 3.5 系统设置

丰富的配置选项：

| 分类 | 配置项 |
|------|--------|
| **网络设置** | 连接超时、读取超时、最大重试次数、重试间隔 |
| **浏览器设置** | 页面加载超时、元素等待超时、操作延迟范围、无头模式 |
| **下载设置** | 最大文件大小(1GB)、缓冲区大小、断点续传开关 |
| **日志设置** | 日志级别、文件日志开关、保留天数 |
| **外观设置** | 主题切换（浅色/深色/跟随系统） |

---

## 四、OpenClaw AI 代理模块

### 4.1 架构设计

```
┌──────────────────────────────────────────────────────┐
│                   IAiAgent 接口                       │
│  - LoginAsync()      登录验证                        │
│  - UploadVideoAsync() 上传视频                       │
│  - SetVideoMetadataAsync() 设置元数据                │
│  - SetCoverImageAsync() 设置封面                     │
│  - PublishAsync()    发布视频                        │
├──────────────────────────────────────────────────────┤
│                   AgentBase 基类                      │
│  - 重试策略 (Polly)                                  │
│  - 浏览器控制 (Playwright)                           │
│  - 验证码检测与处理                                   │
│  - 统一的状态和进度事件                               │
├──────────────────────────────────────────────────────┤
│              平台专用代理实现                          │
│  DouyinAgent │ XiaohongshuAgent │ BaijiahaoAgent    │
│  WeixinChannelAgent │ ToutiaoAgent                  │
└──────────────────────────────────────────────────────┘
```

### 4.2 IAiAgent 接口定义

```csharp
public interface IAiAgent : IDisposable
{
    // 属性
    PlatformType Platform { get; }
    string Name { get; }
    AgentStatus Status { get; }

    // 核心方法
    Task InitializeAsync(CancellationToken ct = default);
    Task<bool> LoginAsync(CookieSession session, CancellationToken ct = default);
    Task<bool> UploadVideoAsync(VideoContent video, IProgress<int>? progress = null, CancellationToken ct = default);
    Task<bool> SetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct = default);
    Task<bool> SetCoverImageAsync(CoverImage cover, CancellationToken ct = default);
    Task<PublishResult> PublishAsync(CancellationToken ct = default);

    // 事件
    event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    event EventHandler<AgentProgressEventArgs>? ProgressChanged;
}
```

### 4.3 任务调度器

```csharp
// 任务调度器核心功能
PublishTaskScheduler scheduler = new PublishTaskScheduler(dbContext);

// 启动调度器
scheduler.Start();

// 添加任务到队列
long taskId = await scheduler.EnqueueAsync(publishTask);

// 取消任务
await scheduler.CancelTaskAsync(taskId);

// 重试失败任务
await scheduler.RetryTaskAsync(taskId);

// 事件监听
scheduler.TaskStatusChanged += (s, e) => { /* 状态变更处理 */ };
scheduler.TaskProgressChanged += (s, e) => { /* 进度更新处理 */ };
```

### 4.4 浏览器自动化

基于 Playwright 的浏览器自动化特性：

- **反检测机制**：注入脚本隐藏自动化特征
- **Cookie 管理**：自动加载/保存平台 Cookie
- **页面等待策略**：智能等待页面加载完成
- **截图功能**：发布失败时自动保存截图用于调试
- **多浏览器支持**：Chromium / Firefox / WebKit

---

## 五、网络弹性设计

### 5.1 网络监控

```csharp
NetworkMonitor monitor = new NetworkMonitor();
monitor.Start();

// 检查网络连通性
bool isConnected = await monitor.CheckConnectivityAsync();

// 测量网络延迟
int latencyMs = await monitor.MeasureLatencyAsync("www.baidu.com");

// 等待网络恢复
await monitor.WaitForConnectivityAsync(cancellationToken);

// 监听状态变化
monitor.StatusChanged += (s, e) => {
    Console.WriteLine($"网络状态: {(e.IsConnected ? "已连接" : "已断开")}");
};
```

### 5.2 重试策略

采用 Polly 实现指数退避重试：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| MaxRetries | 3 | 最大重试次数 |
| InitialDelay | 1秒 | 初始重试延迟 |
| MaxDelay | 30秒 | 最大重试延迟 |
| BackoffMultiplier | 2.0 | 退避倍数 |

### 5.3 大文件下载器

```csharp
FileDownloader downloader = new FileDownloader(networkMonitor);

var result = await downloader.DownloadAsync(
    url: "https://example.com/video.mp4",
    destinationPath: "Downloads/video.mp4",
    progress: new Progress<DownloadProgress>(p => {
        Console.WriteLine($"下载进度: {p.Progress}% - {p.FormattedSpeed}");
    }),
    cancellationToken
);

if (result.Success)
{
    Console.WriteLine($"下载完成: {result.DownloadedBytes} 字节");
}
```

**特性：**
- 最大支持 1GB 文件
- 断点续传支持
- 实时进度报告
- 网络中断自动等待

---

## 六、数据模型

### 6.1 核心实体

#### VideoContent (视频内容)
```csharp
public class VideoContent
{
    public long Id { get; set; }
    public string Title { get; set; }           // 视频标题
    public string Description { get; set; }      // 视频描述
    public string Tags { get; set; }             // 标签(逗号分隔)
    public string LocalFilePath { get; set; }    // 本地文件路径
    public string SourceUrl { get; set; }        // 源URL
    public long FileSizeBytes { get; set; }      // 文件大小
    public int DurationSeconds { get; set; }     // 时长(秒)
    public DateTime CreatedAt { get; set; }      // 创建时间
}
```

#### PublishTask (发布任务)
```csharp
public class PublishTask
{
    public long Id { get; set; }
    public long VideoContentId { get; set; }     // 关联视频ID
    public PlatformType Platform { get; set; }   // 目标平台
    public long CookieSessionId { get; set; }    // 使用的会话ID
    public PublishTaskStatus Status { get; set; } // 任务状态
    public int Progress { get; set; }            // 进度(0-100)
    public string CurrentStep { get; set; }      // 当前步骤
    public string ErrorMessage { get; set; }     // 错误信息
    public int RetryCount { get; set; }          // 已重试次数
    public string PublishedUrl { get; set; }     // 发布后的URL
    public DateTime? ScheduledAt { get; set; }   // 计划发布时间
}
```

#### CookieSession (Cookie会话)
```csharp
public class CookieSession
{
    public long Id { get; set; }
    public PlatformType Platform { get; set; }   // 所属平台
    public string AccountName { get; set; }      // 账号名称
    public string CookieData { get; set; }       // Cookie JSON
    public SessionStatus Status { get; set; }    // 会话状态
    public DateTime ExpiresAt { get; set; }      // 过期时间
    public DateTime LastRefreshAt { get; set; }  // 最后刷新时间
    public bool IsDefault { get; set; }          // 是否默认账号
}
```

### 6.2 枚举定义

```csharp
// 平台类型
public enum PlatformType
{
    Douyin = 1,        // 抖音
    Xiaohongshu = 2,   // 小红书
    Baijiahao = 3,     // 百家号
    WeixinChannel = 4, // 微信视频号
    Toutiao = 5        // 今日头条
}

// 发布任务状态
public enum PublishTaskStatus
{
    Pending = 0,       // 待处理
    Queued = 1,        // 已排队
    Running = 2,       // 执行中
    Uploading = 3,     // 上传中
    Publishing = 4,    // 发布中
    Completed = 5,     // 已完成
    Failed = 6,        // 失败
    Cancelled = 7      // 已取消
}

// 会话状态
public enum SessionStatus
{
    Active = 0,        // 活跃
    Expired = 1,       // 已过期
    Invalid = 2        // 无效
}
```

---

## 七、使用指南

### 7.1 快速开始

1. **添加账号**
   - 进入「账号管理」页面
   - 点击「添加账号」
   - 选择平台，输入账号名称
   - 从浏览器导出 Cookie 并粘贴

2. **导入视频**
   - 进入「视频库」页面
   - 点击「添加视频」
   - 填写标题、描述、标签
   - 选择本地视频文件

3. **创建发布任务**
   - 在视频列表中选择视频
   - 点击「发布」按钮
   - 选择目标平台和账号
   - 确认后任务自动加入队列

4. **监控任务进度**
   - 进入「发布任务」页面
   - 查看任务状态和进度
   - 失败任务可一键重试

### 7.2 最佳实践

- **Cookie 管理**：定期检查 Cookie 有效性，过期前及时更新
- **错峰发布**：避免短时间内大量发布，降低被平台限制风险
- **封面优化**：为不同平台准备合适比例的封面图片
- **网络环境**：确保网络稳定，系统会自动处理短暂的网络抖动

---

## 八、配置说明

### 8.1 应用配置文件

配置文件路径：`appsettings.json`

```json
{
  "Database": {
    "FilePath": "Data/autopublisher.db"
  },
  "Network": {
    "ConnectionTimeoutSeconds": 30,
    "MaxRetries": 3,
    "InitialRetryDelaySeconds": 1,
    "MaxRetryDelaySeconds": 30,
    "NetworkCheckIntervalSeconds": 5
  },
  "Browser": {
    "Headless": false,
    "PageLoadTimeoutSeconds": 60,
    "ActionTimeoutSeconds": 30,
    "DefaultDelayMs": 1000
  },
  "Download": {
    "MaxFileSizeBytes": 1073741824,
    "BufferSize": 81920,
    "EnableResume": true
  },
  "Log": {
    "LogDirectory": "Logs",
    "MinimumLevel": "Information",
    "RetainedFileDays": 30
  }
}
```

### 8.2 数据库结构

系统使用 SQLite 数据库存储数据，主要表结构：

| 表名 | 说明 |
|------|------|
| VideoContents | 视频内容 |
| CookieSessions | Cookie会话 |
| PublishTasks | 发布任务 |
| CoverImages | 封面图片 |
| DownloadRecords | 下载记录 |
| SystemLogs | 系统日志 |

---

## 九、开发扩展

### 9.1 添加新平台支持

1. 在 `PlatformType` 枚举中添加新平台
2. 创建新的代理类继承 `AgentBase`
3. 实现 `IAiAgent` 接口的所有方法
4. 在 `AgentFactory` 中注册新代理

```csharp
public class NewPlatformAgent : AgentBase
{
    public override PlatformType Platform => PlatformType.NewPlatform;
    public override string Name => "新平台";

    protected override async Task<bool> DoLoginAsync(CookieSession session, CancellationToken ct)
    {
        // 实现登录逻辑
    }

    protected override async Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct)
    {
        // 实现上传逻辑
    }

    // ... 实现其他方法
}
```

### 9.2 自定义重试策略

```csharp
var customPolicy = new RetryPolicyBuilder()
    .WithMaxRetries(5)
    .WithInitialDelay(TimeSpan.FromSeconds(2))
    .WithMaxDelay(TimeSpan.FromMinutes(1))
    .WithBackoffMultiplier(3.0)
    .Build();

await agent.ExecuteWithRetryAsync(async () => {
    // 需要重试的操作
}, customPolicy);
```

---

## 十、常见问题

### Q1: Cookie 如何获取？
使用浏览器的开发者工具（F12），在 Network 面板中找到请求，复制 Cookie 值；或使用浏览器扩展导出 Cookie 为 JSON 格式。

### Q2: 发布失败如何处理？
1. 查看任务的错误信息
2. 检查 Cookie 是否过期
3. 确认网络连接正常
4. 查看日志文件获取详细错误信息
5. 使用「重试」功能重新发布

### Q3: 如何避免账号被限制？
- 控制发布频率，避免短时间内大量发布
- 使用随机延迟模拟人工操作
- 定期更换 Cookie
- 关注平台的发布规则变化

### Q4: 支持定时发布吗？
是的，创建任务时可以设置「计划发布时间」，任务调度器会在指定时间自动执行。

---

## 十一、版本演进

### 版本历史

```
┌─────────────────────────────────────────────────────────────────┐
│  v1.0 (2022)        v2.0 (2023)        v3.0 AI (2026)          │
│      │                  │                   │                   │
│   基础版             增强版             智能版                   │
│      │                  │                   │                   │
│  手动上传 ────────▶ 批量管理 ────────▶ AI 自动化               │
│  单平台支持          多平台支持          OpenClaw 集成           │
│  本地存储            数据库存储          智能任务调度             │
└─────────────────────────────────────────────────────────────────┘
```

### v1.0 - 基础版 (2022)

首个正式发布版本，提供基础的视频管理功能。

| 特性 | 说明 |
|------|------|
| 视频管理 | 本地视频文件的基本 CRUD 操作 |
| 单平台 | 仅支持抖音平台 |
| 手动模式 | 需要手动打开浏览器完成上传 |
| 配置文件 | 基于 XML 的简单配置 |

### v2.0 - 增强版 (2023)

引入多平台支持和数据库存储，大幅提升用户体验。

| 特性 | 说明 |
|------|------|
| 多平台支持 | 新增小红书、百家号、视频号、头条 |
| SQLite 数据库 | 本地持久化存储，支持数据检索 |
| 批量操作 | 支持批量导入、批量删除 |
| Cookie 管理 | 初步的会话管理功能 |
| UI 升级 | 采用 WPF-UI 2.x 现代化界面 |
| 封面管理 | 支持为视频设置封面图片 |

### v3.0 AI - 智能版 (2026) ⭐ 当前版本

**革命性升级**：集成 OpenClaw AI 代理技术，实现真正的自动化发布。

| 特性 | 说明 |
|------|------|
| **OpenClaw AI 代理** | 智能浏览器自动化，模拟真实用户操作 |
| **IAiAgent 架构** | 统一的代理接口，支持快速扩展新平台 |
| **任务调度器** | 队列化任务管理，支持并发控制 |
| **Playwright 集成** | 现代化浏览器自动化引擎 |
| **智能重试** | Polly 弹性策略，自动处理网络抖动 |
| **网络监控** | 实时网络状态检测与自动恢复 |
| **大文件支持** | 最大 1GB 视频，断点续传 |
| **WPF-UI 3.0** | 全新 Fluent Design 界面 |
| **验证码处理** | 智能检测，支持人工干预 |
| **状态回调** | 实时进度反馈和事件通知 |

### 版本对比

| 功能 | v1.0 | v2.0 | v3.0 AI |
|------|:----:|:----:|:-------:|
| 抖音支持 | ✅ | ✅ | ✅ |
| 小红书支持 | ❌ | ✅ | ✅ |
| 百家号支持 | ❌ | ✅ | ✅ |
| 视频号支持 | ❌ | ✅ | ✅ |
| 头条支持 | ❌ | ✅ | ✅ |
| 数据库存储 | ❌ | ✅ | ✅ |
| AI 自动化 | ❌ | ❌ | ✅ |
| 任务队列 | ❌ | ❌ | ✅ |
| 智能重试 | ❌ | ❌ | ✅ |
| 网络监控 | ❌ | ❌ | ✅ |
| 断点续传 | ❌ | ❌ | ✅ |
| 定时发布 | ❌ | ❌ | ✅ |

### 当前版本信息

- **版本号**：3.0.0
- **代号**：AI Edition
- **发布日期**：2026年
- **开发者**：unitos.cn
- **技术支持**：https://unitos.cn

### 未来规划 (v3.1+)

- [ ] 更多平台支持（B站、快手）
- [ ] OCR 验证码自动识别
- [ ] 视频智能剪辑
- [ ] 数据分析仪表盘
- [ ] 云端同步功能

---

## 十二、许可协议

本项目基于 MIT 许可证开源。

```
MIT License

Copyright (C) 2024 ShortVideo.AutoPublisher Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software...
```
