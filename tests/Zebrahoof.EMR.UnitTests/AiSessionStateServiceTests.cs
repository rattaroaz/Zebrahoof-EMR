using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class AiSessionStateServiceTests
{
    [Fact]
    public void RecordFlags_RoundTripPerPatient()
    {
        var state = new AiSessionStateService();

        Assert.False(state.HaveRecordsBeenUpdated(1));
        state.MarkRecordsUpdated(1);
        Assert.True(state.HaveRecordsBeenUpdated(1));
        Assert.False(state.HaveRecordsBeenUpdated(2));

        state.ResetRecordsUpdated(1);
        Assert.False(state.HaveRecordsBeenUpdated(1));
    }

    [Fact]
    public void UpdateDocumentCount_ReturnsTrueOnlyWhenCountGrows()
    {
        var state = new AiSessionStateService();

        Assert.True(state.UpdateDocumentCount(9, 1));
        Assert.False(state.UpdateDocumentCount(9, 1));
        Assert.True(state.UpdateDocumentCount(9, 3));
    }
}
