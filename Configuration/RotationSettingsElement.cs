using System;
using System.Configuration;
using System.ComponentModel;

namespace Smartgeek.LogRotator.Configuration
{
	public class RotationSettingsElement : ConfigurationElement
	{
		private static readonly ConfigurationElementProperty s_elemProperty;
		private static readonly ConfigurationPropertyCollection s_properties;
		private static readonly ConfigurationProperty s_propCompress;
		private static readonly ConfigurationProperty s_propCompressAfter;
		private static readonly ConfigurationProperty s_propDelete;
		private static readonly ConfigurationProperty s_propDeleteAfter;

		static RotationSettingsElement()
		{
			s_elemProperty = new ConfigurationElementProperty(
				new CallbackValidator(
					typeof(RotationSettingsElement),
					new ValidatorCallback(RotationSettingsElement.Validate)
				)
			);

			s_propCompress = new ConfigurationProperty(
				"compress",
				typeof(bool),
				false,
				ConfigurationPropertyOptions.None
			);

			s_propCompressAfter = new ConfigurationProperty(
				"compressAfter",
				typeof(int),
				1,
				TypeDescriptor.GetConverter(typeof(int)),
				new IntegerValidator(1, int.MaxValue),
				ConfigurationPropertyOptions.None
			);

			s_propDelete = new ConfigurationProperty(
				"delete",
				typeof(bool),
				false,
				ConfigurationPropertyOptions.None
			);

			s_propDeleteAfter = new ConfigurationProperty(
				"deleteAfter",
				typeof(int),
				1,
				TypeDescriptor.GetConverter(typeof(int)),
				new IntegerValidator(1, int.MaxValue),
				ConfigurationPropertyOptions.None
			);

			s_properties = new ConfigurationPropertyCollection();
			s_properties.Add(s_propCompress);
			s_properties.Add(s_propCompressAfter);
			s_properties.Add(s_propDelete);
			s_properties.Add(s_propDeleteAfter);
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get { return s_properties; }
		}

		protected static ConfigurationPropertyCollection BaseProperties
		{
			get { return s_properties; }
		}

		protected override ConfigurationElementProperty ElementProperty
		{
			get { return s_elemProperty; }
		}

		private static void Validate(object value)
		{
			RotationSettingsElement element = (RotationSettingsElement)value;

			if (element.ElementInformation.IsPresent)
			{
				if (element.Compress && element.Delete && element.DeleteAfter <= element.CompressAfter)
				{
					throw new ConfigurationErrorsException(
						"The 'DeleteAfter' attribute value must be greater than 'CompressAfter' attribute value.",
						element.ElementInformation.Source,
						element.ElementInformation.LineNumber
					);
				}
			}
		}

		public bool Compress
		{
			get { return ((bool)(this[s_propCompress])); }
			set { this[s_propCompress] = value; }
		}

		public int CompressAfter
		{
			get { return ((int)(this[s_propCompressAfter])); }
			set { this[s_propCompressAfter] = value; }
		}

		public bool Delete
		{
			get { return ((bool)(this[s_propDelete])); }
			set { this[s_propDelete] = value; }
		}

		public int DeleteAfter
		{
			get { return ((int)(this[s_propDeleteAfter])); }
			set { this[s_propDeleteAfter] = value; }
		}
	}
}
