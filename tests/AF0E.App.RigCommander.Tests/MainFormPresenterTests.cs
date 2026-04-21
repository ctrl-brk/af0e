using FluentAssertions;
using RigCommander.Presentation;
using RigCommander.Tests.TestDoubles;
using System.Windows.Forms;
using Xunit;

namespace RigCommander.Tests;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public sealed class MainFormPresenterTests
{
    [Fact]
    public void BuildInitialState_UsesServerAndProfile_AndReadsStartupRegistration()
    {
        var startupSpy = new StartupRegistrationSpy { IsEnabledResult = true };
        var settings = new RigCommanderSettings { ActiveProfile = "IC-9100" };
        var presenter = new MainFormPresenter(settings, startupSpy);

        var state = presenter.BuildInitialState(new Uri("http://localhost:5050"));

        state.ServerLabelText.Should().Be($"Server: http://localhost:5050/{Environment.NewLine}Radio: IC-9100");
        state.RunAtStartupEnabled.Should().BeTrue();
        startupSpy.IsEnabledCalls.Should().Be(1);
    }

    [Fact]
    public void BuildInitialState_DefaultsStartupToFalse_WhenRegistrationMissing()
    {
        var settings = new RigCommanderSettings { ActiveProfile = "IC-7410" };
        var presenter = new MainFormPresenter(settings, startupRegistration: null);

        var state = presenter.BuildInitialState(new Uri("http://localhost:5050"));

        state.RunAtStartupEnabled.Should().BeFalse();
    }

    [Fact]
    public void ShouldStartMinimizedOnShown_ReturnsSettingOnFirstCall_ThenFalse()
    {
        var settings = new RigCommanderSettings { Ui = new Ui { StartMinimized = true } };
        var presenter = new MainFormPresenter(settings, startupRegistration: null);

        var first = presenter.ShouldStartMinimizedOnShown();
        var second = presenter.ShouldStartMinimizedOnShown();

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public void ShouldHideToTrayOnResize_ReturnsTrueOnlyForMinimized()
    {
        MainFormPresenter.ShouldHideToTrayOnResize(FormWindowState.Minimized).Should().BeTrue();
        MainFormPresenter.ShouldHideToTrayOnResize(FormWindowState.Normal).Should().BeFalse();
        MainFormPresenter.ShouldHideToTrayOnResize(FormWindowState.Maximized).Should().BeFalse();
    }

    [Fact]
    public void OnRunAtStartupToggled_CallsRegistration_WhenInteractive()
    {
        var startupSpy = new StartupRegistrationSpy();
        var presenter = new MainFormPresenter(new RigCommanderSettings(), startupSpy);

        presenter.OnRunAtStartupToggled(enabled: true, suppressChange: false, isDesignTime: false);

        startupSpy.SetEnabledCalls.Should().ContainSingle().Which.Should().BeTrue();
    }

    [Fact]
    public void OnRunAtStartupToggled_DoesNothing_WhenSuppressedOrDesignTime()
    {
        var startupSpy = new StartupRegistrationSpy();
        var presenter = new MainFormPresenter(new RigCommanderSettings(), startupSpy);

        presenter.OnRunAtStartupToggled(enabled: true, suppressChange: true, isDesignTime: false);
        presenter.OnRunAtStartupToggled(enabled: false, suppressChange: false, isDesignTime: true);

        startupSpy.SetEnabledCalls.Should().BeEmpty();
    }

}
