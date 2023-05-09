# ShortVideo.AutoPublisher

ShortVideo.AutoPublisher 是套实现，抖音，百家号，小红书，视频号，头条，等平台短视频自动发布的创作者工具。

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

## 接入示例

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
