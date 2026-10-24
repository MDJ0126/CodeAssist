using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
namespace CodeAssist.Core
{
    public sealed class HelpBlock
    {
        public string Kind { get; }
        public string Label { get; }
        public string Text { get; }
        internal HelpBlock(string kind,string label,string text){Kind=kind;Label=label??"";Text=text??"";}
    }
    public sealed class HelpSection
    {
        public string Id { get; }
        public string Title { get; }
        public string[] Paragraphs { get; }
        public HelpBlock[] Blocks { get; }
        internal HelpSection(string id,string title,HelpBlock[] blocks){Id=id;Title=title;Blocks=blocks;Paragraphs=blocks.Where(b=>b.Kind!="heading").Select(b=>(b.Label.Length>0?b.Label+": ":"")+b.Text).ToArray();}
        public override string ToString()=>Title;
    }
    public static class HelpCatalog
    {
        private static readonly Lazy<XDocument> Content=new Lazy<XDocument>(()=>{
            using(var stream=typeof(HelpCatalog).Assembly.GetManifestResourceStream("CodeAssist.Core.Documentation.help.xml"))
            using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))return XDocument.Load(reader);
        });
        public static HelpSection[] Sections(string language)
        {
            var code=string.IsNullOrWhiteSpace(language)||language=="auto"?CultureInfo.CurrentUICulture.Name:language;
            var neutral=code.Split('-')[0];
            return Content.Value.Root.Elements("section").Select(section=>{
                var versions=section.Elements("content").ToArray();
                var chosen=versions.FirstOrDefault(c=>string.Equals((string)c.Attribute("lang"),code,StringComparison.OrdinalIgnoreCase))??versions.FirstOrDefault(c=>string.Equals((string)c.Attribute("lang"),neutral,StringComparison.OrdinalIgnoreCase))??versions.First(c=>(string)c.Attribute("lang")=="en");
                return new HelpSection((string)section.Attribute("id"),(string)chosen.Element("title"),chosen.Elements().Where(e=>e.Name.LocalName!="title").Select(e=>new HelpBlock(e.Name.LocalName,(string)e.Attribute("label"),e.Value)).ToArray());
            }).ToArray();
        }
        public static HelpSection[] Search(string language,string query)=>Sections(language).Where(s=>string.IsNullOrWhiteSpace(query)||s.Title.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0||s.Paragraphs.Any(p=>p.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0)).ToArray();
    }
}
