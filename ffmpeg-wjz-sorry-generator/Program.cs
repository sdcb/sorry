using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Gradio.Net;
using Sdcb.FFmpeg.Toolboxs.Extensions;

namespace Sorry;

public static partial class Program
{
    static void Main()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddGradio();
        WebApplication webApplication = builder.Build();
        webApplication.UseGradio(CreateBlocks());
        webApplication.Run();
    }

    static Blocks CreateBlocks()
    {
        using Blocks blocks = gr.Blocks();


        gr.Markdown("# 视频模板、字幕，生成视频gif");
        Textbox template;
        Button loadSubtitleButton;
        using (gr.Row())
        {
            template = gr.Textbox("真香", interactive: true, label: $"允许的模板值：{string.Join("|", Mp4SourceDef.All.Select(x => x.Title))}");
            loadSubtitleButton = gr.Button("加载字幕");
        }

        Image image;
        Textbox subtitle;
        using (gr.Row())
        {
            subtitle = gr.Textbox(Mp4SourceDef.Xiang.CombinedText, lines: 8, label: "输入字幕");
            image = gr.Image(interactive: false);
        }
        gr.Button("生成视频").Click(i =>
        {
            string template = i.Data[0].ToString()!;
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"模板{template}错误，请输入 {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} 之一");
            string subtitle = i.Data[1].ToString()!;

            byte[] gif = def.CreateLines(subtitle.Split(Environment.NewLine)).DecodeAddSubtitle();
            string path = Path.GetTempFileName();
            File.WriteAllBytes(path, gif);
            return Task.FromResult(gr.Output(path));
        }, [template, subtitle], [image]);

        loadSubtitleButton.Click(i =>
        {
            string template = i.Data[0].ToString()!;
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"模板{template}错误，请输入 {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} 之一");
            return Task.FromResult(gr.Output(def.CombinedText));
        }, [template], [subtitle]);

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
