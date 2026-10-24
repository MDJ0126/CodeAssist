using System;using CodeAssist.Extension;
class Program { [STAThread]static int Main(){using(var c=new WindowsSpelling("en-US")){foreach(var word in new[]{"mispellng","wrod","return","correction"}){var value=c.Check(word);Console.WriteLine(word+"="+(value??"UNSUPPORTED"));if(value==null||((word=="mispellng"||word=="wrod")!=(value.Length>0)))return 1;}}return 0;} }
