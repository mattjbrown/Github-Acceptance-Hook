using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http.Headers;
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
            var creds = new InMemoryCredentialStore(new Credentials("bc9f717ef4f0a3b9f2ec30a988ea21f08787f535"));
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

                    var pullReq = new PullRequest()
                    {
                        Base = "master",
                        Head = branchName,
                        Title = string.Format("#{0} - {1}", commentEvent.Issue.Number, commentEvent.Issue.Title),
                        Body = "Pull Request Auto-Created by Acceptbot™"
                    };

                    var pullReqNumber = apiConnection.Post<Dictionary<string, object>>(new Uri(pullRequestBase), JsonConvert.SerializeObject(pullReq, settings)).Result["number"];

                    var merge = new Merge()
                    {
                        CommitMessage = "Auto-merging pull request. Beep Boop."
                    };

                    var mergeResult = apiConnection.Put<MergeResult>(new Uri(string.Format(pullRequestMerge, pullReqNumber)), JsonConvert.SerializeObject(merge, settings)).Result;

                    if (mergeResult.Merged)
                    {
                        apiConnection.Delete(new Uri(string.Format(deleteBranch, branchName)));

                        var comment = new PostableComment()
                        {
                            Body = string.Format("Pulled (#{0}) and deleted {1} :ok_woman:. Acceptbot™ signing off.", pullReqNumber, branchName)
                        };

                        var result3 = github.Issue.Comment.Create("mattjbrown", "test-hooks", commentEvent.Issue.Number, JsonConvert.SerializeObject(comment, settings)).Result;
                    }
                    else
                    {
                        var comment = new PostableComment()
                        {
                            Body = string.Format("Acceptbot™ was unable to merge Pull Request #{0} for {1}. Sorry about that :person_frowning:.", pullReqNumber, branchName)
                        };

                        var result3 = github.Issue.Comment.Create("mattjbrown", "test-hooks", commentEvent.Issue.Number, JsonConvert.SerializeObject(comment, settings)).Result;
                    }
                }
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
