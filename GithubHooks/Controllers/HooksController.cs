using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Web.Http;
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
        private const string baseUrl = "https://api.github.com/";
        private const string pullRequestBase = baseUrl + "repos/mattjbrown/test-hooks/pulls";
        private const string pullRequestMerge = pullRequestBase + "/{0}/merge";
        private const string deleteBranch = baseUrl + "repos/mattjbrown/test-hooks/git/refs/heads/{0}";


        [Route("hook")]
        [HttpPost]
        public IHttpActionResult ProcessHook(IssueCommentEvent commentEvent)
        {
            var creds = new InMemoryCredentialStore(new Credentials("cb844264d27992bd3e3c8f867c5dce039e86497f"));
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
                var comment = new PostableComment()
                {
                    Body = string.Format("Zhenbot™ was unable to create Pull Request for {0}. Sorry about that :person_frowning:. Exception: {1}", branchName, e)
                };

                var finalComment = github.Issue.Comment.Create("mattjbrown", "test-hooks", issueNumber, JsonConvert.SerializeObject(comment, settings)).Result;
                return BadRequest();
            }

            var merge = new Merge()
            {
                CommitMessage = "Auto-merging pull request. Beep Boop."
            };

            var mergeResult = new MergeResult()
            {
                Merged = false
            };

            var mergeUrl = string.Format(pullRequestMerge, pullReqNumber);

            try
            {
                mergeResult = apiConnection.Put<MergeResult>(new Uri(mergeUrl), JsonConvert.SerializeObject(merge, settings)).Result;
            }
            catch (Exception e)
            {
                var comment = new PostableComment()
                {
                    Body = string.Format("Zhenbot™ was unable to merge Pull Request #{0} for {1}. Sorry about that :person_frowning:. Exception: {2}.", pullReqNumber, mergeUrl, e)
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
