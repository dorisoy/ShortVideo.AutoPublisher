# ShortVideo.AutoPublisher

ShortVideo.AutoPublisher 是套基于 WPF 实现的，[抖音](https://creator.douyin.com)，[百家号](https://baijiahao.baidu.com/builder/rc/edit?type=videoV2)，[小红书](https://creator.xiaohongshu.com)，[视频号](https://channels.weixin.qq.com)，[头条](https://mp.toutiao.com/profile_v4/xigua/upload-video)，等平台短视频自动发布的创作者工具库，你可以根据本示例创建自己的短视频自动发布管理系统。

下载地址：[http://shop.unitos.cn/item/8](http://shop.unitos.cn/item/8)

## 功能

1. 基于 CefSharp 实现的浏览器模拟操作，优化了任务链，不阻塞，优雅的重启并释放资源，使用定时重启新应用。
2. 高效稳定的文件流下载，最大 1G 文件大小， 支持监控文件下载进度和失败时记录到系统日志。
3. 支持抖音，百家号，小红书，视频号，头条个平台的视频封面（横版，竖版）自定义上传和裁切。
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

## 视频上传示例

```
    /// <summary>
    /// 打开文件上传选对话框
    /// </summary>
    protected Action<BaseWindow, string, string> UploadFileDialog = async (win, videoId,ext) =>
    {
        await win?.Dispatcher.InvokeAsync(() =>
        {
            //自定义对话框处理器 mp4  Download
            if (win.browser != null && !win.browser.IsDisposed)
                win.browser.DialogHandler = new DialogHandler($"{Download.path}/{videoId}.{ext}");
        });
    };
```

## 上传封面示例

```
    await e.Browser.EvaluateScriptAsync(@"
        var play = document.getElementsByClassName('uploader-inner')[0]
        function findPos(obj)
        {
            var curleft = 0;
            var curtop = 0;

            if (obj.offsetParent)
            {
                do
                {
                    curleft += obj.offsetLeft;
                    curtop += obj.offsetTop;
                } while (obj = obj.offsetParent);

                return { X: curleft,Y: curtop};
            }
        }
        findPos(play)"
    )
    .ContinueWith(async x =>
    {
        var responseForMouseClick = x.Result;
        if (responseForMouseClick.Success && responseForMouseClick.Result != null)
        {
            var xy = responseForMouseClick.Result;
            var json = JsonConvert.SerializeObject(xy).ToString();
            var coordx = json.Substring(json.IndexOf(':') + 1, 3);
            var coordy = json.Substring(json.LastIndexOf(':') + 1, 3);

            MouseLeftDown(int.Parse(coordx) + 5, int.Parse(coordy) + 5);
            MouseLeftUp(int.Parse(coordx) + 100, int.Parse(coordy) + 100);
        }
        await Task.Delay(3000);
    });
```

## 日志

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/Console.png" />

## 百家号

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/Baijiahao.png" />

## 抖音

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/Douyin.png" />

## 视频号

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/Shipinhao.png" />

## 头条

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/Toutiao.png" />

## 小红书

<img src="https://github.com/dorisoy/ShortVideo.AutoPublisher/blob/main/Screen/XiaoHongShu.png" />


## 微信扫码交流

![](https://github.com/dorisoy/Wesley/blob/main/weixing.png?raw=true)
