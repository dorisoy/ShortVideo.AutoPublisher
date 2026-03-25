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

    public DouyinAgent(BrowserManager browserManager, AppSettings settings, ILogger<DouyinAgent> logger)
        : base(browserManager, settings, logger)
    {
    }

    protected override async Task<bool> CheckLoginStatusAsync(CancellationToken ct = default)
    {
        if (Page == null) return false;

        try
        {
            // 检查是否存在登录用户标识
            var avatar = await Page.QuerySelectorAsync(".avatar, .user-avatar, [class*='avatar']");
            return avatar != null;
        }
        catch
        {
            return false;
        }
    }

    protected override async Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct)
    {
        if (Page == null) return false;

        try
        {
            // 点击上传按钮
            var uploadInput = await Page.QuerySelectorAsync("input[type='file']");
            if (uploadInput != null)
            {
                await uploadInput.SetInputFilesAsync(video.FilePath);
                progress?.Report(50);

                // 等待上传完成
                await Page.WaitForSelectorAsync(".upload-success, .progress-complete, [class*='success']", 
                    new() { Timeout = 300000 }); // 5分钟超时
                
                progress?.Report(100);
                return true;
            }

            return false;
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
            var titleInput = await Page.QuerySelectorAsync("input[placeholder*='标题'], textarea[placeholder*='标题'], .title-input");
            if (titleInput != null)
            {
                await titleInput.FillAsync(title);
                await BrowserManager.RandomDelayAsync(ct);
            }

            // 填写描述
            var descInput = await Page.QuerySelectorAsync("textarea[placeholder*='描述'], .description-input, [class*='desc']");
            if (descInput != null && !string.IsNullOrEmpty(description))
            {
                await descInput.FillAsync(description);
                await BrowserManager.RandomDelayAsync(ct);
            }

            // 添加标签
            foreach (var tag in tags.Take(5)) // 最多5个标签
            {
                var tagInput = await Page.QuerySelectorAsync("input[placeholder*='话题'], input[placeholder*='标签'], .tag-input");
                if (tagInput != null)
                {
                    await tagInput.FillAsync($"#{tag}");
                    await Page.Keyboard.PressAsync("Enter");
                    await BrowserManager.RandomDelayAsync(ct);
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
            // 点击封面设置
            var coverBtn = await Page.QuerySelectorAsync("[class*='cover'], button:has-text('封面')");
            if (coverBtn != null)
            {
                await coverBtn.ClickAsync();
                await BrowserManager.RandomDelayAsync(ct);

                // 上传封面图片
                var coverInput = await Page.QuerySelectorAsync("input[type='file'][accept*='image']");
                if (coverInput != null)
                {
                    await coverInput.SetInputFilesAsync(cover.FilePath);
                    await BrowserManager.RandomDelayAsync(ct);

                    // 确认
                    var confirmBtn = await Page.QuerySelectorAsync("button:has-text('确定'), button:has-text('确认')");
                    if (confirmBtn != null)
                    {
                        await confirmBtn.ClickAsync();
                    }
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
            var publishBtn = await Page.QuerySelectorAsync("button:has-text('发布'), .publish-btn, [class*='publish']");
            if (publishBtn != null)
            {
                await publishBtn.ClickAsync();
                await BrowserManager.RandomDelayAsync(ct);

                // 等待发布完成
                await Page.WaitForSelectorAsync(".publish-success, [class*='success']", 
                    new() { Timeout = 60000 });

                return new PublishResult { Success = true };
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
