using QrWifiConnect.Models;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.ViewModels;

public sealed class ConfirmationViewModelTests
{
    private readonly FakeNavigationService _navigation = new();

    private ConfirmationViewModel CreateSut(WifiCredential? credential = null)
    {
        var vm = new ConfirmationViewModel(_navigation);
        vm.Credential = credential ?? new WifiCredential
        {
            Ssid = "TestNet",
            SecurityType = WifiSecurityType.Wpa,
            Password = "test123"
        };
        return vm;
    }

    // --- Connect navigates to connecting with credential ---

    [Fact]
    public async Task ConnectCommand_NavigatesToConnecting()
    {
        var vm = CreateSut();

        await vm.ConnectCommand.ExecuteAsync(null);

        Assert.Equal("connecting", _navigation.LastRoute);
    }

    [Fact]
    public async Task ConnectCommand_PassesCredentialAsParameter()
    {
        var credential = new WifiCredential
        {
            Ssid = "MyHome",
            SecurityType = WifiSecurityType.Wpa,
            Password = "homepass"
        };
        var vm = CreateSut(credential);

        await vm.ConnectCommand.ExecuteAsync(null);

        Assert.NotNull(_navigation.LastParams);
        var passed = _navigation.LastParams!["credential"] as WifiCredential;
        Assert.NotNull(passed);
        Assert.Equal("MyHome", passed.Ssid);
    }

    [Fact]
    public async Task ConnectCommand_ClearsCredentialAfterNavigate()
    {
        var vm = CreateSut();

        await vm.ConnectCommand.ExecuteAsync(null);

        Assert.Null(vm.Credential);
    }

    // --- Cancel navigates back to scanner ---

    [Fact]
    public async Task CancelCommand_NavigatesToScanner()
    {
        var vm = CreateSut();

        await vm.CancelCommand.ExecuteAsync(null);

        Assert.Equal("//scanner", _navigation.LastRoute);
    }

    [Fact]
    public async Task CancelCommand_ClearsCredential()
    {
        var vm = CreateSut();

        await vm.CancelCommand.ExecuteAsync(null);

        Assert.Null(vm.Credential);
    }

    // --- Exposed properties ---

    [Fact]
    public void Ssid_ReflectsCredential()
    {
        var vm = CreateSut(new WifiCredential
        {
            Ssid = "Office",
            SecurityType = WifiSecurityType.Wpa3,
            Password = "off!ce"
        });

        Assert.Equal("Office", vm.Ssid);
    }

    [Fact]
    public void IsHidden_WhenHiddenCredential_ReturnsTrue()
    {
        var vm = CreateSut(new WifiCredential
        {
            Ssid = "Hidden",
            SecurityType = WifiSecurityType.Wpa,
            Password = "pass",
            IsHidden = true
        });

        Assert.True(vm.IsHidden);
    }
}
