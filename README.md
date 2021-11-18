# Post Comment to Markdown Pull Request Azure Function

An Azure Function App that receives HTTP form posts containing a comment for a blog and turns them into pull request against your GitHub repository as part of the [nuxt-blog-comments](https://github.com/damieng/nuxt-blog-comments) system.

The app includes two functions:

- `PostComment` - receives form POST submission and creates a PR to add the comment to your Jekyll site
- `Preload` - consider calling this from inside your textarea's change event notificiation to warm up the function

## Setup

To set this up, you'll need to have an [Azure Portal account](https://portal.azure.com).

1. Fork this repository
2. [Create a **v3** Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function)
3. [Set up your function to deploy from your fork](https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-create-function-app-github-continuous)
4. Set up the following [App Settings for your Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings)

| Setting                                           | Value                                                                                                                                                                                       |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PostCommentSettings__PullRequestRepository`      | `owner/name` of the repository that houses your Jekyll site for pull requests to be created against. For example, `damieng/damieng.com` will post to https://github.com/damieng/damieng.com |
| `PostCommentSettings__GitHubToken`                | A [GitHub personal access token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/) with access to edit your target repository.                       |
| `PostCommentSettings__CommentWebsiteUrl`          | The URL to the website that hosts the comments. This is used to make sure the correct site is posting comments to the receiver.                                                             |
| `PostCommentSettings__CommentFallbackCommitEmail` | The email address to use for GitHub commits and PR's if the form does not supply one.                                                                                                       |
