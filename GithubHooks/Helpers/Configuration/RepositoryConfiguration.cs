using System;

namespace GithubHooks.Configuration
{
    [Serializable]
    public class RepositoryConfiguration : ObjectConfigurationSection
    {
        public string Owner;
        public string RepoName;
    }
}