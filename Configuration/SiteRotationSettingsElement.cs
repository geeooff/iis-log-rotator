using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Smartgeek.LogRotator.Configuration
{
	public class SiteRotationSettingsElement : RotationSettingsElement
	{
		private static readonly ConfigurationPropertyCollection s_properties;
		private static readonly ConfigurationProperty s_propID;

		static SiteRotationSettingsElement()
		{
			s_propID = new ConfigurationProperty(
				"id",
				typeof(String),
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

		public String ID
		{
			get { return ((String)(this[s_propID])); }
			set { this[s_propID] = value; }
		}
	}
}
