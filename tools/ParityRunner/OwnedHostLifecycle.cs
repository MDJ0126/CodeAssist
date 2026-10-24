using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
internal static class OwnedHostLifecycle
{
    [DllImport("ole32.dll")]private static extern int GetRunningObjectTable(int reserved,out IRunningObjectTable table);
    [DllImport("ole32.dll")]private static extern int CreateBindCtx(int reserved,out IBindCtx context);
    [STAThread]private static int Main(string[] args)
    {
        if(args.Length<2)return 2;
        int pid;if(!int.TryParse(args[0],out pid))return 2;
        var root=Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!Directory.Exists(root)||!root.EndsWith(Path.DirectorySeparatorChar+"parity-runs"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return 3;
        IRunningObjectTable table;Marshal.ThrowExceptionForHR(GetRunningObjectTable(0,out table));
        IEnumMoniker enumerator;table.EnumRunning(out enumerator);var monikers=new IMoniker[1];
        while(enumerator.Next(1,monikers,IntPtr.Zero)==0)
        {
            IBindCtx context;CreateBindCtx(0,out context);string name;monikers[0].GetDisplayName(context,null,out name);
            if(!name.Equals("!VisualStudio.DTE.17.0:"+pid,StringComparison.OrdinalIgnoreCase))continue;
            object instance;table.GetObject(monikers[0],out instance);dynamic dte=instance;
            string solution=dte.Solution.FullName;
            if(string.IsNullOrEmpty(solution)||!Path.GetFullPath(solution).StartsWith(root,StringComparison.OrdinalIgnoreCase)){Console.WriteLine("Refusing unrelated solution");return 4;}
            foreach(dynamic document in dte.Documents)
            {
                string file=document.FullName;
                bool welcome=file.Equals(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CodeAssist","Welcome.codeassist"),StringComparison.OrdinalIgnoreCase);
                if(!welcome&&!Path.GetFullPath(file).StartsWith(root,StringComparison.OrdinalIgnoreCase)){Console.WriteLine("Refusing unrelated open document");return 5;}
            }
            if(args.Length==3&&args[2]=="describe"){Console.WriteLine(solution);return 0;}
            if(args.Length==3&&args[2]=="options"){dte.ExecuteCommand("VAssistX.Options");return 0;}
            if(args.Length==3){string boot=Path.GetFullPath(args[2]);if(!boot.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(boot))return 7;dte.ItemOperations.OpenFile(boot);Console.WriteLine("Opened owned bootstrap");return 0;}
            dte.ExecuteCommand("File.SaveAll");
            dte.Quit();
            Console.WriteLine("Saved and requested graceful exit of owned fixture host "+pid);return 0;
        }
        Console.WriteLine("DTE instance not found");return 6;
    }
}
