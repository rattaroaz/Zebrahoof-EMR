using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class AiSessionStateServiceTests
{
    [Fact]
    public void DocumentAndRecordFlags_RoundTripPerPatient()
    {
        var state = new AiSessionStateService();

        Assert.False(state.HaveDocumentsBeenSent(1));
        state.MarkDocumentsSent(1);
        Assert.True(state.HaveDocumentsBeenSent(1));
        Assert.False(state.HaveDocumentsBeenSent(2));

        state.MarkRecordsUpdated(1);
        Assert.True(state.HaveRecordsBeenUpdated(1));
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
        state.ResetDocumentsSent(9);
        Assert.False(state.HaveDocumentsBeenSent(9));
    }
}
