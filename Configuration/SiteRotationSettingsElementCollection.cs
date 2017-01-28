using System;
using System.Net.Mail;
using System.Configuration;
using System.Collections.Generic;
using Smartgeek.Configuration;

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
