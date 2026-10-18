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
}
