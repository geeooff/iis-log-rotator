using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IisLogRotator.Configuration
{
	public class RotationSection : ConfigurationSection
	{
		private static readonly ConfigurationElementProperty s_elemProperty;
		private static readonly ConfigurationPropertyCollection s_properties;
		private static readonly ConfigurationProperty s_propEnableEventLog;
		private static readonly ConfigurationProperty s_propDefaultSettings;
		private static readonly ConfigurationProperty s_propSitesSettings;
		private static readonly ConfigurationProperty s_propHttpSysErrorSettings;

		static RotationSection()
		{
			s_elemProperty = new ConfigurationElementProperty(
				new CallbackValidator(
					typeof(RotationSection),
					new ValidatorCallback(RotationSection.Validate)
				)
			);

			s_propEnableEventLog = new ConfigurationProperty(
				"enableEventLog",
				typeof(bool),
				false,
				ConfigurationPropertyOptions.None
			);

			s_propDefaultSettings = new ConfigurationProperty(
				"defaultSettings",
				typeof(RotationSettingsElement),
				new RotationSettingsElement(),
				ConfigurationPropertyOptions.None
			);

			s_propSitesSettings = new ConfigurationProperty(
				"sitesSettings",
				typeof(SiteRotationSettingsElementCollection),
				new SiteRotationSettingsElementCollection(),
				ConfigurationPropertyOptions.IsDefaultCollection
			);

			s_propHttpSysErrorSettings = new ConfigurationProperty(
				"httpSysErrorSettings",
				typeof(HttpSysErrorRotationSettingsElement),
				new HttpSysErrorRotationSettingsElement(),
				ConfigurationPropertyOptions.None
			);

			s_properties = new ConfigurationPropertyCollection
			{
				s_propEnableEventLog,
				s_propDefaultSettings,
				s_propSitesSettings,
				s_propHttpSysErrorSettings
			};
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get { return s_properties; }
		}

		protected override ConfigurationElementProperty ElementProperty
		{
			get { return s_elemProperty; }
		}

		private static void Validate(object value)
		{
			//RotationSection section = (RotationSection)value;
		}

		public bool EnableEventLog
		{
			get { return ((bool)(this[s_propEnableEventLog])); }
			set { this[s_propEnableEventLog] = value; }
		}

		public RotationSettingsElement DefaultSettings
		{
			get { return ((RotationSettingsElement)(this[s_propDefaultSettings])); }
			set { this[s_propDefaultSettings] = value; }
		}

		public SiteRotationSettingsElementCollection SitesSettings
		{
			get { return ((SiteRotationSettingsElementCollection)(this[s_propSitesSettings])); }
			set { this[s_propSitesSettings] = value; }
		}

		public HttpSysErrorRotationSettingsElement HttpSysErrorSettings
		{
			get { return ((HttpSysErrorRotationSettingsElement)(this[s_propHttpSysErrorSettings])); }
			set { this[s_propHttpSysErrorSettings] = value; }
		}

		public RotationSettingsElement GetSiteSettingsOrDefault(string id)
		{
			SiteRotationSettingsElement siteSettings = this.SitesSettings
				.Cast<SiteRotationSettingsElement>()
				.FirstOrDefault(s => s.ID == id);

			return siteSettings ?? this.DefaultSettings;
		}
	}
}
