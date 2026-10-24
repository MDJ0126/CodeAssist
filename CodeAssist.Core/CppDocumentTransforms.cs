using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace CodeAssist.Core
{
    public static class CppDocumentTransforms
    {
        public static EditorTextChange ReorderParameters(string text,int identifierOffset,int[] order,bool declaration)
        {
            if(text==null||identifierOffset<0||identifierOffset>=text.Length)throw new ArgumentException("Invalid symbol location");
            var code=CppCodeMask.Create(text);var end=identifierOffset;while(end<code.Length&&(char.IsLetterOrDigit(code[end])||code[end]=='_'))end++;while(end<code.Length&&char.IsWhiteSpace(code[end]))end++;
            if(end>=code.Length||code[end]!='(')throw new NotSupportedException("Function pointers, macros and non-call references are unsupported.");
            var map=new BraceMap(code);if(!map.At(end,out var close)||close<0)throw new ArgumentException("Invalid parameter list");
            var arguments=CppRefactoring.SplitArguments(text.Substring(end+1,close-end-1));if(order==null||order.Length!=arguments.Length||order.Distinct().Count()!=order.Length||order.Any(i=>i<0||i>=arguments.Length))throw new ArgumentException("Parameter order must contain every parameter exactly once; calls using omitted defaults are unsupported.");
            var sorted=order.Select(i=>arguments[i]).ToArray();
            if(declaration){var seenDefault=false;foreach(var value in sorted){if(value.Contains("="))seenDefault=true;else if(seenDefault)throw new ArgumentException("A parameter without a default cannot follow one with a default.");}}
            var replacement=string.Join(", ",sorted);return new EditorTextChange(end+1,close-end-1,replacement,end+1,replacement.Length);
        }
        public static string ConvertLocalPointer(string text,int declarationStart,int declarationLength)
        {
            if(text==null||declarationStart<0||declarationLength<=0||declarationStart>text.Length-declarationLength)throw new ArgumentException("Select a complete local declaration");
            var selected=text.Substring(declarationStart,declarationLength);
            var m=Regex.Match(selected,@"^\s*(?<type>[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*)\s*(?<pointer>\*)?\s+(?<name>[A-Za-z_]\w*)\s*(?<tail>[^;]*)\s*;\s*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
            if(!m.Success)throw new NotSupportedException("Select a simple non-template local object or owning new-expression pointer declaration.");
            var type=m.Groups["type"].Value;var name=m.Groups["name"].Value;var context=new CppReferenceContext(text);var declared=declarationStart+m.Groups["name"].Index;
            if(!context.IsExecutableAt(declared)||context.DeclarationAt(name,declared)!=declared)throw new NotSupportedException("Only a resolved local variable is supported.");
            var pointer=m.Groups["pointer"].Success;var edits=new List<EditorTextChange>();
            foreach(var offset in context.References(name,declared).Where(p=>p!=declared))
            {
                var next=offset+name.Length;while(next<context.Code.Length&&char.IsWhiteSpace(context.Code[next]))next++;
                if(pointer){if(next+1>=context.Code.Length||context.Code.Substring(next,2)!="->")throw new NotSupportedException("Only member-access uses are supported; pointer escapes, delete and arithmetic must be handled separately.");edits.Add(new EditorTextChange(next,2,".",0,0));}
                else {if(next>=context.Code.Length||context.Code[next]!='.')throw new NotSupportedException("Only member-access uses are supported; address-taking and argument uses require semantic conversion.");edits.Add(new EditorTextChange(next,1,"->",0,0));}
            }
            string replacement;
            if(pointer)
            {
                var init=Regex.Match(m.Groups["tail"].Value,@"^\s*=\s*new\s+"+Regex.Escape(type)+@"\s*(?<init>\([^;]*\)|\{[^;]*\})?\s*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
                if(!init.Success)throw new NotSupportedException("Only a direct new-expression initializer can become an instance.");
                var initializer=init.Groups["init"].Value;if(initializer=="()")initializer="{}";
                replacement=type+" "+name+initializer+";";
            }
            else
            {
                var storage=name+"Storage";for(var n=2;Regex.IsMatch(context.Code,@"\b"+Regex.Escape(storage)+@"\b");n++)storage=name+"Storage"+n;
                var original=selected.Remove(m.Groups["name"].Index,name.Length).Insert(m.Groups["name"].Index,storage).Trim();
                replacement=original+(text.Contains("\r\n")?"\r\n":"\n")+type+"* "+name+" = &"+storage+";";
            }
            edits.Add(new EditorTextChange(declarationStart,declarationLength,replacement,0,0));return PluginEdits.Apply(text,edits);
        }
        public static string ExtractGlobalMethod(string text,int start,int length,string signature)
        {
            if(text==null||start<0||length<=0||start>text.Length-length)throw new ArgumentException("Select complete statements");
            var sig=Regex.Match(signature??"",@"^\s*void\s+(?<name>[A-Za-z_]\w*)\s*\((?<args>.*)\)\s*$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));if(!sig.Success)throw new NotSupportedException("Use a void free-function signature with explicitly supplied reference parameters.");
            var code=CppCodeMask.Create(text);var selected=code.Substring(start,length);
            if(Regex.IsMatch(selected,@"\b(?:return|break|continue|goto|co_return|co_await)\b|^\s*#",RegexOptions.Multiline)||Regex.IsMatch(selected,@"\b[A-Za-z_]\w*\s+[*&]?\s*[A-Za-z_]\w*\s*(?==|;)"))throw new NotSupportedException("Control transfers, preprocessor directives and declarations are unsupported.");
            var name=sig.Groups["name"].Value;if(Regex.IsMatch(code,@"\b"+Regex.Escape(name)+@"\b"))throw new ArgumentException("The extracted method name already appears in the document.");
            var map=new BraceMap(code);var declaration=-1;
            foreach(Match method in Regex.Matches(code,@"(?m)^[ \t]*(?:(?:static|inline)\s+)*(?:[A-Za-z_]\w*(?:::[A-Za-z_]\w*)*\s*[*&]?)\s+[A-Za-z_]\w*\s*\([^;{}]*\)\s*\{",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50)))
            {var brace=method.Index+method.Length-1;if(map.At(brace,out var close)&&brace<start&&start+length<=close){var depth=0;for(var i=0;i<method.Index;i++){if(code[i]=='{')depth++;if(code[i]=='}')depth--;}if(depth==0){declaration=method.Index;break;}}}
            if(declaration<0)throw new NotSupportedException("Select statements inside a global non-template free function; class and namespace methods require compiler extraction.");
            var args=CppRefactoring.SplitArguments(sig.Groups["args"].Value).Select(arg=>{var value=Regex.Match(arg,@"(?<name>[A-Za-z_]\w*)\s*$");if(!value.Success||value.Index==0)throw new ArgumentException("Every extracted parameter must have a type and name");return value.Groups["name"].Value;}).ToArray();
            var nl=text.Contains("\r\n")?"\r\n":"\n";var body=text.Substring(start,length);var function=signature+nl+"{"+nl+body+nl+"}"+nl+nl;
            return text.Remove(start,length).Insert(start,name+"("+string.Join(", ",args)+");").Insert(declaration,function);
        }
    }
}
