using System;
using System.Text;
using CodeAssist.Indexing;

internal static class Program
{
    private static string Read() => Encoding.UTF8.GetString(Convert.FromBase64String(Console.ReadLine() ?? ""));
    private static int Main()
    {
        try
        {
            var mode = Read(); var root = Read(); var project = Read(); var output = Read();
            var index = new PersistentSymbolIndex {AdditionalSourceExtensions=(Environment.GetEnvironmentVariable("CODEASSIST_SOURCE_EXTENSIONS")??"").Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries)};
            if(int.TryParse(Environment.GetEnvironmentVariable("CODEASSIST_PARSE_CONCURRENCY"),out var concurrency))index.ParseConcurrency=concurrency;
            if (mode == "project") index.BuildProject(root);
            else if (mode == "engine") index.BuildEngine(root, project);
            else throw new ArgumentException("Unknown indexing mode");
            index.Save(output);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
