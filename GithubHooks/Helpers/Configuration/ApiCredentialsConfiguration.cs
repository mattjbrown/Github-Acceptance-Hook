using System;

namespace GithubHooks.Configuration
{
    [Serializable]
    public class ApiCredentialsConfiguration : ObjectConfigurationSection
    {
        public string Key;
    }
}