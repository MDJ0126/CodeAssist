using CodeAssist.Core;
namespace CodeAssist.Core.Tests;
public sealed class StartupPreferencesTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "CodeAssistStartupTests", Guid.NewGuid().ToString("N"));
    private string State => Path.Combine(directory, "startup.xml");
    [Theory] [InlineData(true)] [InlineData(false)]
    public void DisplaySettingsSurviveSaveAndCopy(bool alphabetical)
    {
        var state=new StartupPreferences {SortNavigationAlphabetically=alphabetical,ContextBarEnabled=false,ColumnIndicatorEnabled=true,ColumnIndicator=75,QuickInfoEnabled=false};
        state.Save(State);var restored=StartupPreferences.Load(State).Copy();
        Assert.Equal(alphabetical,restored.SortNavigationAlphabetically);
        Assert.False(restored.ContextBarEnabled);Assert.True(restored.ColumnIndicatorEnabled);
        Assert.Equal(75,restored.ColumnIndicator);Assert.False(restored.QuickInfoEnabled);
    }
    [Fact] public void FirstRunShowsWelcomeAndInfoBarBeforeModalSetup()
    {
        Assert.Equal(new[] { StartupAction.Welcome, StartupAction.InfoBar, StartupAction.IntegrationSetup },
            new StartupPreferences().GetStartupActions());
    }
    [Fact] public void StartupRespectsPreviouslyDismissedSurfaces()
    {
        var state = new StartupPreferences { SetupCompleted = true, WelcomeSeen = true, HideWelcomeBar = true };
        Assert.Empty(state.GetStartupActions());
        state.ShowWelcomeOnStartup = true;
        Assert.Equal(new[] { StartupAction.Welcome }, state.GetStartupActions());
    }
    [Fact] public void NewInstallRequestsSetupAndWelcome()
    {
        var state = StartupPreferences.Load(State);
        Assert.False(state.SetupCompleted);
        Assert.True(state.ShouldShowWelcome);
        Assert.True(state.CompletionEnabled);
        Assert.True(state.ColoringEnabled);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public void IntegrationSelectionControlsBothEditorFeatures(bool full)
    {
        var state = new StartupPreferences(); state.ApplyIntegration(full);
        Assert.True(state.SetupCompleted);
        Assert.Equal(full, state.CompletionEnabled); Assert.Equal(full, state.ColoringEnabled);
    }
    [Fact] public void SeenWelcomeDoesNotReopenUnlessRequested()
    {
        var state = new StartupPreferences { WelcomeSeen = true };
        Assert.False(state.ShouldShowWelcome);
        state.ShowWelcomeOnStartup = true; Assert.True(state.ShouldShowWelcome);
    }
    [Fact] public void SettingsSurviveRestartAndReplacementCreatesBackup()
    {
        var state = new StartupPreferences { WelcomeSeen = true, HideWelcomeBar = true };
        state.ApplyIntegration(false); state.Save(State);
        var loaded = StartupPreferences.Load(State);
        Assert.True(loaded.SetupCompleted); Assert.True(loaded.HideWelcomeBar);
        Assert.False(loaded.ShouldShowWelcome); Assert.False(loaded.CompletionEnabled);
        loaded.ShowWelcomeOnStartup = true; loaded.Save(State);
        Assert.True(StartupPreferences.Load(State).ShouldShowWelcome);
        Assert.True(File.Exists(State + ".bak"));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }
    [Fact] public void DamagedSettingsRecoverWithoutDeletingEvidence()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(State, "<broken");
        Assert.False(StartupPreferences.Load(State).SetupCompleted);
        Assert.Equal("<broken", File.ReadAllText(State));
    }
    [Fact] public void CompletionAcceptanceTogglesPersistIndependently()
    {
        var state=new StartupPreferences{CompletionCommitWithEnter=false,CompletionCommitWithTab=true};state.Save(State);
        var loaded=StartupPreferences.Load(State);Assert.False(loaded.CompletionCommitWithEnter);Assert.True(loaded.CompletionCommitWithTab);
        var copy=loaded.Copy();copy.CompletionCommitWithEnter=true;Assert.False(loaded.CompletionCommitWithEnter);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}