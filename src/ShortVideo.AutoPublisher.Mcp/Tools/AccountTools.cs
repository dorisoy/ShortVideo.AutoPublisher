using System.ComponentModel;
using ModelContextProtocol.Server;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;

namespace ShortVideo.AutoPublisher.Mcp.Tools;

/// <summary>
/// 账号相关 MCP 工具
/// </summary>
[McpServerToolType]
public class AccountTools
{
    private readonly CookieSessionService _sessionService;

    public AccountTools(CookieSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// 获取平台账号列表
    /// </summary>
    [McpServerTool, Description("获取所有平台账号的列表")]
    public async Task<object> list_accounts()
    {
        var sessions = await _sessionService.GetAllAsync();
        return sessions.Select(s => new
        {
            s.Id,
            Platform = s.Platform.GetDisplayName(),
            PlatformCode = s.Platform.ToString().ToLower(),
            s.AccountName,
            s.IsDefault,
            s.IsValid,
            UpdatedAt = s.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
    }

    /// <summary>
    /// 添加平台账号
    /// </summary>
    [McpServerTool, Description("添加平台账号")]
    public async Task<object> add_account(
        [Description("平台代码 (douyin/xiaohongshu/baijiahao/weixin/toutiao)")] string platform,
        [Description("账号名称")] string account_name,
        [Description("Cookie数据")] string cookie_data,
        [Description("是否设为默认账号")] bool is_default = true)
    {
        var platformType = ParsePlatform(platform);
        if (platformType == null)
        {
            return new { success = false, error = $"无效的平台: {platform}。支持: douyin, xiaohongshu, baijiahao, weixin, toutiao" };
        }

        try
        {
            var session = await _sessionService.AddAsync(platformType.Value, account_name, cookie_data, is_default);
            return new
            {
                success = true,
                account = new
                {
                    session.Id,
                    Platform = session.Platform.GetDisplayName(),
                    session.AccountName,
                    session.IsDefault
                }
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 删除平台账号
    /// </summary>
    [McpServerTool, Description("删除指定的平台账号")]
    public async Task<object> delete_account([Description("账号ID")] long account_id)
    {
        try
        {
            await _sessionService.DeleteAsync(account_id);
            return new { success = true, message = "账号已删除" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 验证账号状态
    /// </summary>
    [McpServerTool, Description("验证账号的Cookie是否有效")]
    public async Task<object> validate_account([Description("账号ID")] long account_id)
    {
        try
        {
            await _sessionService.RefreshStatusAsync(account_id);
            return new { success = true, message = "账号验证通过" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 设置默认账号
    /// </summary>
    [McpServerTool, Description("将指定账号设为该平台的默认账号")]
    public async Task<object> set_default_account([Description("账号ID")] long account_id)
    {
        try
        {
            await _sessionService.SetDefaultAsync(account_id);
            return new { success = true, message = "已设为默认账号" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    private static PlatformType? ParsePlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "douyin" => PlatformType.Douyin,
            "xiaohongshu" => PlatformType.Xiaohongshu,
            "baijiahao" => PlatformType.Baijiahao,
            "weixin" => PlatformType.WeixinChannel,
            "toutiao" => PlatformType.Toutiao,
            _ => null
        };
    }
}
