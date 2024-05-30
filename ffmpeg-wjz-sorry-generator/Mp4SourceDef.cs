namespace Sorry;

public record Mp4SourceDef(string Title, string Mp4Url, SubtitleDef[] SubtitleDefs)
{
    public IEnumerable<SubtitleDef> GetSubtitlesDefOnTS(float ts) => SubtitleDefs.Where(x => x.WillShowInTS(ts));

    public Mp4Source CreateDefault() => new(Mp4Url, SubtitleDefs.Select(x => new Subtitle(x, SubtitleValue.CreateDefault(x.RefText))).ToArray());

    public Mp4Source CreateLines(params string[] lines) => new(Mp4Url, SubtitleDefs.Zip(lines).Select(x => new Subtitle(x.First, SubtitleValue.CreateDefault(x.Second))).ToArray());

    public string CombinedText => string.Join(Environment.NewLine, SubtitleDefs.Select(x => x.RefText));

    public static readonly Mp4SourceDef WangJingZe = new("真香", "https://io.starworks.cc:88/cv-public/2022/gif-wjz.mp4",
    [
        new SubtitleDef(0, 1.04, "我王境泽就是饿死"),
        new SubtitleDef(1.46, 2.9, "死外面 从这里跳下去"),
        new SubtitleDef(3.09, 4.33, "也不会吃你们一点东西的"),
        new SubtitleDef(4.59, 5.93, "真香~"),
    ]);

    public static readonly Mp4SourceDef Sorry = new("Sorry", "https://io.starworks.cc:88/cv-public/2022/gif-sorry.mp4",
    [
        new SubtitleDef(1.18, 1.56, "好啊"),
        new SubtitleDef(3.18, 4.43, "别说我是一等良民"),
        new SubtitleDef(5.31, 7.43, "就算你真的想要诬告我"),
        new SubtitleDef(7.56, 9.93, "我有的是钱找律师帮我打官司"),
        new SubtitleDef(10.06, 11.56, "我想我根本不用坐牢了"),
        new SubtitleDef(11.93, 13.06, "你别以为有钱了不起啊"),
        new SubtitleDef(13.81, 16.31, "Sorry"),
        new SubtitleDef(18.06, 19.56, "有钱真的了不起"),
        new SubtitleDef(19.60, 21.60, "不过我想你不会明白这种感觉"),
    ]);

    public static readonly Mp4SourceDef Nlgl = new("哪里贵了", "https://io.starworks.cc:88/cv-public/2023/nlgl-mini.mp4",
    [
        new SubtitleDef(0.333, 1.399, "花西子越来越贵了"),
        new SubtitleDef(1.400, 2.333, "哪里贵了"),
        new SubtitleDef(2.766, 4.733, "这么多年都是这个价格好吧"),
        new SubtitleDef(4.733, 6.166, "不要睁着眼睛乱说"),
        new SubtitleDef(6.166, 8.966, "国货品牌很难的哦"),
        new SubtitleDef(8.966, 10.199, "而且花西子真的不是那种"),
        new SubtitleDef(10.200, 11.700, "随便买原料就做的品牌"),
        new SubtitleDef(11.700, 13.966, "哎我跟花西子跟了多少年"),
        new SubtitleDef(13.966, 15.899, "他怎么起来的我是最知道的一个人"),
        new SubtitleDef(15.900, 16.700, "是啊"),
        new SubtitleDef(17.100, 18.966, "他们就差点把他们家掏给我了"),
        new SubtitleDef(19.100, 19.900, "哈哈好不好"),
        new SubtitleDef(19.900, 20.600, "真的乱说"),
        new SubtitleDef(20.600, 22.266, "这么多年都是79块钱"),
        new SubtitleDef(22.266, 23.333, "哪里贵了"),
        new SubtitleDef(23.366, 24.899, "有的时候找找自己原因好吧"),
        new SubtitleDef(24.900, 25.333, "这么多年了"),
        new SubtitleDef(25.333, 25.999, "工资涨没涨"),
        new SubtitleDef(26.000, 28.200, "有没有认真工作好不好"),
        new SubtitleDef(28.800, 29.966, "这么多年都是这个价格"),
        new SubtitleDef(29.966, 30.899, "我真的快疯掉了"),
    ]);

    public static Mp4SourceDef[] All => [WangJingZe, Sorry, Nlgl];
}
