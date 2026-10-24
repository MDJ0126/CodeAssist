using CodeAssist.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;

namespace CodeAssist.Extension
{
    internal static class StartupCommandRegistration
    {
        internal static void Register(IMenuCommandService commands, Action welcome, Action settings)
        {
            if (commands == null) throw new InvalidOperationException("Visual Studio menu command service is unavailable.");
            commands.AddCommand(new OleMenuCommand((s, e) => welcome(), new CommandID(GoToSymbolCommand.CommandSet, 0x140)));
            commands.AddCommand(new OleMenuCommand((s, e) => settings(), new CommandID(GoToSymbolCommand.CommandSet, 0x141)));
        }
    }

    internal static class StartupUi
    {
        internal static readonly string StatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeAssist", "startup.xml");
        internal static StartupPreferences Preferences { get; private set; } = StartupPreferences.Load(StatePath);
        internal static event Action SettingsApplied;
        private static AsyncPackage package;
        private static bool packageReady;
        private static bool welcomeRequested;
        private static bool welcomeOpening;
        private static readonly WelcomeDocumentWindow welcomeDocument = new WelcomeDocumentWindow();
        private static IVsInfoBarUIElement bar;
        private static BarEvents barEvents;
        private static uint cookie;
        private static DispatcherTimer barRetry;
        private static int barRetryAttempts;
        private static ProjectInspection inspection;
        private static EnvDTE.SolutionEvents solutionEvents;
        private static Microsoft.VisualStudio.Imaging.Interop.IImageHandle iconHandle;
        internal static Microsoft.VisualStudio.Imaging.Interop.ImageMoniker BrandIcon => iconHandle?.Moniker ?? KnownMonikers.StatusInformation;

        internal static async Task InitializeAsync(AsyncPackage owner)
        {
            await owner.JoinableTaskFactory.SwitchToMainThreadAsync();
            package = owner;
            var images = await owner.GetServiceAsync(typeof(SVsImageService)) as IVsManagedImageService;
            var icon = Path.Combine(Path.GetDirectoryName(typeof(StartupUi).Assembly.Location), "Resources", "CodeAssist.png");
            if (images != null && File.Exists(icon)) { var brand=new BitmapImage(new Uri(icon));iconHandle = images.AddCustomImage(brand, false);CompletionIcons.Initialize(images,brand); }
            var commands = await owner.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService;
            await owner.JoinableTaskFactory.SwitchToMainThreadAsync();
            StartupCommandRegistration.Register(commands, OpenWelcome, () => Configure(false));
            WelcomeDocumentWindow.RecordEvent("StartupCommandsRegistered Welcome=0x140 Settings=0x141");
            var dte = await owner.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if(dte != null)
            {
                solutionEvents = dte.Events.SolutionEvents;
                solutionEvents.Opened += OnSolutionChanged; solutionEvents.AfterClosing += OnSolutionChanged;
                if(!Preferences.ToolbarInitialized || Preferences.ToolbarLayoutVersion<2)
                {
                    try {
                        var bars = dte.CommandBars as Microsoft.VisualStudio.CommandBars.CommandBars;
                        if(bars != null) { bars["CodeAssist"].Reset();bars["CodeAssist"].Visible=true;Preferences.ToolbarInitialized=true;Preferences.ToolbarLayoutVersion=2;Save(); }
                    } catch(Exception ex) { ActivityLog.LogWarning("CodeAssist Toolbar",ex.Message); }
                }
            }
            EditorClipboardHistory.History.Capacity=Preferences.ClipboardLimit;
        }

        internal static void CompleteInitialization()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            packageReady = true;
            WelcomeDocumentWindow.RecordEvent("PackageInitializationCompleted");
            // Queue only after every package initialization await has completed.
            #pragma warning disable VSTHRD001 // Intentional WPF idle callback after package initialization; already on shell UI thread.
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    if (package == null || package.DisposalToken.IsCancellationRequested) return;
            var keyboardDte=Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if(keyboardDte!=null && Preferences.KeyboardCompatibilityProfile!=keyboardDte.RegistryRoot && KeyboardCompatibility.Apply(keyboardDte)) {
                Preferences.KeyboardCompatibilityProfile=keyboardDte.RegistryRoot;Save();
            }

                    ToolbarCommands.RefreshLabels();
                    if (welcomeRequested && !Preferences.ShouldShowWelcome) OpenWelcome();
                    foreach (var action in Preferences.GetStartupActions())
                    {
                        try
                        {
                            switch (action)
                            {
                                case StartupAction.Welcome: OpenWelcome(); break;
                                case StartupAction.InfoBar: ShowBar(); break;
                                case StartupAction.IntegrationSetup: Configure(true); break;
                            }
                        }
                        catch (Exception ex) { ActivityLog.LogError("CodeAssist Startup " + action, ex.ToString()); }
                    }
                }
                catch (Exception ex) { ActivityLog.LogError("CodeAssist Startup", ex.ToString()); }
            }));
        }

#pragma warning restore VSTHRD001

        private static void OnSolutionChanged()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RefreshBar();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if(dte?.Solution?.IsOpen == true && Preferences.InspectionMode == InspectionMode.Project) inspection?.Start();
            else inspection?.Stop();
        }

        internal static bool Save()
        {
            try { Preferences.Save(StatePath); return true; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ActivityLog.LogError("CodeAssist Settings", ex.ToString());
                MessageBox.Show("설정을 저장하지 못했습니다. " + ex.Message, "CodeAssist", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        #pragma warning disable VSTHRD010 // UI guarded by owning document/configure caller; view also instantiated by offline STA renderer.
        internal static FrameworkElement Welcome() => ProductWelcome.Create();
#pragma warning restore VSTHRD010

        internal static bool ApplyPreferences(StartupPreferences value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var previous = Preferences;
            Preferences = value;
            if(!Save()) { Preferences=previous; return false; }
            EditorClipboardHistory.History.Capacity = value.ClipboardLimit;
            SettingsApplied?.Invoke();
            RefreshBar();
            if(value.InspectionMode == InspectionMode.Project) inspection?.Start();
            else inspection?.Stop();
            return true;
        }
        internal static void AttachInspection(ProjectIndexService index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            inspection = new ProjectInspection(package,index);
            if(Preferences.InspectionMode == InspectionMode.Project) inspection.Start();
            RefreshBar();
        }
        internal static void OpenHelp(string topic)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(package==null)throw new InvalidOperationException("CodeAssist document host is not ready.");
            package.JoinableTaskFactory.RunAsync(async()=>{
                var pane=await package.ShowToolWindowAsync(typeof(HelpToolWindow),0,true,package.DisposalToken);
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(!(pane?.Content is HelpView view))throw new InvalidOperationException("CodeAssist help view is unavailable.");
                pane.Caption="CodeAssist — "+UiText.Get("Documentation",Preferences.UiLanguage);
                view.Navigate(topic);WelcomeDocumentWindow.RecordEvent("HelpOpened topic="+topic);
            }).FileAndForget("CodeAssist/Help");
        }

        internal static void Execute(int id)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            package.JoinableTaskFactory.Run(async () =>
            {
                var commands = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                commands?.GlobalInvoke(new CommandID(GoToSymbolCommand.CommandSet, id));
            });
        }

        #pragma warning disable VSTHRD010 // UI guarded by owning document/configure caller; view also instantiated by offline STA renderer.
        internal static Window CreateConfigurationWindow(bool firstRun) => ProductOptions.Create(firstRun);
#pragma warning restore VSTHRD010

        internal static void Configure(bool firstRun)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var window = CreateConfigurationWindow(firstRun);
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell != null && shell.GetDialogOwnerHwnd(out var hwnd) >= 0) new WindowInteropHelper(window).Owner = hwnd;
            window.ShowDialog();

        }

        internal static void OpenWelcome()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            WelcomeDocumentWindow.RecordEvent("WelcomeCommandInvoked ready=" + packageReady);
            if (!packageReady) { welcomeRequested = true; return; }
            if (welcomeOpening) { WelcomeDocumentWindow.RecordEvent("WelcomeOpenAlreadyInProgress"); return; }
            welcomeRequested = false; welcomeOpening = true;
            try
            {
                welcomeDocument.Show(package);
                Preferences.WelcomeSeen = true; Save();
            }
            catch (Exception error)
            {
                WelcomeDocumentWindow.RecordFailure("OpenWelcome", error);
                MessageBox.Show("Welcome 문서를 열지 못했습니다. 오류 코드: 0x" + error.HResult.ToString("X8") + Environment.NewLine +
                    "실패 단계와 상세 기록: " + WelcomeDocumentWindow.DiagnosticPath,
                    "CodeAssist Welcome", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { welcomeOpening = false; }
        }


        private static void ShowBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Preferences.HideWelcomeBar || bar != null) return;
            var shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
            if (shell == null || shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var value) < 0 || !(value is IVsInfoBarHost host)) { RetryBar(); return; }
            var factory = Package.GetGlobalService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            if (factory == null) { RetryBar(); return; }
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            var ask = Preferences.InspectionMode == InspectionMode.Ask && dte?.Solution?.IsOpen == true;
            var message = ask ? string.Format(UiText.Get("Inspection.Prompt",Preferences.UiLanguage),Path.GetFileNameWithoutExtension(dte.Solution.FullName))
                : UiText.Get("Inspection.Start",Preferences.UiLanguage);
            var actions = ask ? new IVsInfoBarActionItem[] {
                new InfoBarHyperlink(UiText.Get("Enable",Preferences.UiLanguage), "enable"), new InfoBarHyperlink(UiText.Get("Disable",Preferences.UiLanguage), "disable"),
                new InfoBarHyperlink(UiText.Get("Use per-file engine",Preferences.UiLanguage), "perfile"), new InfoBarHyperlink(UiText.Get("Welcome",Preferences.UiLanguage), "welcome"), new InfoBarHyperlink(UiText.Get("Options",Preferences.UiLanguage), "settings") }
                : new IVsInfoBarActionItem[] { new InfoBarHyperlink(UiText.Get("Welcome",Preferences.UiLanguage), "welcome"), new InfoBarHyperlink(UiText.Get("Options",Preferences.UiLanguage), "settings") };
            bar = factory.CreateInfoBar(new InfoBarModel(message, actions, BrandIcon, true));
            barEvents = new BarEvents(); bar.Advise(barEvents, out cookie); host.AddInfoBar(bar);
            barRetry?.Stop();
        }

        private static void RefreshBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if(bar != null) { bar.Unadvise(cookie);bar.Close();bar=null;barEvents=null; }
            barRetry?.Stop();barRetryAttempts=0;
            if(!Preferences.HideWelcomeBar) ShowBar();
        }

        private static void RetryBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (barRetryAttempts >= 10 || package == null) return;
            if (barRetry == null)
            {
                barRetry = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                barRetry.Tick += (sender, args) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    barRetryAttempts++;
                    if (package == null || Preferences.HideWelcomeBar) { barRetry.Stop(); return; }
                    try { ShowBar(); }
                    catch (Exception ex) { ActivityLog.LogError("CodeAssist InfoBar", ex.ToString()); barRetry.Stop(); }
                    if (barRetryAttempts >= 10) { barRetry.Stop(); if (bar == null) ActivityLog.LogWarning("CodeAssist InfoBar", "Host unavailable after 10 attempts."); }
                };
            }
            if (!barRetry.IsEnabled) barRetry.Start();
        }

        private sealed class BarEvents : IVsInfoBarUIEvents
        {
            public void OnActionItemClicked(IVsInfoBarUIElement element, IVsInfoBarActionItem item)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (Equals(item.ActionContext, "welcome")) OpenWelcome();
                else if (Equals(item.ActionContext, "enable") || Equals(item.ActionContext, "disable") || Equals(item.ActionContext, "perfile"))
                {
                    var draft = Preferences.Copy();
                    draft.InspectionMode = Equals(item.ActionContext, "enable") ? InspectionMode.Project :
                        Equals(item.ActionContext, "disable") ? InspectionMode.Disabled : InspectionMode.PerFile;
                    ApplyPreferences(draft);
                }
                else Configure(false);
            }
            public void OnClosed(IVsInfoBarUIElement element)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                element.Unadvise(cookie); bar = null; barEvents = null;
                Preferences.HideWelcomeBar = true; Save();
            }
        }
        internal static void Shutdown()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (bar != null) { bar.Unadvise(cookie); bar.Close(); bar = null; barEvents = null; }
            if(solutionEvents != null) { solutionEvents.Opened -= OnSolutionChanged; solutionEvents.AfterClosing -= OnSolutionChanged; solutionEvents=null; }
            inspection?.Dispose(); inspection=null;
            welcomeDocument.Dispose();
            barRetry?.Stop(); barRetry = null;
            package = null;
        }
    }
}