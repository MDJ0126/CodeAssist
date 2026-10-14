using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CodeAssist.Core;
using CodeAssist.Semantics;

namespace CodeAssist.Extension
{
    internal static class CompilerRename
    {
        private sealed class Plan
        {
            public string File; public string Before; public string After; public TextReplacement[] Changes;
        }
        internal static async Task<bool> TryAsync(AsyncPackage package, EnvDTE80.DTE2 dte, string root)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (string.IsNullOrEmpty(root)) root = string.IsNullOrEmpty(dte.Solution?.FullName) ? null : Path.GetDirectoryName(dte.Solution.FullName);
            if (root != null && !Directory.Exists(root)) root = Path.GetDirectoryName(root);
            if (string.IsNullOrEmpty(root) || (Environment.GetEnvironmentVariable("CODEASSIST_USE_CLANGD") != "1" &&
                !File.Exists(Path.Combine(root, "compile_commands.json")) && !File.Exists(Path.Combine(root, "compile_flags.txt")))) return false;
            var selection = dte.ActiveDocument?.Selection as EnvDTE.TextSelection;
            var document = dte.ActiveDocument?.Object("TextDocument") as EnvDTE.TextDocument;
            if (selection == null || document == null) return false;
            var file = dte.ActiveDocument.FullName; var text = document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
            var line = selection.ActivePoint.Line; var column = selection.ActivePoint.LineCharOffset;
            var oldName = IdentifierParser.At(selection.ActivePoint.CreateEditPoint().GetLines(line, line + 1), column - 1);
            if (string.IsNullOrEmpty(oldName)) return false;
            var name = new TextBox { Text = oldName, Margin = new Thickness(10) };
            var rename = new Button { Content = "변경 계획 보기", IsDefault = true, Margin = new Thickness(10) };
            var panel = new StackPanel(); panel.Children.Add(name); panel.Children.Add(rename);
            var prompt = new Window { Title = "CodeAssist: Rename", Content = panel, Width = 400, Height = 160, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            new System.Windows.Interop.WindowInteropHelper(prompt).Owner = (IntPtr)dte.MainWindow.HWnd;
            rename.Click += (s, e) => { if (Regex.IsMatch(name.Text, @"^[A-Za-z_]\w*$")) prompt.DialogResult = true; };
            prompt.Loaded += (s, e) => { name.Focus(); name.SelectAll(); };
            if (prompt.ShowDialog() != true || name.Text == oldName) return true;
            var open = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (EnvDTE.Document current in dte.Documents)
            {
                try
                {
                    var currentText = current.Object("TextDocument") as EnvDTE.TextDocument;
                    if (currentText != null && Regex.IsMatch(Path.GetExtension(current.FullName), @"^\.(?:h|hpp|hh|cpp|cc|cxx|inl)$", RegexOptions.IgnoreCase))
                        open[current.FullName] = currentText.StartPoint.CreateEditPoint().GetText(currentText.EndPoint);
                }
                catch (COMException) { }
            }
            var result = await CompilerSemanticService.Instance.QueryAsync("textDocument/rename", root, dte.FullName, file, text, line, column,
                package.DisposalToken, new JObject { ["newName"] = name.Text }, open);
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (result == null) { MessageBox.Show("변경 계획을 만들지 못했습니다. " + CompilerSemanticService.Instance.LastError, "CodeAssist"); return true; }
            var plans = new List<Plan>();
            try
            {
                foreach (var pair in WorkspaceEdits.Files(result))
                {
                    var original = open.TryGetValue(pair.Key, out var contents) ? contents : File.ReadAllText(pair.Key);
                    var changes = WorkspaceEdits.Plan(original, pair.Value);
                    foreach (var change in changes)
                        if (original.Substring(change.Start, change.Length) != oldName) throw new InvalidOperationException("이름 변경 위치와 현재 내용이 일치하지 않습니다: " + pair.Key);
                    plans.Add(new Plan { File = pair.Key, Before = original, After = WorkspaceEdits.Apply(original, pair.Value), Changes = changes });
                }
            }
            catch (Exception error) { MessageBox.Show(error.Message, "CodeAssist"); return true; }
            if (plans.Count == 0) { MessageBox.Show("변경할 참조를 찾지 못했습니다.", "CodeAssist"); return true; }
            var tabs = new TabControl();
            foreach (var plan in plans)
            {
                var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition());
                var before = Preview(plan.Before); var after = Preview(plan.After); Grid.SetColumn(after, 1); grid.Children.Add(before); grid.Children.Add(after);
                tabs.Items.Add(new TabItem { Header = Path.GetFileName(plan.File), ToolTip = plan.File, Content = grid });
            }
            var apply = new Button { Content = "이름 변경 적용", IsDefault = true, Margin = new Thickness(10) };
            var previewPanel = new DockPanel(); DockPanel.SetDock(apply, Dock.Bottom); previewPanel.Children.Add(apply); previewPanel.Children.Add(tabs);
            var preview = new Window { Title = "CodeAssist: " + oldName + " → " + name.Text + " (" + plans.Count + " files)", Content = previewPanel,
                Width = 1100, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            new System.Windows.Interop.WindowInteropHelper(preview).Owner = (IntPtr)dte.MainWindow.HWnd;
            apply.Click += (s, e) => preview.DialogResult = true;
            if (preview.ShowDialog() != true) return true;
            var manager = await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            var components = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var adapters = components?.GetService<IVsEditorAdaptersFactoryService>();
            if (manager == null || adapters == null || dte.UndoContext.IsOpen) { MessageBox.Show("현재 편집 작업이 완료된 후 다시 실행해 주세요.", "CodeAssist"); return true; }
            var edits = new List<ITextEdit>(); var undoOpened = false;
            try
            {
                foreach (var plan in plans)
                {
                    dte.ItemOperations.OpenFile(plan.File, EnvDTE.Constants.vsViewKindTextView);
                    if (manager.GetActiveView(1, null, out var adapter) < 0) throw new InvalidOperationException("편집기를 열지 못했습니다.");
                    var view = adapters.GetWpfTextView(adapter);
                    if (view == null || view.IsClosed || view.TextSnapshot.GetText() != plan.Before) throw new InvalidOperationException("파일이 변경되었습니다: " + plan.File);
                    var edit = view.TextBuffer.CreateEdit(); edits.Add(edit);
                    foreach (var change in plan.Changes)
                        if (!edit.Replace(change.Start, change.Length, change.Text)) throw new InvalidOperationException("수정할 수 없는 위치입니다: " + plan.File);
                }
                dte.UndoContext.Open("CodeAssist Rename", false); undoOpened = true;
                foreach (var edit in edits) edit.Apply();
            }
            catch (Exception error)
            {
                if (undoOpened) dte.UndoContext.SetAborted(); MessageBox.Show(error.Message, "CodeAssist");
            }
            finally
            {
                foreach (var edit in edits) edit.Dispose();
                if (undoOpened) dte.UndoContext.Close();
            }
            return true;
        }
        private static TextBox Preview(string value) => new TextBox { Text = value, IsReadOnly = true, AcceptsReturn = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"), VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
