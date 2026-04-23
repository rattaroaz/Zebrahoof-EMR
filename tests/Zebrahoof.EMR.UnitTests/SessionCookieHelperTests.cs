using System.Text;
using Zebrahoof_EMR.Helpers;

namespace Zebrahoof_EMR.UnitTests;

public class SessionCookieHelperTests
{
    [Fact]
    public void Encode_ReturnsValidBase64String()
    {
        var sessionId = Guid.NewGuid();
        var refreshToken = "test-token";

        var encoded = SessionCookieHelper.Encode(sessionId, refreshToken);

        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
        Assert.True(Convert.TryFromBase64String(encoded, new Span<byte>(new byte[encoded.Length * 2]), out _));
    }

    [Fact]
    public void TryDecode_ValidCookie_ReturnsCorrectValues()
    {
        var sessionId = Guid.NewGuid();
        var refreshToken = "test-token";
        var encoded = SessionCookieHelper.Encode(sessionId, refreshToken);

        var result = SessionCookieHelper.TryDecode(encoded, out var decodedSessionId, out var decodedRefreshToken);

        Assert.True(result);
        Assert.Equal(sessionId, decodedSessionId);
        Assert.Equal(refreshToken, decodedRefreshToken);
    }

    [Fact]
    public void TryDecode_NullOrEmptyCookie_ReturnsFalse()
    {
        var result1 = SessionCookieHelper.TryDecode(null, out var sessionId1, out var refreshToken1);
        var result2 = SessionCookieHelper.TryDecode(string.Empty, out var sessionId2, out var refreshToken2);
        var result3 = SessionCookieHelper.TryDecode("   ", out var sessionId3, out var refreshToken3);

        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
        Assert.Equal(Guid.Empty, sessionId1);
        Assert.Equal(string.Empty, refreshToken1);
        Assert.Equal(Guid.Empty, sessionId2);
        Assert.Equal(string.Empty, refreshToken2);
        Assert.Equal(Guid.Empty, sessionId3);
        Assert.Equal(string.Empty, refreshToken3);
    }

    [Fact]
    public void TryDecode_InvalidBase64_ReturnsFalse()
    {
        var invalidBase64 = "invalid-base64-string";

        var result = SessionCookieHelper.TryDecode(invalidBase64, out var sessionId, out var refreshToken);

        Assert.False(result);
        Assert.Equal(Guid.Empty, sessionId);
        Assert.Equal(string.Empty, refreshToken);
    }

    [Fact]
    public void TryDecode_MalformedPayload_ReturnsFalse()
    {
        var sessionId = Guid.NewGuid();
        var malformedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(sessionId.ToString("N")));

        var result = SessionCookieHelper.TryDecode(malformedPayload, out var decodedSessionId, out var decodedRefreshToken);

        Assert.False(result);
        Assert.Equal(Guid.Empty, decodedSessionId);
        Assert.Equal(string.Empty, decodedRefreshToken);
    }

    [Fact]
    public void TryDecode_InvalidGuidFormat_ReturnsFalse()
    {
        var payload = "invalid-guid:token";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        var result = SessionCookieHelper.TryDecode(encoded, out var sessionId, out var refreshToken);

        Assert.False(result);
        Assert.Equal(Guid.Empty, sessionId);
        Assert.Equal(string.Empty, refreshToken);
    }

    [Fact]
    public void TryDecode_EmptyToken_ReturnsTrue()
    {
        var sessionId = Guid.NewGuid();
        var payload = $"{sessionId:N}:";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        var result = SessionCookieHelper.TryDecode(encoded, out var decodedSessionId, out var decodedRefreshToken);

        Assert.True(result);
        Assert.Equal(sessionId, decodedSessionId);
        Assert.Equal(string.Empty, decodedRefreshToken);
    }

    [Fact]
    public void TryDecode_CookieWithSpecialCharacters_ReturnsCorrectValues()
    {
        var sessionId = Guid.NewGuid();
        var refreshToken = "token-with-special-chars-!@#$%^&*()_+-={}[]|\\:;\"'<>?,./";
        var encoded = SessionCookieHelper.Encode(sessionId, refreshToken);

        var result = SessionCookieHelper.TryDecode(encoded, out var decodedSessionId, out var decodedRefreshToken);

        Assert.True(result);
        Assert.Equal(sessionId, decodedSessionId);
        Assert.Equal(refreshToken, decodedRefreshToken);
    }

    [Fact]
    public void EncodeDecode_RoundTrip_Success()
    {
        var sessionId = Guid.NewGuid();
        var refreshToken = "test-refresh-token";

        var encoded = SessionCookieHelper.Encode(sessionId, refreshToken);
        var decoded = SessionCookieHelper.TryDecode(encoded, out var decodedSessionId, out var decodedRefreshToken);

        Assert.True(decoded);
        Assert.Equal(sessionId, decodedSessionId);
        Assert.Equal(refreshToken, decodedRefreshToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("single-part")]
    [InlineData("part1:part2:part3")]
    [InlineData("::")]
    public void TryDecode_InvalidPayloadParts_ReturnsFalse(string payload)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        var result = SessionCookieHelper.TryDecode(encoded, out var sessionId, out var refreshToken);

        Assert.False(result);
        Assert.Equal(Guid.Empty, sessionId);
        Assert.Equal(string.Empty, refreshToken);
    }
}
