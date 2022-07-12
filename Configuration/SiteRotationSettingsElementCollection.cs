using Smartgeek.Configuration;
using System.Configuration;

namespace IisLogRotator.Configuration
{
    public class SiteRotationSettingsElementCollection : ConfigurationElementCollection<SiteRotationSettingsElement, string>
    {
        protected override string GetElementKey(SiteRotationSettingsElement element)
        {
            return element.ID;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }
    }
}
