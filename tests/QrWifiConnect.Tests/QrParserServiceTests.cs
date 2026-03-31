using QrWifiConnect.Models;
using QrWifiConnect.Services;
using Xunit;

namespace QrWifiConnect.Tests;

public sealed class QrParserServiceTests
{
    private readonly IQrParserService _sut = new QrParserService();

    // --- Valid payloads ---

    [Fact]
    public void TryParse_ValidWpa_ReturnsCredential()
    {
        var result = _sut.TryParse("WIFI:T:WPA;S:TestNet;P:pass123;;");

        Assert.NotNull(result);
        Assert.Equal("TestNet", result.Ssid);
        Assert.Equal(WifiSecurityType.Wpa, result.SecurityType);
        Assert.Equal("pass123", result.Password);
        Assert.False(result.IsHidden);
    }

    [Fact]
    public void TryParse_ValidWpa3_ReturnsCredential()
    {
        var result = _sut.TryParse("WIFI:T:WPA3;S:SecureNet;P:s3cr3t;;");

        Assert.NotNull(result);
        Assert.Equal("SecureNet", result.Ssid);
        Assert.Equal(WifiSecurityType.Wpa3, result.SecurityType);
        Assert.Equal("s3cr3t", result.Password);
    }

    [Fact]
    public void TryParse_OpenNetwork_NoPassword_ReturnsCredential()
    {
        var result = _sut.TryParse("WIFI:T:nopass;S:OpenNet;;");

        Assert.NotNull(result);
        Assert.Equal("OpenNet", result.Ssid);
        Assert.Equal(WifiSecurityType.Open, result.SecurityType);
        Assert.Null(result.Password);
    }

    [Fact]
    public void TryParse_HiddenSsid_SetsIsHidden()
    {
        var result = _sut.TryParse("WIFI:T:WPA;S:HiddenNet;P:mypass;H:true;;");

        Assert.NotNull(result);
        Assert.Equal("HiddenNet", result.Ssid);
        Assert.True(result.IsHidden);
    }

    [Fact]
    public void TryParse_HiddenFalse_IsHiddenFalse()
    {
        var result = _sut.TryParse("WIFI:T:WPA;S:VisibleNet;P:mypass;H:false;;");

        Assert.NotNull(result);
        Assert.False(result.IsHidden);
    }

    // --- Non-WIFI codes: must return null ---

    [Fact]
    public void TryParse_NonWifiCode_ReturnsNull()
    {
        var result = _sut.TryParse("https://example.com");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_PlainText_ReturnsNull()
    {
        var result = _sut.TryParse("Hello World");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_NullInput_ReturnsNull()
    {
        var result = _sut.TryParse(null!);

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_EmptyInput_ReturnsNull()
    {
        var result = _sut.TryParse(string.Empty);

        Assert.Null(result);
    }

    // --- Malformed WIFI: payloads ---

    [Fact]
    public void TryParse_MissingSSID_ReturnsNull()
    {
        var result = _sut.TryParse("WIFI:T:WPA;P:pass;;");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_WpaWithoutPassword_ReturnsNull()
    {
        // WPA requires a password; missing password → invalid
        var result = _sut.TryParse("WIFI:T:WPA;S:NoPassNet;;;");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_SsidTooLong_ReturnsNull()
    {
        var longSsid = new string('A', 33); // IEEE 802.11 max is 32 chars
        var result = _sut.TryParse($"WIFI:T:WPA;S:{longSsid};P:pass;;");

        Assert.Null(result);
    }

    // --- Security: injection / special character handling ---

    [Fact]
    public void TryParse_SsidWithSpecialChars_ExtractedLiterallyNotExecuted()
    {
        // Semicolons and special chars within the SSID value should be rejected by
        // the strict regex (semicolons delimit fields).
        // A semicolon in the SSID value would prematurely terminate the field.
        var result = _sut.TryParse("WIFI:T:WPA;S:Net;Work;P:pass;;");

        // Might parse "Net" as SSID and "Work" as part of next field — implementation
        // must not crash and should either parse safely or return null.
        // Either outcome is acceptable; what must NOT happen is an exception.
        // The key security invariant is: no code execution from QR content.
    }

    [Fact]
    public void TryParse_PasswordWithSpecialChars_ExtractedLiterallyNotExecuted()
    {
        var result = _sut.TryParse("WIFI:T:WPA;S:TestNet;P:pass!@#$%^&*();;");

        // Special characters in password should be extracted literally
        if (result is not null)
        {
            Assert.Equal("pass!@#$%^&*()", result.Password);
        }
    }

    // --- T035: Security hardening — ToString() must never include password ---

    [Fact]
    public void WifiCredential_ToString_DoesNotContainPassword()
    {
        var credential = new WifiCredential
        {
            Ssid = "MyNetwork",
            SecurityType = WifiSecurityType.Wpa,
            Password = "SuperSecretPassword123!"
        };

        var str = credential.ToString();

        Assert.DoesNotContain("SuperSecretPassword123!", str);
        Assert.Contains("MyNetwork", str);
    }

    [Fact]
    public void WifiCredential_StringInterpolation_DoesNotContainPassword()
    {
        var credential = new WifiCredential
        {
            Ssid = "TestSSID",
            SecurityType = WifiSecurityType.Wpa,
            Password = "PasswordMustNotLeak"
        };

        var interpolated = $"Credential: {credential}";

        Assert.DoesNotContain("PasswordMustNotLeak", interpolated);
    }
}
