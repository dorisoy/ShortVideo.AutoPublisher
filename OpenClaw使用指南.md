# OpenClaw AI 代理使用指南

> 本指南介绍如何在 ShortVideo.AutoPublisher 中配置和使用 OpenClaw AI 代理实现视频自动发布

## 一、配置文件说明

配置文件位于 `src/ShortVideo.AutoPublisher/appsettings.json`：

```json
{
  "Database": {
    "FilePath": "Data/autopublisher.db"  // SQLite 数据库路径
  },
  "Network": {
    "ConnectionTimeoutSeconds": 30,      // 连接超时
    "MaxRetries": 3,                     // 最大重试次数
    "InitialRetryDelaySeconds": 1,       // 初始重试延迟
    "MaxRetryDelaySeconds": 30,          // 最大重试延迟
    "NetworkCheckIntervalSeconds": 5     // 网络检查间隔
  },
  "Browser": {
    "Headless": false,                   // 是否无头模式（调试时设为 false）
    "PageLoadTimeoutSeconds": 60,        // 页面加载超时
    "ActionTimeoutSeconds": 30,          // 操作超时
    "DefaultDelayMs": 1000,              // 默认延迟
    "MinDelayMs": 500,                   // 最小延迟
    "MaxDelayMs": 2000                   // 最大延迟
  },
  "Agent": {
    "EnableAiAgent": true,               // 启用 AI 代理
    "MaxConcurrentTasks": 3,             // 最大并发任务数
    "TaskQueueSize": 100,                // 任务队列大小
    "EnableAutoRetry": true,             // 自动重试
    "RetryCount": 3,                     // 重试次数
    "RetryDelaySeconds": 5,              // 重试延迟
    "EnableAntiDetection": true          // 反检测机制
  }
}
```

### 配置项详解

| 分类 | 配置项 | 默认值 | 说明 |
|------|--------|--------|------|
| **数据库** | FilePath | Data/autopublisher.db | SQLite 数据库文件路径 |
| **网络** | ConnectionTimeoutSeconds | 30 | 网络连接超时时间（秒） |
| **网络** | MaxRetries | 3 | 网络请求最大重试次数 |
| **网络** | InitialRetryDelaySeconds | 1 | 初始重试延迟（秒） |
| **网络** | MaxRetryDelaySeconds | 30 | 最大重试延迟（秒） |
| **浏览器** | Headless | false | 是否使用无头模式 |
| **浏览器** | PageLoadTimeoutSeconds | 60 | 页面加载超时（秒） |
| **浏览器** | ActionTimeoutSeconds | 30 | 操作超时（秒） |
| **浏览器** | DefaultDelayMs | 1000 | 默认操作延迟（毫秒） |
| **代理** | EnableAiAgent | true | 是否启用 AI 代理 |
| **代理** | MaxConcurrentTasks | 3 | 最大并发任务数 |
| **代理** | TaskQueueSize | 100 | 任务队列大小 |
| **代理** | EnableAutoRetry | true | 是否启用自动重试 |
| **代理** | EnableAntiDetection | true | 是否启用反检测机制 |

---

## 二、使用方式

### 方式一：WPF 桌面应用

#### 1. 启动应用

```bash
cd src/ShortVideo.AutoPublisher/bin/Debug/net8.0-windows
./ShortVideo.AutoPublisher.exe
```

#### 2. 添加账号（账号管理页面）

- 选择平台（抖音/小红书/百家号/视频号/头条）
- 输入账号名称
- 粘贴从浏览器获取的 Cookie 数据

#### 3. 导入视频（视频库页面）

- 点击「添加视频」
- 填写标题、描述、标签
- 选择视频文件

#### 4. 创建发布任务（发布任务页面）

- 选择视频 → 选择目标平台 → 确认发布
- 任务自动加入队列

#### 5. 启动调度器

- 点击「启动调度器」按钮
- AI 代理自动执行发布任务

---

### 方式二：CLI 命令行

#### 1. 添加账号

```bash
autopub account add -p douyin -n "我的账号" -c "你的cookie数据"
```

#### 2. 添加视频

```bash
autopub video add -f "D:/Videos/demo.mp4" -t "视频标题" --tags "测试,演示"
```

#### 3. 创建发布任务

```bash
# 发布到指定平台
autopub publish -v 1 -p douyin

# 发布到所有已配置平台
autopub publish -v 1 --all

# 定时发布
autopub publish -v 1 -p xiaohongshu -s "2026-04-01 10:00"
```

#### 4. 启动调度器

```bash
autopub scheduler start
```

---

### 方式三：MCP Server（AI Agent 调用）

#### 配置 Claude Desktop

编辑配置文件：
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "autopublisher": {
      "command": "dotnet",
      "args": ["run", "--project", "D:/Git/ShortVideo.AutoPublisher/src/ShortVideo.AutoPublisher.Mcp"]
    }
  }
}
```

#### 在 Claude 中调用

```
用户: 帮我列出视频库中的所有视频
用户: 把视频ID为1的视频发布到抖音
用户: 启动调度器开始发布
```

#### 可用工具列表

| 工具名称 | 描述 |
|---------|------|
| `list_videos` | 获取视频库列表 |
| `add_video` | 添加视频到库 |
| `get_video` | 获取视频详情 |
| `delete_video` | 删除视频 |
| `search_videos` | 搜索视频 |
| `list_accounts` | 获取平台账号列表 |
| `add_account` | 添加平台账号 |
| `delete_account` | 删除账号 |
| `validate_account` | 验证账号状态 |
| `publish_video` | 发布视频到指定平台 |
| `list_tasks` | 获取发布任务列表 |
| `get_task_status` | 查询任务状态 |
| `retry_task` | 重试失败任务 |
| `cancel_task` | 取消任务 |
| `scheduler_start` | 启动任务调度器 |
| `scheduler_stop` | 停止任务调度器 |

---

## 三、获取 Cookie 的方法

### 方法一：浏览器开发者工具

1. 打开浏览器，登录目标平台（如抖音创作者中心）
2. 按 `F12` 打开开发者工具
3. 切换到 **Network** 面板
4. 刷新页面，找到任意请求
5. 在请求头中找到 `Cookie` 字段，复制完整值

### 方法二：使用浏览器扩展

**推荐扩展：**
- Chrome: EditThisCookie、Cookie Editor
- Firefox: Cookie Quick Manager

**导出步骤：**
1. 安装扩展
2. 登录目标平台
3. 点击扩展图标
4. 选择「导出」→ 选择 JSON 格式
5. 粘贴到应用中

### 各平台 Cookie 获取地址

| 平台 | 创作者中心地址 |
|------|---------------|
| 抖音 | https://creator.douyin.com |
| 小红书 | https://creator.xiaohongshu.com |
| 百家号 | https://baijiahao.baidu.com |
| 视频号 | https://channels.weixin.qq.com |
| 今日头条 | https://mp.toutiao.com |

---

## 四、自动发布流程图

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  添加账号   │───▶│  导入视频   │───▶│  创建任务   │
└─────────────┘    └─────────────┘    └─────────────┘
                                              │
                                              ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  发布完成   │◀───│  AI代理执行 │◀───│  启动调度器 │
└─────────────┘    └─────────────┘    └─────────────┘
                         │
            ┌────────────┼────────────┐
            ▼            ▼            ▼
        ┌───────┐    ┌───────┐    ┌───────┐
        │ 登录  │───▶│ 上传  │───▶│ 发布  │
        └───────┘    └───────┘    └───────┘
```

### AI 代理执行流程

1. **初始化** - 启动浏览器实例，配置反检测脚本
2. **登录验证** - 加载 Cookie，验证登录状态
3. **上传视频** - 自动填写表单，上传视频文件
4. **设置元数据** - 填写标题、描述、标签
5. **发布提交** - 完成发布，获取发布链接

---

## 五、关键配置建议

### 场景配置对照表

| 场景 | 配置建议 |
|------|----------|
| **调试阶段** | `Browser.Headless: false`（显示浏览器窗口） |
| **正式运行** | `Browser.Headless: true`（后台运行） |
| **高并发** | `Agent.MaxConcurrentTasks: 5` |
| **网络不稳定** | `Network.MaxRetries: 5` |
| **防检测** | `Agent.EnableAntiDetection: true` |

### 调试模式配置

```json
{
  "Browser": {
    "Headless": false,
    "DefaultDelayMs": 2000
  },
  "Agent": {
    "MaxConcurrentTasks": 1,
    "EnableAutoRetry": false
  }
}
```

### 生产环境配置

```json
{
  "Browser": {
    "Headless": true,
    "DefaultDelayMs": 1000
  },
  "Agent": {
    "MaxConcurrentTasks": 3,
    "EnableAutoRetry": true,
    "RetryCount": 3
  }
}
```

---

## 六、常见问题

### Q1: Cookie 多久会过期？

Cookie 有效期因平台而异：
- 抖音：约 7-30 天
- 小红书：约 15-30 天
- 百家号：约 30 天

**建议**：定期（每周）检查账号状态，及时更新过期的 Cookie。

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
- 设置 `MinDelayMs` 和 `MaxDelayMs` 增加随机延迟

### Q4: 无头模式和有头模式有什么区别？

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| 有头模式 (`Headless: false`) | 显示浏览器窗口，可以看到操作过程 | 调试、排查问题 |
| 无头模式 (`Headless: true`) | 后台运行，不显示窗口 | 正式运行、批量发布 |

---

## 七、支持的平台

| 平台 | 代理类 | 状态 |
|------|--------|------|
| 抖音 | DouyinAgent | ✅ 已支持 |
| 小红书 | XiaohongshuAgent | ✅ 已支持 |
| 百家号 | BaijiahaoAgent | ✅ 已支持 |
| 微信视频号 | WeixinChannelAgent | ✅ 已支持 |
| 今日头条 | ToutiaoAgent | ✅ 已支持 |

---

## 八、技术架构

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

代理实现位于：`src/ShortVideo.AutoPublisher.Core/OpenClaw/Agents/`
