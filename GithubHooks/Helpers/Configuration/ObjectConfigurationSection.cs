using System;
using System.Configuration;
using System.IO;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace GithubHooks.Configuration
{
    /// <summary>
    /// A generic configuration section handler that deserializes any
    /// object and supports external configuration sections.
    /// </summary>
    public class ObjectConfigurationSection : IConfigurationSectionHandler
    {
        //Public methods
        #region IConfigurationSectionHandler Members
        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        /// <param name="parent">Parent object.</param>
        /// <param name="configContext">Configuration context object.</param>
        /// <param name="section">Section XML node.</param>
        /// <returns>The created section handler object.</returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            XmlNode node = section.Attributes.RemoveNamedItem("file");
            if (node == null || node.Value.Length == 0)
            {
                //No file attribute, so the object must be serialized inline
                return CreateStatic(parent, section);
            }

            //Find the path to the external file, default to the current directory
            string path = Directory.GetCurrentDirectory();
            HttpConfigurationContext httpConfigContext = configContext as HttpConfigurationContext;
            if (httpConfigContext != null)
            {
                path = httpConfigContext.VirtualPath + node.Value;
                path = System.Web.HttpContext.Current.Server.MapPath(path);
            }

            if (!File.Exists(path))
            {
                throw new ConfigurationErrorsException("Unable to locate external configuration file " + path, section);
            }

            ConfigXmlDocument document = new ConfigXmlDocument();
            document.Load(path);
            return CreateStatic(parent, document.DocumentElement);
        }
        #endregion

        //Internal methods
        #region CreateStatic()
        /// <summary>
        /// Responsible for the actual deserialization of an configuration
        /// section into a class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="section">The section.</param>
        /// <returns>An deserialized configuration section</returns>
        internal static object CreateStatic(object parent, XmlNode section)
        {
            try
            {
                //Retrieve the type name and deserialize the section
                Type type = Type.GetType(section.Attributes["type"].Value);
                XmlSerializer serializer = new XmlSerializer(type);
                return serializer.Deserialize(new XmlNodeReader(section));
            }
            catch
            {
                throw new ConfigurationErrorsException("Unable to create object configuration section", section);
            }
        }
        #endregion
    }
}