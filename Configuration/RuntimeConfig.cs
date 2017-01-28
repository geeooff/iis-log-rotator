using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IisLogRotator.Configuration
{
	public static class RuntimeConfig
	{
		private static readonly Object s_rotationSectionInitializeSyncRoot = new Object();

		private static System.Configuration.Configuration s_config;
		private static RotationSection s_rotationSection;
		private static bool s_rotationSectionInitialized;
		private static Exception s_rotationSectionInitializeException;
		
		public static RotationSection Rotation
		{
			get
			{
				Initialize();
				return s_rotationSection;
			}
		}

		private static void Initialize()
		{
			if (!s_rotationSectionInitialized)
			{
				lock (s_rotationSectionInitializeSyncRoot)
				{
					if (!s_rotationSectionInitialized)
					{
						try
						{
							s_config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
							s_rotationSection = (RotationSection)s_config.GetSection(@"rotation");

							if (s_rotationSection == null)
							{
								s_rotationSection = new RotationSection();
							}
						}
						catch (Exception ex)
						{
							s_rotationSectionInitializeException = ex;
						}
						finally
						{
							s_rotationSectionInitialized = true;
						}
					}
				}
			}
			if (s_rotationSectionInitializeException != null)
			{
				throw s_rotationSectionInitializeException;
			}
		}

		public static void Save()
		{
			if (s_rotationSectionInitialized)
			{
				s_config.Save();
			}
		}
	}
}
