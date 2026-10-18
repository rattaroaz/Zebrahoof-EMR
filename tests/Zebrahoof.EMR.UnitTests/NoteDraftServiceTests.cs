using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class NoteDraftServiceTests
{
    [Fact]
    public void Draft_IsRestoredForTheSameUserAndPatient()
    {
        var drafts = new NoteDraftService();
        drafts.Save("physician", "browser-a", 2, new NoteDraft
        {
            ChiefComplaint = "R5-DRAFT",
            HistoryOfPresentIllness = "unsaved text"
        });

        var restored = drafts.Get("physician", "browser-a", 2);

        Assert.NotNull(restored);
        Assert.Equal("R5-DRAFT", restored.ChiefComplaint);
        Assert.Equal("unsaved text", restored.HistoryOfPresentIllness);
        Assert.Null(drafts.Get("nurse", "browser-a", 2));
        Assert.Null(drafts.Get("physician", "browser-a", 1));
        Assert.Null(drafts.Get("physician", "browser-b", 2));

        drafts.Clear("physician", "browser-a", 2);
        Assert.Null(drafts.Get("physician", "browser-a", 2));
    }

    [Fact]
    public void Draft_ExpiresAfterTwentyFourHours()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var drafts = new NoteDraftService(clock);
        drafts.Save("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "keep" });

        clock.UtcNow = clock.UtcNow.AddHours(24).AddTicks(-1);
        Assert.Equal("keep", drafts.Get("physician", "browser-a", 2)!.ChiefComplaint);

        clock.UtcNow = clock.UtcNow.AddTicks(1);
        Assert.Null(drafts.Get("physician", "browser-a", 2));
    }

    [Fact]
    public void ExplicitSignOut_RejectsOnlyTheBrowserThatLoggedOut()
    {
        var drafts = new NoteDraftService();
        var epochA = drafts.CurrentEpoch("browser-a");
        var epochB = drafts.CurrentEpoch("browser-b");
        Assert.True(drafts.TrySave("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "open-a" }, epochA));
        Assert.True(drafts.TrySave("physician", "browser-b", 2, new NoteDraft { ChiefComplaint = "open-b" }, epochB));

        drafts.Revoke("browser-a");

        Assert.False(drafts.TrySave("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "written back" }, epochA));
        Assert.Null(drafts.Get("physician", "browser-a", 2));
        Assert.True(drafts.TrySave("physician", "browser-b", 2, new NoteDraft { ChiefComplaint = "still" }, epochB));
        Assert.Equal("still", drafts.Get("physician", "browser-b", 2)!.ChiefComplaint);

        Assert.True(drafts.TrySave("physician", "browser-a-next", 2, new NoteDraft { ChiefComplaint = "after" }, drafts.CurrentEpoch("browser-a-next")));
        Assert.Equal("after", drafts.Get("physician", "browser-a-next", 2)!.ChiefComplaint);
        Assert.Null(drafts.Get("physician", "browser-a", 2));
    }

    [Fact]
    public void Sweep_RemovesDraftsOlderThanTwentyFourHours()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        var drafts = new NoteDraftService(clock);
        drafts.Save("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "old" });
        clock.UtcNow = clock.UtcNow.AddHours(24);
        drafts.Save("nurse", "browser-n", 3, new NoteDraft { ChiefComplaint = "fresh" });

        Assert.Equal(1, drafts.Sweep());
        Assert.Null(drafts.Get("physician", "browser-a", 2));
        Assert.Equal("fresh", drafts.Get("nurse", "browser-n", 3)!.ChiefComplaint);
    }

    private sealed class MutableClock : TimeProvider
    {
        public MutableClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
