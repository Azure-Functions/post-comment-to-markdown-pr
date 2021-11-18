using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace PostCommentToMarkdownPR
{
    public class PostCommentToMarkdownPR
    {
        private static readonly string[] reservedFilenames = new[]
            { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        private readonly PostCommentSettings settings;

        public PostCommentToMarkdownPR(IOptions<PostCommentSettings> settings)
        {
            if (settings == null)
                ArgumentNullException.ThrowIfNull(settings);
            this.settings = settings.Value;
        }

        [FunctionName("PostComment")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, ILogger log)
        {
            var form = req.Form;

            // Bail if the settings are not configured correctly
            if (!settings.IsValid(out string settingsErrors))
                return new BadRequestErrorMessageResult(settingsErrors);

            // Bail if we are being told to redirect somewhere we should not (no open relay)
            var redirect = form["redirect"];
            if (!String.IsNullOrEmpty(redirect) && !redirect.ToString().StartsWith(settings.CommentWebsiteUrl))
                return new BadRequestErrorMessageResult($"This Azure comments receiver is not permitted to redirect to '{redirect}'.");

            // Make sure the site posting the comment is the correct site.
            var postedSite = form["comment-site"];
            if (String.IsNullOrWhiteSpace(postedSite))
                return new BadRequestErrorMessageResult("This Azure comments receiever is set to only allow specific sites and no 'comment-site' form value as provided.");
            if (!AreSameSites(settings.CommentWebsiteUrl, postedSite))
                return new BadRequestErrorMessageResult($"This Azure comments receiever does not handle forms for '{postedSite}'. You should point to your own instance.");

            // Ensure the form is valid and we have everything we need
            if (!Comment.TryCreateFromForm(form, out var comment, out var errors))
                return new BadRequestErrorMessageResult(String.Join("\n", errors));

            // Don't let people create folders you can't check out on Windows
            if (reservedFilenames.Contains(comment.post_id))
                return new BadRequestErrorMessageResult("This Azure comments receiver prohibits post_ids that use reserved Windows filenames.");

            await CreatePullRequest(comment);

            // Redirect if we were told to
            if (!Uri.TryCreate(form["redirect"], UriKind.Absolute, out var redirectUri))
                return new OkResult();

            return new RedirectResult(redirectUri.OriginalString);
        }

        [FunctionName("Preload")]
        public static IActionResult Preload([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            return new OkResult();
        }

        private static bool AreSameSites(string commentSite, string postedCommentSite)
        {
            return Uri.TryCreate(commentSite, UriKind.Absolute, out var commentSiteUri)
                && Uri.TryCreate(postedCommentSite, UriKind.Absolute, out var postedCommentSiteUri)
                && commentSiteUri.Host.Equals(postedCommentSiteUri.Host, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<PullRequest> CreatePullRequest(Comment comment)
        {

            // Create the Octokit client
            var github = new GitHubClient(new ProductHeaderValue("PostCommentToPullRequest"),
                new Octokit.Internal.InMemoryCredentialStore(new Credentials(settings.GitHubToken)));

            // Get a reference to our GitHub repository
            var repoOwnerName = settings.PullRequestRepository.Split('/');
            var repo = await github.Repository.Get(repoOwnerName[0], repoOwnerName[1]);

            // Create a new branch from the default branch
            var defaultBranch = await github.Repository.Branch.Get(repo.Id, repo.DefaultBranch);
            var newBranch = await github.Git.Reference.Create(repo.Id, new NewReference($"refs/heads/comment-{comment.id}", defaultBranch.Commit.Sha));

            // Create a new file with the comments in it
            var fileRequest = new CreateFileRequest($"Comment by {comment.name} on {comment.post_id}", comment.ToContent(), newBranch.Ref)
            {
                Committer = new Committer(comment.name, comment.email ?? settings.CommentFallbackCommitEmail ?? "redacted@example.com", comment.date)
            };
            await github.Repository.Content.CreateFile(repo.Id, $"content/{comment.post_id}/{comment.id}.md", fileRequest);

            // Create a pull request for the new branch and file
            return await github.Repository.PullRequest.Create(repo.Id, new NewPullRequest(fileRequest.Message, newBranch.Ref, defaultBranch.Name)
            {
                Body = $"avatar: <img src=\"{comment.avatar}\" width=\"64\" height=\"64\" />\n\n{comment.message}"
            });
        }
    }
}
