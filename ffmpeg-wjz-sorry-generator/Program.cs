using Amazon.S3;
using Amazon.S3.Model;
using Gradio.Net;
using System.Net;
using System.Text.RegularExpressions;

namespace Sorry;

public static partial class Program
{
    static void Main()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGradio();
        WebApplication webApplication = builder.Build();
        webApplication.UseGradio(CreateBlocks(webApplication.Services));
        webApplication.Run();
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    public static partial Regex LineSpliter();

    static Blocks CreateBlocks(IServiceProvider sp)
    {
        using Blocks blocks = gr.Blocks();


        gr.Markdown("# 视频模板、字幕，生成视频gif");
        Radio template = gr.Radio(Mp4SourceDef.All.Select(x => x.Title).ToArray(), label: "选择模板", value: Mp4SourceDef.All.First().Title);

        Image image;
        Textbox subtitle;
        using (gr.Row())
        {
            subtitle = gr.Textbox(Mp4SourceDef.Xiang.CombinedText, lines: 8, label: "输入字幕");
            image = gr.Image(interactive: false);
        }
        gr.Button("生成视频").Click(async i =>
        {
            string template = Radio.Payload(i.Data[0]).Single();
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"模板{template}错误，请输入 {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} 之一");
            string subtitle = i.Data[1].ToString()!;

            byte[] gif = def.CreateLines(LineSpliter().Split(subtitle)).DecodeAddSubtitle();
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            File.WriteAllBytes(Path.Combine(desktop, "output.gif"), gif);

            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            string path = null!;
            if (config.GetValue<bool>("S3:Enabled"))
            {
                string accessKey = config["S3:AccessKey"] ?? throw new Exception("S3:AccessKey is required");
                string secret = config["S3:SecretKey"] ?? throw new Exception("S3:SecretKey is required");
                string bucketName = config["S3:BucketName"] ?? throw new Exception("S3:BucketName is required");
                string serviceUrl = config["S3:ServiceUrl"] ?? throw new Exception("S3:ServiceUrl is required");

                using AmazonS3Client s3 = new(accessKey, secret, new AmazonS3Config
                {
                    ForcePathStyle = true,
                    ServiceURL = serviceUrl,
                });
                string objectKey = $"{DateTime.Now:yyyy/MM/dd}/{template}-{Guid.NewGuid()}.gif";
                PutObjectResponse resp = await s3.PutObjectAsync(new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = new MemoryStream(gif),
                    ContentType = "image/gif",
                });
                if (resp.HttpStatusCode != HttpStatusCode.OK) throw new Exception($"上传失败 {resp.HttpStatusCode}");

                string downloadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Expires = DateTime.UtcNow.AddHours(1),
                });
                path = downloadUrl;
            }
            else
            {
                path = Path.GetTempFileName();
                File.WriteAllBytes(path, gif);
            }

            return gr.Output(path);
        }, inputs: [template, subtitle], outputs: [image]);

        template.Select(i =>
        {
            string template = Radio.Payload(i.Data[0]).Single();
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"模板{template}错误，请输入 {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} 之一");
            return Task.FromResult(gr.Output(def.CombinedText));
        }, inputs: [template], outputs: [subtitle]);

        using (gr.Row())
        {
            gr.Markdown("""
		    ## Github: 
		    * https://github.com/sdcb/sorry
		    * https://github.com/sdcb/Sdcb.FFmpeg
		    """);

            gr.Markdown("""		
		    ## QQ: 495782587
		    """);
        }

        return blocks;
    }
}
