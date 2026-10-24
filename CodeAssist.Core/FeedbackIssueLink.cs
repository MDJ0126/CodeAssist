using System;
namespace CodeAssist.Core
{
    public static class FeedbackIssueLink
    {
        public static string Create(string version,string feedback)
        {
            var title="CodeAssist feedback";
            var body="## Feedback\n"+(feedback??string.Empty)+"\n\n## Version\nCodeAssist "+(version??string.Empty);
            return "https://github.com/MDJ0126/CodeAssist/issues/new?title="+Uri.EscapeDataString(title)+"&body="+Uri.EscapeDataString(body);
        }
    }
}