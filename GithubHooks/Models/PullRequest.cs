using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Octokit;

namespace GithubHooks.Models
{
    public class PullRequest
    {
        [JsonProperty("title")]
        public string Title;
        [JsonProperty("body")]
        public string Body;
        [JsonProperty("number")]
        public int? Number;
        [JsonProperty("head")]
        public string Head;
        [JsonProperty("base")]
        public string Base;
    }
}