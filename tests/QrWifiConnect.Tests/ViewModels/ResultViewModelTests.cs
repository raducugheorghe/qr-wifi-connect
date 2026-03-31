using QrWifiConnect.Models;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.ViewModels;

public sealed class ResultViewModelTests
{
    private readonly FakeNavigationService _navigation = new();

    private ResultViewModel CreateSut(ConnectionResult result)
    {
        var vm = new ResultViewModel(_navigation);
        vm.ConnectionResult = result;
        return vm;
    }

    // --- Success state ---

    [Fact]
    public void IsSuccess_WhenSuccessResult_ReturnsTrue()
    {
        var vm = CreateSut(ConnectionResult.CreateSuccess("HomeNet"));

        Assert.True(vm.IsSuccess);
    }

    [Fact]
    public void Ssid_WhenSuccessResult_ReturnsCorrectSsid()
    {
        var vm = CreateSut(ConnectionResult.CreateSuccess("HomeNet"));

        Assert.Equal("HomeNet", vm.Ssid);
    }

    // --- Failure state ---

    [Fact]
    public void IsSuccess_WhenFailureResult_ReturnsFalse()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Wrong password"));

        Assert.False(vm.IsSuccess);
    }

    [Fact]
    public void Reason_WhenFailureResult_ReturnsReason()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Incorrect password or authentication failed."));

        Assert.Equal("Incorrect password or authentication failed.", vm.Reason);
    }

    [Fact]
    public void IsTimeout_WhenTimeoutFailure_ReturnsTrue()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Timed out", isTimeout: true));

        Assert.True(vm.IsTimeout);
    }

    [Fact]
    public void IsTimeout_WhenNonTimeoutFailure_ReturnsFalse()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Wrong password", isTimeout: false));

        Assert.False(vm.IsTimeout);
    }

    // --- RetryCommand ---

    [Fact]
    public async Task RetryCommand_NavigatesToScanner()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Failed"));

        await vm.RetryCommand.ExecuteAsync(null);

        Assert.Equal("//scanner", _navigation.LastRoute);
    }

    [Fact]
    public async Task RetryCommand_ClearsConnectionResult()
    {
        var vm = CreateSut(ConnectionResult.CreateFailure("BadNet", "Failed"));

        await vm.RetryCommand.ExecuteAsync(null);

        Assert.Null(vm.ConnectionResult);
    }

    // --- ExitCommand ---

    [Fact]
    public void ExitCommand_InvokesApplicationQuit()
    {
        var vm = CreateSut(ConnectionResult.CreateSuccess("HomeNet"));

        vm.ExitCommand.Execute(null);

        Assert.Equal(1, _navigation.QuitCallCount);
    }

    [Fact]
    public void ExitCommand_ClearsConnectionResult()
    {
        var vm = CreateSut(ConnectionResult.CreateSuccess("HomeNet"));

        vm.ExitCommand.Execute(null);

        Assert.Null(vm.ConnectionResult);
    }
}
