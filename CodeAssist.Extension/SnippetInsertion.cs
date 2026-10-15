using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CodeAssist.Core;

namespace CodeAssist.Extension
{
    internal static class SnippetInsertion
    {
        private static Dictionary<string, string> environment;
        internal static void CaptureEnvironment()
        {
            if (environment != null) return;
            environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
                environment[(string)entry.Key] = (string)entry.Value;
        }
        private static readonly SnippetTemplate[] BuiltIns =
        {
            new SnippetTemplate("Include", "include", "#include \"$Header$\"$end$"),
            new SnippetTemplate("If block", "if", "if ($Condition$)\n{\n    $selected$$end$\n}"),
            new SnippetTemplate("For loop", "for", "for (int32 $Index$ = 0; $Index$ < $Count$; ++$Index$)\n{\n    $selected$$end$\n}"),
            new SnippetTemplate("Unreal property", "uproperty", "UPROPERTY($Specifiers$)\n$Type$ $Name$;$end$"),
            new SnippetTemplate("Unreal function", "ufunction", "UFUNCTION($Specifiers$)\n$ReturnType$ $Name$($Arguments$);$end$"),
            new SnippetTemplate("Unreal struct", "ustruct", "USTRUCT($Specifiers$)\nstruct $Name$\n{\n    GENERATED_BODY()\n\n    $selected$$end$\n};"),
            new SnippetTemplate("Header guard", "guard", "#ifndef $FILE_BASE$_H\n#define $FILE_BASE$_H\n\n$selected$$end$\n\n#endif")
        };
        internal static void Show(IWpfTextView view, string file, string projectRoot, IntPtr owner, string projectFile = null, string solutionFile = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var snapshot = view.TextSnapshot;
            var selection = view.Selection.StreamSelectionSpan.SnapshotSpan;
            var templates = new List<SnippetTemplate>(BuiltIns);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                if (!Directory.Exists(projectRoot)) projectRoot = Path.GetDirectoryName(projectRoot);
                var path = Path.Combine(projectRoot, ".codeassist", "snippets.tpl");
                try { if (File.Exists(path)) templates.AddRange(SnippetTemplate.Parse(File.ReadAllText(path))); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            CaptureEnvironment();
            templates = templates.Select(template => template.WithEnvironment(name => environment.TryGetValue(name, out var value) ? value : "")).ToList();
            var reserved = SnippetReservedValues.Create(file, selection.GetText(), DateTime.Now, System.Globalization.CultureInfo.CurrentCulture);
            SnippetReservedValues.AddWorkspace(reserved, projectFile, solutionFile, Guid.NewGuid(), System.Globalization.CultureInfo.CurrentCulture);
            var clipboardCaptured = false;
            var panel = new DockPanel { Margin = new Thickness(12) };
            var choice = new ComboBox { ItemsSource = templates, Margin = new Thickness(0, 0, 0, 10) };
            var fields = new StackPanel();
            var preview = new TextBox { IsReadOnly = true, AcceptsReturn = true, FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            var insert = new Button { Content = "삽입", IsDefault = true, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(15, 5, 15, 5) };
            DockPanel.SetDock(choice, Dock.Top); panel.Children.Add(choice);
            var fieldsHost = new ScrollViewer { Content = fields, MaxHeight = 250, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            DockPanel.SetDock(fieldsHost, Dock.Top); panel.Children.Add(fieldsHost);
            DockPanel.SetDock(insert, Dock.Bottom); panel.Children.Add(insert); panel.Children.Add(preview);
            var dialog = new Window { Title = "CodeAssist: Snippets", Content = panel, Width = 650, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            new System.Windows.Interop.WindowInteropHelper(dialog).Owner = owner;
            var inputs = new Dictionary<string, TextBox>(StringComparer.Ordinal);
            Func<SnippetExpansion> expand = () =>
            {
                var values = new Dictionary<string, string>(reserved, StringComparer.Ordinal);
                foreach (var pair in inputs) values[pair.Key] = pair.Value.Text;
                var template = (SnippetTemplate)choice.SelectedItem;
                var newline = snapshot.GetText().Contains("\r\n") ? "\r\n" : "\n";
                return new SnippetTemplate(template.Title, template.Shortcut, template.Body.Replace("\r\n", "\n").Replace("\n", newline)).Expand(values);
            };
            choice.SelectionChanged += (s, e) =>
            {
                inputs.Clear(); fields.Children.Clear();
                if (((SnippetTemplate)choice.SelectedItem).Variables().Contains("clipboard") && !clipboardCaptured)
                {
                    try { reserved["clipboard"] = Clipboard.ContainsText() ? Clipboard.GetText() : ""; clipboardCaptured = true; }
                    catch (System.Runtime.InteropServices.ExternalException) { reserved["clipboard"] = ""; }
                }
                var inputFields = ((SnippetTemplate)choice.SelectedItem).Inputs(reserved);
                insert.IsEnabled = inputFields.Length <= 8;
                if (inputFields.Length > 8) fields.Children.Add(new TextBlock { Text = "입력 자리표시자는 최대 8개까지 지원합니다." });
                foreach (var field in inputFields)

                {
                    var name = field.Name;
                    fields.Children.Add(new TextBlock { Text = name });
                    var input = new TextBox { Text = field.DefaultValue ?? "", Margin = new Thickness(0, 2, 0, 7) }; inputs[name] = input; fields.Children.Add(input);
                    input.TextChanged += (sender, args) => preview.Text = expand().Text;
                }
                preview.Text = expand().Text;
            };
            insert.Click += (s, e) => dialog.DialogResult = true;
            dialog.PreviewKeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Escape) dialog.DialogResult = false; };
            choice.SelectedIndex = 0;
            if (dialog.ShowDialog() != true || view.IsClosed) return;
            if (!ReferenceEquals(snapshot, view.TextSnapshot))
            {
                MessageBox.Show("파일 내용이 변경되었습니다. 삽입할 위치를 다시 선택해 주세요.", "CodeAssist"); return;
            }
            var result = expand();
            using (var edit = view.TextBuffer.CreateEdit())
            {
                if (!edit.Replace(selection.Span, result.Text)) return;
                var updated = edit.Apply();
                view.Selection.Clear(); view.Caret.MoveTo(new SnapshotPoint(updated, selection.Start.Position + result.Caret));
            }
        }
    }
}
