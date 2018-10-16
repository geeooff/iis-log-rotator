using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IisLogRotator.Configuration
{
	public class HttpSysErrorRotationSettingsElement : RotationSettingsElement
	{
		private static readonly ConfigurationPropertyCollection s_properties;

		static HttpSysErrorRotationSettingsElement()
		{
			s_properties = new ConfigurationPropertyCollection();

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
	}
}
