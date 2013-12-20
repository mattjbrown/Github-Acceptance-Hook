using System;
using System.Xml.Serialization;

namespace GithubHooks.Configuration
{
    [Serializable]
    public class AllowedMergersConfiguration : ObjectConfigurationSection
    {
        [XmlArray("Mergers")]
        [XmlArrayItem(typeof(string), ElementName = "User")]
        public string[] Mergers
        {
            get; set;
        }
    }
}