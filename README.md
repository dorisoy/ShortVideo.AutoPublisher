# ShortVideo.AutoPublisher

ShortVideo.AutoPublisher 是套基于 WPF 实现的，抖音，百家号，小红书，视频号，头条，等平台短视频自动发布的创作者工具库，你可以根据本示例创建自己的短视频自动发布管理系统。

## 功能

1. 基于 CefSharp 实现的浏览器模拟操作，优化了任务链，不阻塞，优雅的重启并释放资源，使用定时重启新应用。
2. 高效稳定的文件流下载，最大 1G 文件大小， 支持监控文件下载进度和失败时记录到系统日志。
3. 支持抖音，百家号，小红书，视频号，头条个平台的商品封面（横版，竖版）自定义上传和裁切。
4. 支持网络状态检查（网络抖动是失败重试机制），浏览器页面加载状态检测，异步延迟等待时间阈值自定义配置。
5. 支持 Cookie 会话授权记录管理，状态自动刷新。

## 快速上手

```
    var v = new VideoModel
    {
        url = "你的视频地址",
        coverUrl = "视频封面",
        title = "2023数博会征集百名专业嘉宾参与“数博发布”活动",
        newTitle = "2023数博会征集百名专业嘉宾参与“数博发布”活动",
        Id = "你的视频ID",
        subject = "#2023数博会 #数博发布 #百名专业嘉宾",
        cookie = "你事先获取到的cookie"
    };
    //下载视频，封面
    await _downloader.DownloadToFileAsync(v.url, path, $"{v.Id}.mp4", CancellationToken.None);
    await _downloader.DownloadToFileAsync(v.coverUrl, path, $"{v.Id}.jpg", CancellationToken.None);
    //发布任务
    new XiaoHongShu(this, v).ShowDialog();
```

## 日志

<img src="Console.png" />

## 百家号

<img src="Baijiahao.png" width="500px"/>

## 抖音

<img src="Douyin.png" width="500px"/>

## 视频号

<img src="Shipinhao.png" width="500px"/>

## 头条

<img src="Toutiao.png" width="500px"/>

## 小红书

<img src="XiaoHongShu.png" width="500px"/>
