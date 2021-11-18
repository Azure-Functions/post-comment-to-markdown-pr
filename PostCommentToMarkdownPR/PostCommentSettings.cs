using System;
using System.Text;

namespace PostCommentToMarkdownPR
{
    public class PostCommentSettings
    {
        public string CommentWebsiteUrl { get; set; }
        public string GitHubToken { get; set; }
        public string PullRequestRepository { get; set; }
        public string CommentFallbackCommitEmail { get; set; }

        public bool IsValid(out string errorMessages)
        {
            var errors = new StringBuilder();

            if (String.IsNullOrWhiteSpace(CommentWebsiteUrl))
                errors.AppendFormat("Comment website url not defined in Azure Function setting '{0}'\n", nameof(CommentWebsiteUrl));
            else if (!Uri.TryCreate(CommentWebsiteUrl, UriKind.Absolute, out var _))
                errors.AppendFormat("Comment website url defined in Azure Function setting '{0}' is not a valid absolute url\n", nameof(CommentWebsiteUrl));

            if (String.IsNullOrWhiteSpace(GitHubToken))
                errors.AppendFormat("GitHub token not defined in Azure Function setting '{0}'\n", nameof(GitHubToken));

            if (String.IsNullOrWhiteSpace(PullRequestRepository))
                errors.AppendFormat("Pull request repository not defined in Azure Function setting '{0}'\n", nameof(PullRequestRepository));

            errorMessages = errors.ToString();

            return errors.Length == 0;
        }
    }
}
