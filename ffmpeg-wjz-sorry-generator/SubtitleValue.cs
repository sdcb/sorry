namespace Sorry;

public record SubtitleValue(string? Font, float? FontSize, string Text)
{
    public static SubtitleValue CreateDefault(string text) => new(null, null, text);
}
