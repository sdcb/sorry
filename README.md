# ffmpeg-wjz-sorry-generator

### 体验网站：
https://ffmpeg-sorry-demo.starworks.cc:88/

### 要点：
* 视频解码
* 将每一帧转换为BGRA像素格式
* 使用Direct2D读取并绘制字幕
* 将每一帧输入视频过滤器，转换为PAL8格式
* 将PAL8编码像素格式的帧编码为gif

示例：
https://ffmpeg-sorry-demo.starworks.cc:88/sorry/generate?type=wjz&subtitle=%E8%BF%98%E6%84%A3%E7%9D%80%E5%B9%B2%E5%98%9B|%E4%B8%8A%E9%A1%B5%E9%9D%A2%E6%98%BE%E7%A4%BA|%E4%B8%8A%E6%8A%A5%E9%94%99%E6%97%A5%E5%BF%97|%E4%BD%A0%E6%89%BE%E5%88%AB%E4%BA%BA%E5%90%A7%EF%BC%8C%E6%88%91%E4%B8%8D%E4%BC%9A

### 依赖
* Sdcb.FFmpeg （使用纯C API平台调用生成，不是命令行）： https://github.com/sdcb/sdcb.ffmpeg
* [Vortice.Windows] (https://github.com/amerkoleci/Vortice.Windows)
