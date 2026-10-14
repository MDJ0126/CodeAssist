using System;
using System.Collections.Generic;

namespace CodeAssist.Core
{
    public static class CompletionSelection
    {
        public static int Validate(string selectedIdentity, int index, IReadOnlyList<string> identities)
        {
            if (index < 0) return index;
            if (index < identities.Count && string.Equals(identities[index], selectedIdentity, StringComparison.Ordinal)) return index;
            for (var i = 0; i < identities.Count; i++)
                if (string.Equals(identities[i], selectedIdentity, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
