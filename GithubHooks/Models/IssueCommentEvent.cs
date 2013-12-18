using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Octokit;

namespace GithubHooks.Models
{
    public class IssueCommentEvent
    {
        public string Action;
        public Issue Issue;
        public IssueComment Comment;
    }
}