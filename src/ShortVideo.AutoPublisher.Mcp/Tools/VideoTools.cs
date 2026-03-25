using System.ComponentModel;
using ModelContextProtocol.Server;
using ShortVideo.AutoPublisher.Services;

namespace ShortVideo.AutoPublisher.Mcp.Tools;

/// <summary>
/// 视频相关 MCP 工具
/// </summary>
[McpServerToolType]
public class VideoTools
{
    private readonly VideoContentService _videoService;

    public VideoTools(VideoContentService videoService)
    {
        _videoService = videoService;
    }

    /// <summary>
    /// 获取视频库列表
    /// </summary>
    [McpServerTool, Description("获取视频库中所有视频的列表")]
    public async Task<object> list_videos()
    {
        var videos = await _videoService.GetAllAsync();
        return videos.Select(v => new
        {
            v.Id,
            v.Title,
            v.Description,
            v.Tags,
            v.FilePath,
            FileSize = v.FormattedFileSize,
            CreatedAt = v.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
    }

    /// <summary>
    /// 添加视频到库
    /// </summary>
    [McpServerTool, Description("添加视频文件到视频库")]
    public async Task<object> add_video(
        [Description("视频文件路径")] string file_path,
        [Description("视频标题")] string title,
        [Description("视频描述（可选）")] string? description = null,
        [Description("标签，逗号分隔（可选）")] string? tags = null)
    {
        if (!File.Exists(file_path))
        {
            return new { success = false, error = $"文件不存在: {file_path}" };
        }

        try
        {
            var video = await _videoService.AddAsync(file_path, title, description, tags);
            return new
            {
                success = true,
                video = new
                {
                    video.Id,
                    video.Title,
                    video.FilePath,
                    FileSize = video.FormattedFileSize
                }
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 获取视频详情
    /// </summary>
    [McpServerTool, Description("获取指定视频的详细信息")]
    public async Task<object> get_video([Description("视频ID")] long video_id)
    {
        var video = await _videoService.GetByIdAsync(video_id);
        if (video == null)
        {
            return new { success = false, error = $"视频不存在 (ID: {video_id})" };
        }

        return new
        {
            success = true,
            video = new
            {
                video.Id,
                video.Title,
                video.Description,
                video.Tags,
                video.FilePath,
                FileSize = video.FormattedFileSize,
                CreatedAt = video.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
    }

    /// <summary>
    /// 删除视频
    /// </summary>
    [McpServerTool, Description("从视频库删除指定视频")]
    public async Task<object> delete_video([Description("视频ID")] long video_id)
    {
        try
        {
            var video = await _videoService.GetByIdAsync(video_id);
            if (video == null)
            {
                return new { success = false, error = $"视频不存在 (ID: {video_id})" };
            }

            await _videoService.DeleteAsync(video_id);
            return new { success = true, message = "视频已删除" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 搜索视频
    /// </summary>
    [McpServerTool, Description("根据关键字搜索视频")]
    public async Task<object> search_videos([Description("搜索关键字")] string keyword)
    {
        var videos = await _videoService.SearchAsync(keyword);
        return videos.Select(v => new
        {
            v.Id,
            v.Title,
            v.Tags,
            FileSize = v.FormattedFileSize
        }).ToList();
    }
}
