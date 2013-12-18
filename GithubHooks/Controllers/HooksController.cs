using System.IO;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Results;
using GithubHooks.Models;
using Octokit;
using Octokit.Internal;

namespace GithubHooks.Controllers
{
    public class HooksController : ApiController
    {
        [Route("hook")]
        [HttpPost]
        public IHttpActionResult ProcessHook(IssueCommentEvent commentEvent)
        {
            var github = new GitHubClient(new ProductHeaderValue("GitHooks"),
                new InMemoryCredentialStore(new Credentials("bc9f717ef4f0a3b9f2ec30a988ea21f08787f535")));

            if (commentEvent != null)
            {
            }

            var thing = github.Issue.Comment.Create("mattjbrown", "test-hooks", 1, "{ \"body\": \"a new comment\"").Result;

            return Ok();
        }
    }
}
