using System;
using System.Net.Mail;
using System.Configuration;
using System.Collections.Generic;
using Smartgeek.Configuration;

namespace Smartgeek.LogRotator.Configuration
{
	public class SiteRotationSettingsElementCollection : ConfigurationElementCollection<SiteRotationSettingsElement, long>
	{
		protected override long GetElementKey(SiteRotationSettingsElement element)
		{
			return element.ID;
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get { return ConfigurationElementCollectionType.BasicMap; }
		}
	}
}
