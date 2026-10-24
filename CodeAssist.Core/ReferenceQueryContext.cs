using System;
namespace CodeAssist.Core
{
    /// <summary>Identifies the solution and index that own a reference query.</summary>
    public sealed class ReferenceQueryContext
    {
        private readonly string solution;
        private readonly string indexRoot;
        public ReferenceQueryContext(string solution, string indexRoot)
        {
            this.solution = solution ?? string.Empty;
            this.indexRoot = indexRoot ?? string.Empty;
        }
        public bool Matches(string currentSolution, string currentIndexRoot) =>
            string.Equals(solution, currentSolution ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(indexRoot, currentIndexRoot ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
