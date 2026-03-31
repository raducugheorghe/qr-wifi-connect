using QrWifiConnect.Models;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.Integration;

/// <summary>
/// Integration tests for the connecting → result flow.
/// Uses FakeWifiConnector — no live network access required.
/// </summary>
public sealed class ResultFlowIntegrationTests
{
    private readonly FakeWifiConnector _connector = new();
    private readonly FakeNavigationService _navigation = new();

    private static WifiCredential TestCredential => new()
    {
        Ssid = "TestNet",
        SecurityType = WifiSecurityType.Wpa,
        Password = "pass123"
    };

    // --- Success result flow ---

    [Fact]
    public async Task SuccessfulConnection_NavigatesToResultWithSuccessOutcome()
    {
        _connector.SetSuccess("TestNet");
        var connecting = new ConnectingViewModel(_connector, _navigation)
        {
            Credential = TestCredential
        };

        await connecting.OnAppearingAsync();

        Assert.Equal("result", _navigation.LastRoute);
        var result = _navigation.LastParams?["connectionResult"] as ConnectionResult;
        Assert.IsType<ConnectionResult.SuccessResult>(result);
    }

    [Fact]
    public async Task SuccessfulConnection_ResultViewModelShowsSuccessState()
    {
        _connector.SetSuccess("TestNet");
        var connectingNav = new FakeNavigationService();
        var connecting = new ConnectingViewModel(_connector, connectingNav)
        {
            Credential = TestCredential
        };

        await connecting.OnAppearingAsync();

        var result = connectingNav.LastParams?["connectionResult"] as ConnectionResult;
        var resultNav = new FakeNavigationService();
        var resultVm = new ResultViewModel(resultNav) { ConnectionResult = result };

        Assert.True(resultVm.IsSuccess);
        Assert.Equal("TestNet", resultVm.Ssid);
    }

    // --- Failure result flow ---

    [Fact]
    public async Task FailedConnection_NavigatesToResultWithFailureOutcome()
    {
        _connector.SetFailure("TestNet", "Wrong password");
        var connecting = new ConnectingViewModel(_connector, _navigation)
        {
            Credential = TestCredential
        };

        await connecting.OnAppearingAsync();

        var result = _navigation.LastParams?["connectionResult"] as ConnectionResult;
        Assert.IsType<ConnectionResult.FailureResult>(result);
    }

    [Fact]
    public async Task FailureResult_ReasonIsPreserved()
    {
        _connector.SetFailure("TestNet", "Incorrect password or authentication failed.");
        var connectingNav = new FakeNavigationService();
        var connecting = new ConnectingViewModel(_connector, connectingNav)
        {
            Credential = TestCredential
        };

        await connecting.OnAppearingAsync();

        var result = connectingNav.LastParams?["connectionResult"] as ConnectionResult;
        var resultVm = new ResultViewModel(new FakeNavigationService()) { ConnectionResult = result };

        Assert.False(resultVm.IsSuccess);
        Assert.Equal("Incorrect password or authentication failed.", resultVm.Reason);
    }

    // --- Retry navigation ---

    [Fact]
    public async Task RetryCommand_NavigatesBackToScanner()
    {
        var resultNav = new FakeNavigationService();
        var vm = new ResultViewModel(resultNav)
        {
            ConnectionResult = ConnectionResult.CreateFailure("TestNet", "Failed")
        };

        await vm.RetryCommand.ExecuteAsync(null);

        Assert.Equal("//scanner", resultNav.LastRoute);
    }

    // --- Exit action ---

    [Fact]
    public void ExitCommand_QuitsApplication()
    {
        var resultNav = new FakeNavigationService();
        var vm = new ResultViewModel(resultNav)
        {
            ConnectionResult = ConnectionResult.CreateSuccess("TestNet")
        };

        vm.ExitCommand.Execute(null);

        Assert.Equal(1, resultNav.QuitCallCount);
    }

    // --- Credential cleared after connection ---

    [Fact]
    public async Task ConnectingViewModel_CredentialClearedAfterConnect()
    {
        _connector.SetSuccess("TestNet");
        var connecting = new ConnectingViewModel(_connector, _navigation)
        {
            Credential = TestCredential
        };

        await connecting.OnAppearingAsync();

        Assert.Null(connecting.Credential);
    }
}
