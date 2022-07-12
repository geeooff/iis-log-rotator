using System.Configuration;

namespace IisLogRotator.Configuration
{
    public class SiteRotationSettingsElement : RotationSettingsElement
    {
        private static readonly ConfigurationPropertyCollection s_properties;
        private static readonly ConfigurationProperty s_propID;

        static SiteRotationSettingsElement()
        {
            s_propID = new ConfigurationProperty(
                "id",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey
            );

            s_properties = new ConfigurationPropertyCollection();
            s_properties.Add(s_propID);

            // add inherited properties
            foreach (ConfigurationProperty property in RotationSettingsElement.BaseProperties)
            {
                s_properties.Add(property);
            }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return s_properties; }
        }

        public string ID
        {
            get { return ((string)(this[s_propID])); }
            set { this[s_propID] = value; }
        }
    }
}
