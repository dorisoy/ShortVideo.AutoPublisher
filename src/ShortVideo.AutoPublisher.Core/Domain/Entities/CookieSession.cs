using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Domain.Entities;

/// <summary>
/// Cookie会话实体
/// </summary>
public class CookieSession
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 平台类型
    /// </summary>
    public PlatformType Platform { get; set; }

    /// <summary>
    /// 账号名称
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Cookie数据（JSON格式）
    /// </summary>
    public string CookieData { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认账号
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 最后验证时间
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 获取平台显示名称
    /// </summary>
    public string PlatformDisplayName => Platform.GetDisplayName();

    /// <summary>
    /// 判断是否已过期
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now;

    /// <summary>
    /// 获取状态描述
    /// </summary>
    public string StatusDescription
    {
        get
        {
            if (!IsValid) return "无效";
            if (IsExpired) return "已过期";
            return "有效";
        }
    }
}
