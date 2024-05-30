namespace Sorry;

public record Mp4SourceDef(string Title, string Mp4Url, SubtitleDef[] SubtitleDefs)
{
    public IEnumerable<SubtitleDef> GetSubtitlesDefOnTS(float ts) => SubtitleDefs.Where(x => x.WillShowInTS(ts));

    public Mp4Source CreateDefault() => new(Mp4Url, SubtitleDefs.Select(x => new Subtitle(x, SubtitleValue.CreateDefault(x.RefText))).ToArray());

    public Mp4Source CreateLines(params string[] lines) => new(Mp4Url, SubtitleDefs.Zip(lines).Select(x => new Subtitle(x.First, SubtitleValue.CreateDefault(x.Second))).ToArray());

    public string CombinedText => string.Join(Environment.NewLine, SubtitleDefs.Select(x => x.RefText));

    public static readonly Mp4SourceDef Xiang = new("真香", "https://io.starworks.cc:88/cv-public/2022/gif-wjz.mp4",
    [
        new SubtitleDef(0, 1.04, "还愣着干嘛"),
        new SubtitleDef(1.46, 2.9, "上页面显示"),
        new SubtitleDef(3.09, 4.33, "上报错日志"),
        new SubtitleDef(4.59, 5.93, "你找别人吧，我不会"),
    ]);

    public static readonly Mp4SourceDef Sorry = new("Sorry", "https://io.starworks.cc:88/cv-public/2022/gif-sorry.mp4",
    [
        new SubtitleDef(1.18, 1.56, "好啊"),
        new SubtitleDef(3.18, 4.43, "就算你是一流程序员"),
        new SubtitleDef(5.31, 7.43, "就算你代码再完美"),
        new SubtitleDef(7.56, 9.93, "毕竟我是产品"),
        new SubtitleDef(10.06, 11.56, "我叫你改需求你就要改"),
        new SubtitleDef(11.93, 13.06, "产品了不起啊"),
        new SubtitleDef(13.81, 16.31, "sorry 产品真的也不起"),
        new SubtitleDef(18.06, 19.56, "以后天天让他改需求"),
        new SubtitleDef(19.60, 21.60, "哈哈，天天改"),
    ]);

    public static readonly Mp4SourceDef Nlgl = new("哪里贵了", "https://io.starworks.cc:88/cv-public/2023/nlgl-mini.mp4",
    [
        new SubtitleDef(0.333, 1.399, "Java 8越来越不行了？"),
        new SubtitleDef(1.400, 2.333, "哪里不行了？"),
        new SubtitleDef(2.766, 4.733, "这么多年来大家都用Java 8"),
        new SubtitleDef(4.733, 6.166, "不要睁着眼睛乱说"),
        new SubtitleDef(6.166, 8.966, "我们Java程序员很不容易的哦"),
        new SubtitleDef(8.966, 10.199, "而且Java真的不是那种"),
        new SubtitleDef(10.200, 11.700, "只能写写Hello World的语言"),
        new SubtitleDef(11.700, 13.966, "哎，我写Java多少年了"),
        new SubtitleDef(13.966, 15.899, "它怎么牛逼我是最知道的"),
        new SubtitleDef(15.900, 16.700, "是啊"),
        new SubtitleDef(17.100, 18.966, "C#就语法好点"),
        new SubtitleDef(19.100, 19.900, "哈哈好不好"),
        new SubtitleDef(19.900, 20.600, "真的乱说"),
        new SubtitleDef(20.600, 22.266, "这么多年都是Java 8"),
        new SubtitleDef(22.266, 23.333, "哪里落后了"),
        new SubtitleDef(23.366, 24.899, "有的时候找找自己原因好吧"),
        new SubtitleDef(24.900, 25.333, "这么多年了"),
        new SubtitleDef(25.333, 25.999, "工资涨没涨"),
        new SubtitleDef(26.000, 28.200, "有没有认真工作好不好"),
        new SubtitleDef(28.800, 29.966, "这么多年都用的Java 8"),
        new SubtitleDef(29.966, 30.899, "我真的快疯掉了"),
    ]);

    public static Mp4SourceDef[] All => [Xiang, Sorry, Nlgl];
}
