using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Octokit;

namespace GithubHooks.Models
{
    public class Merge
    {
        [JsonProperty("commit_message")] 
        public string CommitMessage;
    }
}