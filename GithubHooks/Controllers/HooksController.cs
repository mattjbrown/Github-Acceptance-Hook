using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Web.Http;
using GithubHooks.Configuration;
using GithubHooks.Helpers;
using GithubHooks.Models;
using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;
using WebApplication1.Models;
using PullRequest= GithubHooks.Models.PullRequest;

namespace GithubHooks.Controllers
{
    public class HooksController : ApiController
    {
        private static string apiKey = ConfigurationManager.ApiCredentialsConfig.Key;
        private static string owner = ConfigurationManager.RepositoryConfig.Owner;
        private static string repoName = ConfigurationManager.RepositoryConfig.RepoName;

        private const string baseUrl = "https://api.github.com";
        private static string pullRequestBase = string.Format("{0}/repos/{1}/{2}/pulls", baseUrl, owner, repoName);
        private static string pullRequestMerge = pullRequestBase + "/{0}/merge";
        private static string deleteBranch = string.Format("{0}/repos/{1}/{2}/git/refs/heads", baseUrl, owner, repoName) + "/{0}";


        [Route("hook")]
        [HttpPost]
        public IHttpActionResult ProcessHook(IssueCommentEvent commentEvent)
        {
            var creds = new InMemoryCredentialStore(new Credentials(apiKey));
            var headerVal = new ProductHeaderValue("GitHooks");
            var github = new GitHubClient(headerVal, creds);
            var apiConnection = new ApiConnection(new Connection(headerVal, creds));

            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new LowercaseContractResolver()
            };

            if (commentEvent != null)
            {
                if (commentEvent.Comment.User.Login.Equals("mattjbrown") && checkComment(commentEvent.Comment.Body))
                {
                    var branchName = getBranchNameFromComment(commentEvent.Comment.Body);

                    PullAndMerge(branchName, commentEvent.Issue.Number, commentEvent.Issue.Title, apiConnection, github,
                        settings);
                }
            }

            return Ok();
        }

        private IHttpActionResult PullAndMerge(string branchName, int issueNumber, string issueTitle,
            ApiConnection apiConnection, GitHubClient github, JsonSerializerSettings settings)
        {
            var pullReq = new PullRequest()
            {
                Base = "master",
                Head = branchName,
                Title = string.Format("#{0} - {1}", issueNumber, issueTitle),
                Body = "Pull Request Auto-Created by Zhenbot™"
            };

            object pullReqNumber = null;

            try
            {
                pullReqNumber = apiConnection.Post<Dictionary<string, object>>(new Uri(pullRequestBase), JsonConvert.SerializeObject(pullReq, settings)).Result["number"];
            }
            catch (Exception e)
            {
                var aggregateException = e as AggregateException;
                if (aggregateException != null)
                {
                    var apiException = aggregateException.GetBaseException() as ApiException;
                    if (apiException != null && apiException.Message.Equals("Pull Request is not mergeable"))
                    {
                        
                    }
                }

                var comment = new PostableComment()
                {
                    Body = string.Format("Zhenbot™ was unable to create Pull Request for {0}. Sorry about that :person_frowning:. Exception: {1}", branchName, e)
                };

                var finalComment = github.Issue.Comment.Create("mattjbrown", "test-hooks", issueNumber, JsonConvert.SerializeObject(comment, settings)).Result;
                return BadRequest();
            }

            MergeResult mergeResult;

            try
            {
                mergeResult = MergePullRequest(pullReqNumber, apiConnection, settings, true);
            }
            catch (Exception e)
            {
                var comment = new PostableComment()
                {
                    Body = string.Format("Zhenbot™ was unable to merge Pull Request #{0} for {1}. Sorry about that :person_frowning:. Exception: {2}.", pullReqNumber, branchName, e)
                };

                var finalComment = github.Issue.Comment.Create("mattjbrown", "test-hooks", issueNumber, JsonConvert.SerializeObject(comment, settings)).Result;
                return BadRequest();
            }

            if (mergeResult.Merged)
            {
                apiConnection.Delete(new Uri(string.Format(deleteBranch, branchName)));

                var comment = new PostableComment()
                {
                    Body = string.Format("Pulled (#{0}) and deleted {1} :ok_woman:. Zhenbot™ signing off.", pullReqNumber, branchName)
                };

                var finalComment = github.Issue.Comment.Create("mattjbrown", "test-hooks", issueNumber, JsonConvert.SerializeObject(comment, settings)).Result;
            }
            else
            {
                var comment = new PostableComment()
                {
                    Body = string.Format("Zhenbot™ was unable to merge Pull Request #{0} for {1}. Sorry about that :person_frowning:.", pullReqNumber, branchName)
                };

                var finalComment = github.Issue.Comment.Create("mattjbrown", "test-hooks", issueNumber, JsonConvert.SerializeObject(comment, settings)).Result;
            }

            return Ok();
        }

        private MergeResult MergePullRequest(object pullReqNumber, ApiConnection apiConnection, JsonSerializerSettings settings, bool tryAgain)
        {
            var merge = new Merge()
            {
                CommitMessage = "Auto-merging pull request. Beep Boop."
            };

            var mergeUrl = string.Format(pullRequestMerge, pullReqNumber);

            try
            {
                return apiConnection.Put<MergeResult>(new Uri(mergeUrl), JsonConvert.SerializeObject(merge, settings)).Result;
            }
            catch (AggregateException e)
            {
                var apiException = e.GetBaseException() as ApiException;
                if (apiException != null && apiException.Message.Equals("Pull Request is not mergeable") && tryAgain)
                {
                    //naive sleep, I think the problem is with trying to merge IMMEDIATELY
                    Thread.Sleep(5000);
                    return MergePullRequest(pullReqNumber, apiConnection, settings, false);
                }
                else
                {
                    throw e;
                }
            }

            return null;
        }

        private string getBranchNameFromComment(string comment)
        {
            string[] split = { ":accept:" };

            return comment.Split(split, StringSplitOptions.None)[1].Trim();
        }

        private bool checkComment(string comment)
        {
            return comment.Contains(":accept:");
        }
    }
}
