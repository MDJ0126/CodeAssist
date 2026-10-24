using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeAssist.Core
{
    public static class CompletionFilterSelection
    {
        public static bool[] Toggle(IReadOnlyList<bool> current,int index,int anchor,bool control,bool shift)
        {
            if(index<0||index>=current.Count)throw new ArgumentOutOfRangeException(nameof(index));
            var result=current.ToArray();
            result[index]=!result[index];
            if(shift&&anchor>=0&&anchor<result.Length)
                for(var i=Math.Min(index,anchor);i<=Math.Max(index,anchor);i++)result[i]=true;
            else if(!control)
                for(var i=0;i<result.Length;i++)if(i!=index)result[i]=false;
            return result;
        }
    }
}