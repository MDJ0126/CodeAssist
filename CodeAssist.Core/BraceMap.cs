using System;
using System.Collections.Generic;
namespace CodeAssist.Core
{
    public sealed class BraceMap
    {
        private readonly Dictionary<int,int> pairs=new Dictionary<int,int>();
        public BraceMap(string text)
        {
            var code=CppCodeMask.Create(text??"");var stack=new Stack<int>();
            for(var i=0;i<code.Length;i++)
            {
                if("([{ ".IndexOf(code[i])>=0&&code[i]!=' ')stack.Push(i);
                else if(")] }".IndexOf(code[i])>=0&&code[i]!=' ')
                {
                    if(stack.Count>0&&Closing(code[stack.Peek()])==code[i]){var open=stack.Pop();pairs[open]=i;pairs[i]=open;}
                    else {pairs[i]=-1;while(stack.Count>0)pairs[stack.Pop()]=-1;}
                }
            }
            while(stack.Count>0)pairs[stack.Pop()]=-1;
        }
        private static char Closing(char value)=>value=='('?')':value=='['?']':'}';
        public bool At(int position,out int partner)=>pairs.TryGetValue(position,out partner);
    }
}
