namespace Sorry;

public record struct VideoTime(int Frame, TimeSpan Elapsed, int TotalFrame)
{
    public readonly float Percent => 1.0f * Frame / TotalFrame;
}
