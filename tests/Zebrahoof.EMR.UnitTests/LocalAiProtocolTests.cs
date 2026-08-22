using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class LocalAiProtocolTests
{
    [Fact]
    public void BuildMessages_IncludesSystemHistoryAndUser()
    {
        var history = new[]
        {
            new ChatTurn("first question", "first answer"),
            new ChatTurn("second question", "")
        };

        var messages = LocalAiProtocol.BuildMessages("sys", history, "latest");

        Assert.Equal(5, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Equal("sys", messages[0].Content);
        Assert.Equal("user", messages[1].Role);
        Assert.Equal("first question", messages[1].Content);
        Assert.Equal("assistant", messages[2].Role);
        Assert.Equal("first answer", messages[2].Content);
        Assert.Equal("user", messages[3].Role);
        Assert.Equal("second question", messages[3].Content);
        Assert.Equal("user", messages[4].Role);
        Assert.Equal("latest", messages[4].Content);
    }

    [Fact]
    public void ExtractAssistantText_ReadsOllamaNativeMessage()
    {
        const string json = """{"model":"qwen2.5:7b","message":{"role":"assistant","content":"Hello clinician"},"done":true}""";

        var text = LocalAiProtocol.ExtractAssistantText(json);

        Assert.Equal("Hello clinician", text);
    }

    [Fact]
    public void ExtractAssistantText_ReadsOpenAiCompatibleChoices()
    {
        const string json = """{"choices":[{"message":{"role":"assistant","content":"From choices"}}]}""";

        var text = LocalAiProtocol.ExtractAssistantText(json);

        Assert.Equal("From choices", text);
    }

    [Fact]
    public void ExtractAssistantText_ReturnsNullWhenEmpty()
    {
        Assert.Null(LocalAiProtocol.ExtractAssistantText("""{"done":true}"""));
    }

    [Fact]
    public void ExtractError_ReadsErrorField()
    {
        var error = LocalAiProtocol.ExtractError("""{"error":"model not found"}""");
        Assert.Equal("model not found", error);
    }
}
