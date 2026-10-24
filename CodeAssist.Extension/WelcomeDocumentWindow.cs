using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace CodeAssist.Extension
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("df3762d2-3bc0-42e8-8f07-52452369a24d")]
    public sealed class WelcomeDocumentPane : WindowPane
    {
        public WelcomeDocumentPane(IServiceProvider services) : base(services)
        {
            // Factory enforces the VS UI thread; the view itself also supports an owned STA render test.
#pragma warning disable VSTHRD010
            Content = StartupUi.Welcome();
#pragma warning restore VSTHRD010
        }
    }

    // The registered editor factory lets the shell initialize and site both view and data.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(EditorGuid)]
    public sealed class WelcomeEditorFactory : IVsEditorFactory
    {
        public const string EditorGuid = "fcb5ef5d-27a7-43f4-8e97-cfcf48219013";
        internal const string PhysicalView = "Welcome";
        private readonly IServiceProvider services;
        public WelcomeEditorFactory(IServiceProvider services) { this.services = services; }
        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider site) => VSConstants.S_OK;
        public int Close() => VSConstants.S_OK;
        public int MapLogicalView(ref Guid logicalView, out string physicalView)
        {
            WelcomeDocumentWindow.RecordEvent("MapLogicalView " + logicalView);
            physicalView = logicalView == VSConstants.LOGVIEWID_Primary ? PhysicalView : null;
            return logicalView == VSConstants.LOGVIEWID_Primary ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
        }
        public int CreateEditorInstance(uint createFlags, string document, string physicalView,
            IVsHierarchy hierarchy, uint itemId, IntPtr existingData, out IntPtr docView,
            out IntPtr docData, out string caption, out Guid commandUi, out int windowFlags)
        {
            WelcomeDocumentWindow.RecordEvent("CreateEditorInstanceEntered flags=" + createFlags + " physicalView=" + physicalView);
            docView = docData = IntPtr.Zero;
            caption = ""; commandUi = new Guid(EditorGuid);
            windowFlags = (int)(_VSRDTFLAGS.RDT_CantSave | _VSRDTFLAGS.RDT_DontAutoOpen | _VSRDTFLAGS.RDT_DontAddToMRU);
            if ((createFlags & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0 ||
                (!string.IsNullOrEmpty(physicalView) && physicalView != PhysicalView)) return VSConstants.E_INVALIDARG;
            if (existingData != IntPtr.Zero) return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            WelcomeDocumentPane created = null;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                created = new WelcomeDocumentPane(services);
                docView = Marshal.GetIUnknownForObject(created);
                docData = Marshal.GetIUnknownForObject(new WelcomeDocumentData());
                return VSConstants.S_OK;
            }
            catch (Exception error)
            {
                if (docView != IntPtr.Zero) Marshal.Release(docView);
                if (docData != IntPtr.Zero) Marshal.Release(docData);
                docView = docData = IntPtr.Zero;
                created?.Dispose();
                WelcomeDocumentWindow.RecordFailure("CreateEditorInstance", error);
                return Marshal.GetHRForException(error);
            }
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("d019c789-c15b-45b6-950d-bb27a01e43ed")]
    public sealed class WelcomeDocumentData : IVsPersistDocData
    {
        public int GetGuidEditorType(out Guid editor) { editor = new Guid(WelcomeEditorFactory.EditorGuid); return VSConstants.S_OK; }
        public int IsDocDataDirty(out int dirty) { dirty = 0; return VSConstants.S_OK; }
        public int SetUntitledDocPath(string path) => VSConstants.S_OK;
        public int LoadDocData(string document) => VSConstants.S_OK;
        public int SaveDocData(VSSAVEFLAGS flags, out string newDocument, out int cancelled)
        { newDocument = null; cancelled = 1; return VSConstants.E_NOTIMPL; }
        public int Close() => VSConstants.S_OK;
        public int OnRegisterDocData(uint cookie, IVsHierarchy hierarchy, uint itemId) => VSConstants.S_OK;
        public int RenameDocData(uint attributes, IVsHierarchy hierarchy, uint itemId, string newDocument) => VSConstants.E_NOTIMPL;
        public int IsDocDataReloadable(out int reloadable) { reloadable = 0; return VSConstants.S_OK; }
        public int ReloadDocData(uint flags) => VSConstants.E_NOTIMPL;
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("7f5309d2-9e83-4d5b-b784-0ae3188b725c")]
    public sealed class WelcomeDocumentWindow : IVsWindowFrameNotify3, IDisposable
    {
        // Use a stable local marker for the registered Welcome editor.
        internal static string Moniker => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeAssist", "Welcome.codeassist");
        internal static string DiagnosticPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeAssist", "Welcome-host.log");
        private IVsWindowFrame frame;


        internal void Show(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (frame != null)
            {
                if (ErrorHandler.Succeeded(frame.Show())) { RecordEvent("ReusedDocumentFrame type=1"); return; }
                frame = null;

            }
            var shell = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (shell == null) throw new InvalidOperationException("Visual Studio document host is unavailable.");
            var stage = "PrepareWelcomeDocument";
            RecordEvent(stage);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Moniker));
                if (!File.Exists(Moniker)) File.WriteAllText(Moniker, "CodeAssist Welcome document. Managed by CodeAssist.");
                var editor = new Guid(WelcomeEditorFactory.EditorGuid);
                var logicalView = VSConstants.LOGVIEWID_Primary;
                stage = "OpenDocumentWithSpecificEditor";
                RecordEvent(stage);
                VsShellUtilities.OpenDocumentWithSpecificEditor(package, Moniker, editor, logicalView,
                    out var hierarchy, out var itemId, out frame);
                RecordEvent("OpenDocumentWithSpecificEditorReturned frame=" + (frame != null) + " hierarchy=" + (hierarchy != null) + " item=" + itemId);
                if (frame == null) throw new InvalidOperationException("Visual Studio did not create a Welcome document frame.");
                stage = "GetFrameType";
                ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out var frameType));
                if (Convert.ToInt32(frameType) != 1) throw new InvalidOperationException("Welcome requires a document frame.");
                stage = "SetViewHelper";
                ErrorHandler.ThrowOnFailure(frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, this));
                stage = "ShowDocumentFrame";
                ErrorHandler.ThrowOnFailure(frame.Show());
                RecordEvent("CreatedDocumentFrame type=1 editor=" + WelcomeEditorFactory.EditorGuid);
            }
            catch (Exception error)
            {
                RecordFailure(stage, error);
                var failedFrame = frame;
                frame = null;
                try { failedFrame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave); }
                catch (Exception cleanupError) { RecordFailure("CloseFailedFrame", cleanupError); }
                throw;
            }
        }

        private static string EffectiveDiagnosticPath => string.IsNullOrWhiteSpace(StartupUi.Preferences.DiagnosticLogDirectory)?DiagnosticPath:Path.Combine(Environment.ExpandEnvironmentVariables(StartupUi.Preferences.DiagnosticLogDirectory),"Welcome-host.log");
        internal static void RecordEvent(string text)
        {
            if(!StartupUi.Preferences.DiagnosticLoggingEnabled)return;
            var message = DateTime.UtcNow.ToString("O") + " MVID=" + typeof(WelcomeDocumentWindow).Module.ModuleVersionId + " " + text;
            TryActivityLog(message, false);
            try { Directory.CreateDirectory(Path.GetDirectoryName(EffectiveDiagnosticPath)); File.AppendAllText(EffectiveDiagnosticPath, message + Environment.NewLine); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        internal static void RecordFailure(string stage, Exception error)
        {
            var message = DateTime.UtcNow.ToString("O") + " " + stage + " HRESULT=0x" + error.HResult.ToString("X8") + Environment.NewLine + error + Environment.NewLine;
            TryActivityLog(message, true);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EffectiveDiagnosticPath));
                File.AppendAllText(EffectiveDiagnosticPath, message);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void TryActivityLog(string message, bool error)
        {
            try
            {
                if (error) ActivityLog.LogError("CodeAssist Welcome", message);
                else ActivityLog.LogInformation("CodeAssist Welcome", message);
            }
            catch (InvalidOperationException) { } // Logging services may not be available during editor activation.
            catch (COMException) { }
        }

        public int OnClose(ref uint saveOptions)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            frame = null;
            RecordEvent("ClosedDocumentFrame");
            return VSConstants.S_OK;
        }
        public int OnShow(int show) => VSConstants.S_OK;
        public int OnMove(int x, int y, int width, int height) => VSConstants.S_OK;
        public int OnSize(int x, int y, int width, int height) => VSConstants.S_OK;
        public int OnDockableChange(int dockable, int x, int y, int width, int height) => VSConstants.S_OK;

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var closing = frame; frame = null;
            closing?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }
    }
}