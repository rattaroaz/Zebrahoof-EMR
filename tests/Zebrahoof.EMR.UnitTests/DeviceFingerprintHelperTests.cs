using System.Net;
using Microsoft.AspNetCore.Http;
using Zebrahoof_EMR.Helpers;

namespace Zebrahoof_EMR.UnitTests;

public class DeviceFingerprintHelperTests
{
    [Fact]
    public void ComputeFingerprint_ProducesStableHash_ForSameInputs()
    {
        var context = CreateHttpContext(
            userAgent: "Mozilla/5.0",
            ip: "10.0.0.1",
            forwarded: "192.168.1.10",
            deviceId: "device-123");

        var fingerprint1 = DeviceFingerprintHelper.ComputeFingerprint(context);
        var fingerprint2 = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public void ComputeFingerprint_Changes_WhenHeadersChange()
    {
        var contextA = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", "device-123");
        var contextB = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.11", "device-123");

        var fpA = DeviceFingerprintHelper.ComputeFingerprint(contextA);
        var fpB = DeviceFingerprintHelper.ComputeFingerprint(contextB);

        Assert.NotEqual(fpA, fpB);
    }

    [Fact]
    public void ComputeFingerprint_UsesConnectionIp_WhenNoForwardedHeader()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", forwarded: null, deviceId: "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_UsesFirstForwardedIp_WhenMultipleAreProvided()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "203.0.113.1, 203.0.113.2", "device-123");
        var contextSecond = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "203.0.113.2, 203.0.113.1", "device-123");

        var primary = DeviceFingerprintHelper.ComputeFingerprint(context);
        var secondary = DeviceFingerprintHelper.ComputeFingerprint(contextSecond);

        Assert.NotEqual(primary, secondary);
    }

    [Fact]
    public void ComputeFingerprint_SupportsIPv6Addresses()
    {
        var context = CreateHttpContext("Mozilla/5.0", "2001:db8::1", "2001:db8::2", "device-123");
        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrEmpty(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesMissingUserAgent()
    {
        var context = CreateHttpContext(userAgent: "", ip: "10.0.0.1", forwarded: null, deviceId: "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesMissingDeviceId()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", deviceId: "");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesNullConnectionIp()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0";
        context.Request.Headers["X-Device-Id"] = "device-123";
        context.Connection.RemoteIpAddress = null;

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesEmptyForwardedHeader()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", forwarded: "", deviceId: "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesWhitespaceOnlyForwardedHeader()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", forwarded: "   ", deviceId: "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesSpecialCharactersInUserAgent()
    {
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
        var context = CreateHttpContext(userAgent, "10.0.0.1", "192.168.1.10", "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_HandlesSpecialCharactersInDeviceId()
    {
        var deviceId = "device-123!@#$%^&*()_+-={}[]|\\:;\"'<>?,./";
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", deviceId);

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_ProducesDifferentHashesForDifferentIPs()
    {
        var context1 = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", "device-123");
        var context2 = CreateHttpContext("Mozilla/5.0", "10.0.0.2", "192.168.1.11", "device-123");

        var fp1 = DeviceFingerprintHelper.ComputeFingerprint(context1);
        var fp2 = DeviceFingerprintHelper.ComputeFingerprint(context2);

        Assert.NotEqual(fp1, fp2);
    }

    [Fact]
    public void ComputeFingerprint_ProducesDifferentHashesForDifferentUserAgents()
    {
        var context1 = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", "device-123");
        var context2 = CreateHttpContext("Chrome/91.0", "10.0.0.1", "192.168.1.10", "device-123");

        var fp1 = DeviceFingerprintHelper.ComputeFingerprint(context1);
        var fp2 = DeviceFingerprintHelper.ComputeFingerprint(context2);

        Assert.NotEqual(fp1, fp2);
    }

    [Fact]
    public void ComputeFingerprint_HandlesForwardedHeaderWithSpaces()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", " 192.168.1.10 , 192.168.1.11 ", "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
    }

    [Fact]
    public void ComputeFingerprint_ReturnsBase64String()
    {
        var context = CreateHttpContext("Mozilla/5.0", "10.0.0.1", "192.168.1.10", "device-123");

        var fingerprint = DeviceFingerprintHelper.ComputeFingerprint(context);

        Assert.True(Convert.TryFromBase64String(fingerprint, new Span<byte>(new byte[fingerprint.Length]), out _));
    }

    private static DefaultHttpContext CreateHttpContext(string userAgent, string ip, string? forwarded, string deviceId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = userAgent;
        if (!string.IsNullOrEmpty(forwarded))
        {
            context.Request.Headers["X-Forwarded-For"] = forwarded;
        }
        context.Request.Headers["X-Device-Id"] = deviceId;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }
}
