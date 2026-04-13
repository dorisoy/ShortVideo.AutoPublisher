using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// 抖音代理
/// </summary>
public class DouyinAgent : AgentBase
{
    public override PlatformType Platform => PlatformType.Douyin;
    public override string Name => "抖音代理";

    // 抖音创作者平台选择器
    private static class Selectors
    {
        // 登录状态检测
        public static readonly string[] LoginIndicators = [
            ".semi-avatar",
            "[class*='avatar']",
            ".user-info",
            "[data-e2e='user-info']"
        ];

        // 文件上传
        public const string FileInput = "input[type='file'][accept*='video']";
        public const string FileInputFallback = "input[type='file']";

        // 上传成功标志
        public static readonly string[] UploadSuccessIndicators = [
            ".upload-success",
            "[class*='progress-complete']",
            "[class*='upload-done']",
            ".semi-progress-circle-text:has-text('100%')"
        ];

        // 标题输入
        public static readonly string[] TitleInputs = [
            "input[placeholder*='标题']",
            "textarea[placeholder*='标题']",
            ".title-editor input",
            ".title-editor textarea",
            "[data-e2e='title-input']"
        ];

        // 描述输入
        public static readonly string[] DescriptionInputs = [
            "textarea[placeholder*='描述']",
            ".desc-editor textarea",
            "[data-e2e='description-input']",
            ".DraftEditor-root"
        ];

        // 话题/标签输入
        public static readonly string[] TagInputs = [
            "input[placeholder*='话题']",
            "input[placeholder*='标签']",
            ".tag-input input",
            "[data-e2e='tag-input']"
        ];

        // 封面设置
        public static readonly string[] CoverButtons = [
            "button:has-text('设置封面')",
            "button:has-text('选择封面')",
            "[class*='cover-btn']",
            ".cover-selector"
        ];
        public const string CoverFileInput = "input[type='file'][accept*='image']";
        public static readonly string[] ConfirmButtons = [
            "button:has-text('确定')",
            "button:has-text('确认')",
            ".semi-button-primary"
        ];

        // 发布按钮
        public static readonly string[] PublishButtons = [
            "button:has-text('发布')",
            ".publish-btn",
            "[data-e2e='publish-button']",
            ".semi-button-primary:has-text('发布')"
        ];

        // 发布成功标志
        public static readonly string[] PublishSuccessIndicators = [
            ".publish-success",
            "[class*='success']",
            ":has-text('发布成功')",
            ":has-text('视频已发布')"
        ];
    }

    public DouyinAgent(BrowserManager browserManager, AppSettings settings, ILogger<DouyinAgent> logger)
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
                    Logger.LogDebug("抖音登录状态确认，匹配选择器: {Selector}", selector);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "检查抖音登录状态异常");
            return false;
        }
    }

    protected override async Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct)
    {
        if (Page == null) return false;

        try
        {
            // 尝试多个文件输入选择器
            var uploadInput = await Page.QuerySelectorAsync(Selectors.FileInput)
                           ?? await Page.QuerySelectorAsync(Selectors.FileInputFallback);

            if (uploadInput == null)
            {
                Logger.LogError("抖音: 未找到文件上传输入框");
                return false;
            }

            Logger.LogInformation("抖音: 开始上传视频 {FilePath}", video.FilePath);
            await uploadInput.SetInputFilesAsync(video.FilePath);
            progress?.Report(30);

            // 等待上传完成，尝试多个成功标志
            var uploadCompleted = false;
            foreach (var selector in Selectors.UploadSuccessIndicators)
            {
                try
                {
                    await Page.WaitForSelectorAsync(selector, new() { Timeout = 300000 }); // 5分钟超时
                    uploadCompleted = true;
                    Logger.LogDebug("抖音上传完成，匹配: {Selector}", selector);
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            if (!uploadCompleted)
            {
                // 备用方案：等待一段时间后检查页面是否有下一步操作的元素
                await Task.Delay(30000, ct); // 等待30秒
                var hasNextStep = await Page.QuerySelectorAsync(Selectors.TitleInputs[0]) != null;
                if (!hasNextStep)
                {
                    Logger.LogError("抖音: 视频上传超时");
                    return false;
                }
            }

            progress?.Report(100);
            Logger.LogInformation("抖音: 视频上传成功");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "抖音视频上传失败");
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
                    Logger.LogDebug("抖音: 标题已填写");
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
                        Logger.LogDebug("抖音: 描述已填写");
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
                        Logger.LogDebug("抖音: 标签已添加 #{Tag}", tag);
                        break;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "抖音元数据设置失败");
            return false;
        }
    }

    protected override async Task<bool> DoSetCoverImageAsync(CoverImage cover, CancellationToken ct)
    {
        if (Page == null) return false;

        try
        {
            // 点击封面设置按钮
            foreach (var selector in Selectors.CoverButtons)
            {
                var coverBtn = await Page.QuerySelectorAsync(selector);
                if (coverBtn != null)
                {
                    await coverBtn.ClickAsync();
                    await BrowserManager.RandomDelayAsync(ct);

                    // 上传封面图片
                    var coverInput = await Page.QuerySelectorAsync(Selectors.CoverFileInput);
                    if (coverInput != null)
                    {
                        await coverInput.SetInputFilesAsync(cover.FilePath);
                        await BrowserManager.RandomDelayAsync(ct);

                        // 确认
                        foreach (var confirmSelector in Selectors.ConfirmButtons)
                        {
                            var confirmBtn = await Page.QuerySelectorAsync(confirmSelector);
                            if (confirmBtn != null)
                            {
                                await confirmBtn.ClickAsync();
                                Logger.LogDebug("抖音: 封面设置完成");
                                break;
                            }
                        }
                    }
                    break;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "抖音封面设置失败");
            return false;
        }
    }

    protected override async Task<PublishResult> DoPublishAsync(CancellationToken ct)
    {
        if (Page == null) return new PublishResult { Success = false, ErrorMessage = "页面未初始化" };

        try
        {
            // 点击发布按钮
            foreach (var selector in Selectors.PublishButtons)
            {
                var publishBtn = await Page.QuerySelectorAsync(selector);
                if (publishBtn != null)
                {
                    await publishBtn.ClickAsync();
                    Logger.LogDebug("抖音: 已点击发布按钮");
                    await BrowserManager.RandomDelayAsync(ct);

                    // 等待发布成功
                    foreach (var successSelector in Selectors.PublishSuccessIndicators)
                    {
                        try
                        {
                            await Page.WaitForSelectorAsync(successSelector, new() { Timeout = 60000 });
                            Logger.LogInformation("抖音: 发布成功");
                            return new PublishResult { Success = true };
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                    }

                    // 备用方案：等待页面跳转
                    await Task.Delay(5000, ct);
                    var currentUrl = Page.Url;
                    if (currentUrl.Contains("success") || currentUrl.Contains("manage"))
                    {
                        Logger.LogInformation("抖音: 发布成功（页面跳转确认）");
                        return new PublishResult { Success = true };
                    }

                    return new PublishResult { Success = false, ErrorMessage = "发布后未检测到成功标志" };
                }
            }

            return new PublishResult { Success = false, ErrorMessage = "未找到发布按钮" };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "抖音发布失败");
            return new PublishResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
