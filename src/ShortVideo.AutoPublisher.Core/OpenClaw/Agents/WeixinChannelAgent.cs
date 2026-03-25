using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// 微信视频号代理
/// </summary>
public class WeixinChannelAgent : AgentBase
{
    public override PlatformType Platform => PlatformType.WeixinChannel;
    public override string Name => "视频号代理";

    public WeixinChannelAgent(BrowserManager browserManager, AppSettings settings, ILogger<WeixinChannelAgent> logger)
        : base(browserManager, settings, logger)
    {
    }

    protected override async Task<bool> CheckLoginStatusAsync(CancellationToken ct = default)
    {
        if (Page == null) return false;
        try { var el = await Page.QuerySelectorAsync("[class*='user'], [class*='avatar']"); return el != null; }
        catch { return false; }
    }

    protected override async Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            var uploadInput = await Page.QuerySelectorAsync("input[type='file']");
            if (uploadInput != null)
            {
                await uploadInput.SetInputFilesAsync(video.FilePath);
                progress?.Report(50);
                await Page.WaitForSelectorAsync("[class*='success'], [class*='complete']", new() { Timeout = 300000 });
                progress?.Report(100);
                return true;
            }
            return false;
        }
        catch (Exception ex) { Logger.LogError(ex, "视频号视频上传失败"); return false; }
    }

    protected override async Task<bool> DoSetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            var titleInput = await Page.QuerySelectorAsync("input, textarea");
            if (titleInput != null) { await titleInput.FillAsync(title); await BrowserManager.RandomDelayAsync(ct); }
            return true;
        }
        catch (Exception ex) { Logger.LogError(ex, "视频号元数据设置失败"); return false; }
    }

    protected override async Task<bool> DoSetCoverImageAsync(CoverImage cover, CancellationToken ct)
    {
        if (Page == null) return false;
        try
        {
            var coverInput = await Page.QuerySelectorAsync("input[type='file'][accept*='image']");
            if (coverInput != null) { await coverInput.SetInputFilesAsync(cover.FilePath); await BrowserManager.RandomDelayAsync(ct); }
            return true;
        }
        catch (Exception ex) { Logger.LogError(ex, "视频号封面设置失败"); return false; }
    }

    protected override async Task<PublishResult> DoPublishAsync(CancellationToken ct)
    {
        if (Page == null) return new PublishResult { Success = false, ErrorMessage = "页面未初始化" };
        try
        {
            var publishBtn = await Page.QuerySelectorAsync("button:has-text('发表'), button:has-text('发布')");
            if (publishBtn != null) { await publishBtn.ClickAsync(); await Page.WaitForSelectorAsync("[class*='success']", new() { Timeout = 60000 }); return new PublishResult { Success = true }; }
            return new PublishResult { Success = false, ErrorMessage = "未找到发布按钮" };
        }
        catch (Exception ex) { Logger.LogError(ex, "视频号发布失败"); return new PublishResult { Success = false, ErrorMessage = ex.Message }; }
    }
}
