using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class NoteDraftServiceTests
{
    [Fact]
    public void Draft_IsRestoredForTheSameUserAndPatient()
    {
        var drafts = new NoteDraftService();
        drafts.Save("physician", 2, new NoteDraft
        {
            ChiefComplaint = "R5-DRAFT",
            HistoryOfPresentIllness = "unsaved text"
        });

        var restored = drafts.Get("physician", 2);

        Assert.NotNull(restored);
        Assert.Equal("R5-DRAFT", restored.ChiefComplaint);
        Assert.Equal("unsaved text", restored.HistoryOfPresentIllness);
        Assert.Null(drafts.Get("nurse", 2));
        Assert.Null(drafts.Get("physician", 1));

        drafts.Clear("physician", 2);
        Assert.Null(drafts.Get("physician", 2));
    }

    [Fact]
    public void Draft_ExpiresAfterTwentyFourHours()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var drafts = new NoteDraftService(clock);
        drafts.Save("physician", 2, new NoteDraft { ChiefComplaint = "keep" });

        clock.UtcNow = clock.UtcNow.AddHours(24).AddTicks(-1);
        Assert.Equal("keep", drafts.Get("physician", 2)!.ChiefComplaint);

        clock.UtcNow = clock.UtcNow.AddTicks(1);
        Assert.Null(drafts.Get("physician", 2));
    }

    private sealed class MutableClock : TimeProvider
    {
        public MutableClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
