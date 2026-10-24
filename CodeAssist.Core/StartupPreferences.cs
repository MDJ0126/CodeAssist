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
        public List<SourceLinkRule> SourceLinkRules { get; set; } = new List<SourceLinkRule>();
        public bool HighlightCurrentLine { get; set; } = true;
        public bool HighlightMatchingBraces { get; set; } = true;
        public bool HighlightMismatchingBraces { get; set; } = true;
        public bool ColumnIndicatorEnabled { get; set; }
        private int columnIndicator=100;
        public int ColumnIndicator { get=>columnIndicator;set=>columnIndicator=Math.Max(20,Math.Min(500,value)); }
        private int parseConcurrency=2;
        public int ParseConcurrency { get=>parseConcurrency;set=>parseConcurrency=Math.Max(1,Math.Min(16,value)); }
        public bool AcronymCompletionEnabled { get; set; } = true;
        public bool ShorthandCompletionEnabled { get; set; } = true;
        public bool ListNonInheritedMembersFirst { get; set; }
        public bool SnippetShortcutsEnabled { get; set; } = true;
        public bool ConvertDotToArrow { get; set; }
        public bool ConvertOverloadedArrow { get; set; } = true;
        public bool DoubleKeyRepeat { get; set; }
        public bool SurroundHash { get; set; } = true;
        public bool SurroundWithSnippets { get; set; } = true;
        public bool ColorTextEditors { get; set; } = true;
        public bool ColorTooltips { get; set; } = true;
        public bool ColorListboxes { get; set; } = true;
        public bool ColorObjectBrowser { get; set; } = true;
        public bool ColorViews { get; set; } = true;
        public bool ColorNavigationBar { get; set; } = true;
        public List<string> CustomPaletteColors { get; set; } = new List<string>();
        public string EnumMemberColor { get; set; } = "#B88020";
        public string NamespaceColor { get; set; } = "#B9D7A2";
        public bool RepairTypeCase { get; set; }
        public bool InsertUnderscoreAfterM { get; set; }
        public bool LocalSymbolsBold { get; set; }
        public bool SystemSymbolsItalic { get; set; }
        public string CustomFileExtensions { get; set; } = "";
        public string CompilationDatabaseDirectory { get; set; } = "";
        public bool AutomaticReferenceHighlights { get; set; }
        public bool AltClickGoToDefinition { get; set; }
        public bool SmartSuggestionsEnabled { get; set; } = true;
        public bool SuggestionListsEnabled { get; set; } = true;
        public bool SnippetSuggestionsEnabled { get; set; } = true;
        public string MethodBodyTemplate { get; set; } = "    throw \"Not implemented\";";
        public bool StepFilterEnabled { get; set; }
        public string StepFilterPatterns { get; set; } = "std::*";
        public bool OutputFileHighlighting { get; set; } = true;
        public bool FindResultHighlighting { get; set; } = true;
        public bool SpellUnderliningEnabled { get; set; }
        public string SpellingLanguage { get; set; } = "en-US";
        public bool PairCompletionEnabled { get; set; } = true;
        public ClosingBracePlacement ClosingBracePlacement { get; set; } = ClosingBracePlacement.BlankLine;
        private int methodParenthesesSpaces;
        public int MethodParenthesesSpaces { get=>methodParenthesesSpaces;set=>methodParenthesesSpaces=Math.Max(0,Math.Min(10,value)); }
        public bool QuickInfoEnabled { get; set; } = true;
        public bool ExtendCommentsEnabled { get; set; }
        public bool SurroundSelectionEnabled { get; set; } = true;
        public bool SurroundSlash { get; set; } = true;
        public bool SurroundAsterisk { get; set; } = true;
        public bool SurroundParentheses { get; set; } = true;
        public bool SurroundBraces { get; set; } = true;
        public bool CommentSpaceEnabled { get; set; } = true;
        public bool FormatAfterPasteEnabled { get; set; }
        public bool CompletionUseVisualStudioColors { get; set; }
        public string UiLanguage { get; set; } = "auto";
        public bool SmartSelectEnabled { get; set; } = true;
        public bool SmartSelectSplitWords { get => SmartSelectSplitCase && SmartSelectSplitUnderscores; set {SmartSelectSplitCase=value;SmartSelectSplitUnderscores=value;} }
        public bool SmartSelectSplitCase { get; set; }
        public bool SmartSelectSplitUnderscores { get; set; }
        public bool SmartSelectSmallIncrements { get; set; } = true;
        public bool SmartSelectStartWithWord { get; set; } = true;
        public bool SmartSelectTrimWhitespace { get; set; }
        public bool ContextBarEnabled { get; set; } = true;
        public bool SortNavigationAlphabetically { get; set; } = true;
        public int ToolbarLayoutVersion { get; set; }
        public bool ToolbarInitialized { get; set; }
        public InspectionMode InspectionMode { get; set; }
        public bool ClipboardMenuEnabled { get; set; } = true;
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
        public bool CompletionCommitWithTab { get; set; } = true;
        public bool CompletionCommitWithEnter { get; set; } = true;
        public string KeyboardCompatibilityProfile { get; set; } = "";
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