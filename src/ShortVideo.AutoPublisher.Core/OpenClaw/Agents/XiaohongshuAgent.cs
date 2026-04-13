using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// 小红书代理
/// </summary>
public class XiaohongshuAgent : AgentBase
{
    public override PlatformType Platform => PlatformType.Xiaohongshu;
    public override string Name => "小红书代理";

    // 小红书创作者平台选择器
    private static class Selectors
    {
        // 登录状态检测
        public static readonly string[] LoginIndicators = [
            ".user-avatar",
            "[class*='avatar']",
            ".creator-info",
            "[class*='nickname']",
            ".user-info"
        ];

        // 文件上传
        public const string FileInput = "input[type='file'][accept*='video']";
        public const string FileInputFallback = "input[type='file']";

        // 上传成功标志
        public static readonly string[] UploadSuccessIndicators = [
            "[class*='upload-success']",
            "[class*='upload-complete']",
            ".video-preview",
            "[class*='progress-100']",
            ".upload-done"
        ];

        // 标题输入
        public static readonly string[] TitleInputs = [
            "input[placeholder*='标题']",
            ".title-input input",
            "[data-testid='title-input']",
            ".note-title input"
        ];

        // 描述输入
        public static readonly string[] DescriptionInputs = [
            "textarea[placeholder*='描述']",
            "textarea[placeholder*='内容']",
            ".description-input textarea",
            ".note-content textarea",
            "[contenteditable='true']"
        ];

        // 话题/标签输入
        public static readonly string[] TagInputs = [
            "input[placeholder*='话题']",
            "input[placeholder*='标签']",
            ".tag-input input",
            "[class*='topic-input']"
        ];

        // 封面设置
        public const string CoverFileInput = "input[type='file'][accept*='image']";

        // 发布按钮
        public static readonly string[] PublishButtons = [
            "button:has-text('发布')",
            "button:has-text('立即发布')",
            "[class*='publish-btn']",
            ".submit-btn",
            "button[type='submit']"
        ];

        // 发布成功标志
        public static readonly string[] PublishSuccessIndicators = [
            "[class*='success']",
            ":has-text('发布成功')",
            ":has-text('已发布')",
            ".publish-success"
        ];
    }

    public XiaohongshuAgent(BrowserManager browserManager, AppSettings settings, ILogger<XiaohongshuAgent> logger)
        : base(browserManager, settings, logger)
    {
    }

    protected override async Task<bool> CheckLoginStatusAsync(CancellationToken ct = default)
    {
        if (Page == null) return false;
        try
        {
            foreach (var selector in Selectors.LoginIndicators)
            {
                var element = await Page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    Logger.LogDebug("小红书登录状态确认，匹配选择器: {Selector}", selector);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "检查小红书登录状态异常");
            return false;
        }
    }

    protected override async Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            var uploadInput = await Page.QuerySelectorAsync(Selectors.FileInput)
                           ?? await Page.QuerySelectorAsync(Selectors.FileInputFallback);

            if (uploadInput == null)
            {
                Logger.LogError("小红书: 未找到文件上传输入框");
                return false;
            }

            Logger.LogInformation("小红书: 开始上传视频 {FilePath}", video.FilePath);
            await uploadInput.SetInputFilesAsync(video.FilePath);
            progress?.Report(30);

            // 等待上传完成
            var uploadCompleted = false;
            foreach (var selector in Selectors.UploadSuccessIndicators)
            {
                try
                {
                    await Page.WaitForSelectorAsync(selector, new() { Timeout = 300000 });
                    uploadCompleted = true;
                    Logger.LogDebug("小红书上传完成，匹配: {Selector}", selector);
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            if (!uploadCompleted)
            {
                await Task.Delay(30000, ct);
                var hasNextStep = false;
                foreach (var selector in Selectors.TitleInputs)
                {
                    if (await Page.QuerySelectorAsync(selector) != null)
                    {
                        hasNextStep = true;
                        break;
                    }
                }
                if (!hasNextStep)
                {
                    Logger.LogError("小红书: 视频上传超时");
                    return false;
                }
            }

            progress?.Report(100);
            Logger.LogInformation("小红书: 视频上传成功");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "小红书视频上传失败");
            return false;
        }
    }

    protected override async Task<bool> DoSetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            // 填写标题
            foreach (var selector in Selectors.TitleInputs)
            {
                var titleInput = await Page.QuerySelectorAsync(selector);
                if (titleInput != null)
                {
                    await titleInput.ClickAsync();
                    await Page.Keyboard.PressAsync("Control+a");
                    await titleInput.FillAsync(title);
                    await BrowserManager.RandomDelayAsync(ct);
                    Logger.LogDebug("小红书: 标题已填写");
                    break;
                }
            }

            // 填写描述
            if (!string.IsNullOrEmpty(description))
            {
                foreach (var selector in Selectors.DescriptionInputs)
                {
                    var descInput = await Page.QuerySelectorAsync(selector);
                    if (descInput != null)
                    {
                        await descInput.ClickAsync();
                        await Page.Keyboard.PressAsync("Control+a");
                        await descInput.FillAsync(description);
                        await BrowserManager.RandomDelayAsync(ct);
                        Logger.LogDebug("小红书: 描述已填写");
                        break;
                    }
                }
            }

            // 添加标签
            foreach (var tag in tags.Take(5))
            {
                foreach (var selector in Selectors.TagInputs)
                {
                    var tagInput = await Page.QuerySelectorAsync(selector);
                    if (tagInput != null)
                    {
                        await tagInput.FillAsync($"#{tag}");
                        await Page.Keyboard.PressAsync("Enter");
                        await BrowserManager.RandomDelayAsync(ct);
                        Logger.LogDebug("小红书: 标签已添加 #{Tag}", tag);
                        break;
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "小红书元数据设置失败");
            return false;
        }
    }

    protected override async Task<bool> DoSetCoverImageAsync(CoverImage cover, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            var coverInput = await Page.QuerySelectorAsync(Selectors.CoverFileInput);
            if (coverInput != null)
            {
                await coverInput.SetInputFilesAsync(cover.FilePath);
                await BrowserManager.RandomDelayAsync(ct);
                Logger.LogDebug("小红书: 封面设置完成");
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "小红书封面设置失败");
            return false;
        }
    }

    protected override async Task<PublishResult> DoPublishAsync(CancellationToken ct)
    {
        if (Page == null) return new PublishResult { Success = false, ErrorMessage = "页面未初始化" };
        try
        {
            foreach (var selector in Selectors.PublishButtons)
            {
                var publishBtn = await Page.QuerySelectorAsync(selector);
                if (publishBtn != null)
                {
                    await publishBtn.ClickAsync();
                    Logger.LogDebug("小红书: 已点击发布按钮");

                    // 等待发布成功
                    foreach (var successSelector in Selectors.PublishSuccessIndicators)
                    {
                        try
                        {
                            await Page.WaitForSelectorAsync(successSelector, new() { Timeout = 60000 });
                            Logger.LogInformation("小红书: 发布成功");
                            return new PublishResult { Success = true };
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                    }

                    // 备用方案
                    await Task.Delay(5000, ct);
                    var currentUrl = Page.Url;
                    if (currentUrl.Contains("success") || currentUrl.Contains("note"))
                    {
                        Logger.LogInformation("小红书: 发布成功（页面跳转确认）");
                        return new PublishResult { Success = true };
                    }

                    return new PublishResult { Success = false, ErrorMessage = "发布后未检测到成功标志" };
                }
            }
            return new PublishResult { Success = false, ErrorMessage = "未找到发布按钮" };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "小红书发布失败");
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
