using System;
using System.Collections.Generic;
using System.Configuration;

namespace exampleWS
{
    public class ConfigSettings
    {
        public ConnectionSection ServerAppearanceConfiguration
        {
            get
            {
                return (ConnectionSection)ConfigurationManager.GetSection("serverSection");
            }
        }

        public ServerAppearanceCollection ServerApperances
        {
            get
            {
                return this.ServerAppearanceConfiguration.ServerElement;
            }
        }

        public IEnumerable<Element> ServerElements
        {
            get
            {
                foreach (Element selement in this.ServerApperances)
                {
                    if (selement != null)
                        yield return selement;
                }
            }
        }
    }

    public class Element : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }
        [ConfigurationProperty("protocol", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string protocol
        {
            get { return (string)base["protocol"]; }
            set { base["protocol"] = value; }
        }
        [ConfigurationProperty("way", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string way
        {
            get { return (string)base["way"]; }
            set { base["way"] = value; }
        }
        [ConfigurationProperty("filename", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string filename
        {
            get { return (string)base["filename"]; }
            set { base["filename"] = value; }
        }
        [ConfigurationProperty("home", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string home
        {
            get { return (string)base["home"]; }
            set { base["home"] = value; }
        }
        [ConfigurationProperty("remoteDirectory", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string remoteDirectory
        {
            get { return (string)base["remoteDirectory"]; }
            set { base["remoteDirectory"] = value; }
        }
        [ConfigurationProperty("login", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string login
        {
            get { return (string)base["login"]; }
            set { base["login"] = value; }
        }
        [ConfigurationProperty("pw", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string pw
        {
            get { return (string)base["pw"]; }
            set { base["pw"] = value; }
        }
        [ConfigurationProperty("localDirectoryPath", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string localDirectoryPath
        {
            get { return (string)base["localDirectoryPath"]; }
            set { base["localDirectoryPath"] = value; }
        }
        [ConfigurationProperty("localCommand", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string localCommand
        {
            get { return (string)base["localCommand"]; }
            set { base["localCommand"] = value; }
        }
        [ConfigurationProperty("remoteCommand", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string remoteCommand
        {
            get { return (string)base["remoteCommand"]; }
            set { base["remoteCommand"] = value; }
        }
        [ConfigurationProperty("localDoneDirectoryPath", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string localDoneDirectoryPath
        {
            get { return (string)base["localDoneDirectoryPath"]; }
            set { base["localDoneDirectoryPath"] = value; }
        }


        public List<string> availableFiles { get; set; } // Pratique en download SFTP si le nom du fichier n'est pas exactement file.filename
    }

    [ConfigurationCollection(typeof(Element))]
    public class ServerAppearanceCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "Element";

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }
        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }


        public override bool IsReadOnly()
        {
            return false;
        }


        protected override ConfigurationElement CreateNewElement()
        {
            return new Element();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Element)(element)).name;
        }

        public Element this[int idx]
        {
            get
            {
                return (Element)BaseGet(idx);
            }
        }
    }

    public class ConnectionSection : ConfigurationSection
    {
        [ConfigurationProperty("Servers")]
        public ServerAppearanceCollection ServerElement
        {
            get { return ((ServerAppearanceCollection)(base["Servers"])); }
            set { base["Servers"] = value; }
        }
    }
}
