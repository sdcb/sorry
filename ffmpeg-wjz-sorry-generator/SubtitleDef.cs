namespace Sorry;

public record SubtitleDef(double StartTS, double EndTS, string RefText)
{
    public double Duration => EndTS - StartTS;
    public bool WillShowInTS(double ts) => StartTS <= ts && ts < EndTS;
}
