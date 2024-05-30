using Gradio.Net;

namespace Sorry;

public static partial class Program
{
    static void Main()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddGradio();
        WebApplication webApplication = builder.Build();
        webApplication.UseGradio(CreateBlocks());
        webApplication.Run();
    }

    static Blocks CreateBlocks()
    {
        using Blocks blocks = gr.Blocks();


        gr.Markdown("# ��Ƶģ�塢��Ļ��������Ƶgif");
        Textbox template;
        Button loadSubtitleButton;
        using (gr.Row())
        {
            template = gr.Textbox("����", interactive: true, label: $"�����ģ��ֵ��{string.Join("|", Mp4SourceDef.All.Select(x => x.Title))}");
            loadSubtitleButton = gr.Button("������Ļ");
        }

        Image image;
        Textbox subtitle;
        using (gr.Row())
        {
            subtitle = gr.Textbox(Mp4SourceDef.Xiang.CombinedText, lines: 8, label: "������Ļ");
            image = gr.Image(interactive: false);
        }
        gr.Button("������Ƶ").Click(async i =>
        {
            string template = i.Data[0].ToString()!;
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"ģ��{template}���������� {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} ֮һ");
            string subtitle = i.Data[1].ToString()!;

            byte[] gif = def.CreateLines(subtitle.Split(Environment.NewLine)).DecodeAddSubtitle();
            string path = Path.GetTempFileName();
            File.WriteAllBytes(path, gif);
            return gr.Output(path);
        }, [template, subtitle], [image]);

        loadSubtitleButton.Click(async i =>
        {
            string template = i.Data[0].ToString()!;
            Mp4SourceDef? def = Mp4SourceDef.All.FirstOrDefault(x => x.Title == template);
            if (def == null) throw new Exception($"ģ��{template}���������� {string.Join("|", Mp4SourceDef.All.Select(x => x.Title))} ֮һ");
            return gr.Output(def.CombinedText);
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
