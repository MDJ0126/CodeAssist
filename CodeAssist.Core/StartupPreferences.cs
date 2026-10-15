using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
namespace CodeAssist.Core
{
    public enum InspectionMode { Ask, Disabled, PerFile, Project }

    public enum StartupAction { Welcome, InfoBar, IntegrationSetup }

    public sealed class StartupPreferences
    {
        public bool CompletionUseVisualStudioColors { get; set; }
        public string UiLanguage { get; set; } = "auto";
        public bool SmartSelectEnabled { get; set; } = true;
        public bool SmartSelectSplitWords { get; set; }
        public bool SmartSelectTrimWhitespace { get; set; }
        public bool ContextBarEnabled { get; set; } = true;
        public int ToolbarLayoutVersion { get; set; }
        public bool ToolbarInitialized { get; set; }
        public InspectionMode InspectionMode { get; set; }
        private int clipboardLimit = 10;
        public int ClipboardLimit { get => clipboardLimit; set => clipboardLimit = Math.Max(1, Math.Min(50, value)); }
        private int liveUpdateDelay = 150;
        public int LiveUpdateDelay { get => liveUpdateDelay; set => liveUpdateDelay = Math.Max(50, Math.Min(1000, value)); }
        public string TypeColor { get; set; } = "#FFD700";
        public string VariableColor { get; set; } = "#BDB76B";
        public string FunctionColor { get; set; } = "#FF8000";
        public string MacroColor { get; set; } = "#BD63C5";
        public StartupPreferences Copy()
        {
            using (var output = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(StartupPreferences));
                serializer.Serialize(output, this);
                using (var input = new StringReader(output.ToString()))
                    return (StartupPreferences)serializer.Deserialize(input);
            }
        }
        public bool SetupCompleted { get; set; }
        public bool WelcomeSeen { get; set; }
        public bool ShowWelcomeOnStartup { get; set; }
        public bool HideWelcomeBar { get; set; }
        public bool CompletionEnabled { get; set; } = true;
        public bool ColoringEnabled { get; set; } = true;
        public bool ShouldShowWelcome => ShowWelcomeOnStartup || !WelcomeSeen;
        public IReadOnlyList<StartupAction> GetStartupActions()
        {
            var actions = new List<StartupAction>();
            if (ShouldShowWelcome) actions.Add(StartupAction.Welcome);
            if (!HideWelcomeBar) actions.Add(StartupAction.InfoBar);
            if (!SetupCompleted) actions.Add(StartupAction.IntegrationSetup);
            return actions;
        }
        public void ApplyIntegration(bool full)
        {
            CompletionEnabled = full; ColoringEnabled = full; SetupCompleted = true;
        }
        public static StartupPreferences Load(string path)
        {
            if (!File.Exists(path)) return new StartupPreferences();
            try
            {
                using (var stream = File.OpenRead(path))
                    return (StartupPreferences)new XmlSerializer(typeof(StartupPreferences)).Deserialize(stream);
            }
            catch (InvalidOperationException) { return new StartupPreferences(); }
            catch (IOException) { return new StartupPreferences(); }
            catch (UnauthorizedAccessException) { return new StartupPreferences(); }
        }
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = File.Create(temporary))
                    new XmlSerializer(typeof(StartupPreferences)).Serialize(stream, this);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}