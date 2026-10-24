using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
namespace CodeAssist.Extension
{
    // ABI verified against Windows SDK 10.0.22621.0 um/spellcheck.h.
    internal sealed class WindowsSpelling : IDisposable
    {
        [ComImport,Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFactory { void Languages(out IEnumString value);void IsSupported([MarshalAs(UnmanagedType.LPWStr)]string language,[MarshalAs(UnmanagedType.Bool)]out bool value);void Create([MarshalAs(UnmanagedType.LPWStr)]string language,out IChecker value); }
        [ComImport,Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IChecker { void Language([MarshalAs(UnmanagedType.LPWStr)]out string value);void Check([MarshalAs(UnmanagedType.LPWStr)]string text,out IErrors value);void Suggest([MarshalAs(UnmanagedType.LPWStr)]string text,out IEnumString value); }
        [ComImport,Guid("803E3BD4-2828-4410-8290-418D1D73C762"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IErrors { [PreserveSig]int Next(out IError value); }
        [ComImport,Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IError { void Start(out uint value);void Length(out uint value);void Action(out int value);void Replacement([MarshalAs(UnmanagedType.LPWStr)]out string value); }
        private IChecker checker;
        internal WindowsSpelling(string language)
        {
            object factory=null;
            try { factory=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")));var typed=(IFactory)factory;typed.IsSupported(language,out var supported);if(supported)typed.Create(language,out checker); }
            finally { Release(factory); }
        }
        // null means unsupported/unavailable, empty means no error. Never treat unavailable as a clean result.
        internal string Check(string word)
        {
            if(checker==null)return null;IErrors errors=null;IError error=null;IEnumString suggestions=null;
            try {
                checker.Check(word,out errors);var result=errors.Next(out error);if(result<0)Marshal.ThrowExceptionForHR(result);if(result!=0||error==null)return "";
                checker.Suggest(word,out suggestions);var items=new string[3];var fetched=Marshal.AllocCoTaskMem(4);
                try {suggestions.Next(3,items,fetched);var count=Marshal.ReadInt32(fetched);return "Possible spelling: "+string.Join(", ",items,0,count);}
                finally {Marshal.FreeCoTaskMem(fetched);}
            }
            finally {Release(suggestions);Release(error);Release(errors);}
        }
        private static void Release(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
        public void Dispose(){Release(checker);checker=null;}
    }
}
