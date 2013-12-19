using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Octokit;

namespace GithubHooks.Models
{
    public class MergeResult
    {
        [JsonProperty("sha")]
        public string Sha;
        [JsonProperty("merged")]
        public bool Merged;
        [JsonProperty("message")]
        public string Message;
    }
}