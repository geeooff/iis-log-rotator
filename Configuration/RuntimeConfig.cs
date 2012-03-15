using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Smartgeek.LogRotator.Configuration
{
	public static class RuntimeConfig
	{
		private static readonly Object s_rotationSectionInitializeSyncRoot = new Object();

		private static RotationSection s_rotationSection;
		private static bool s_rotationSectionInitialized;
		private static Exception s_rotationSectionInitializeException;
		
		public static RotationSection Rotation
		{
			get
			{
				if (!s_rotationSectionInitialized)
				{
					lock (s_rotationSectionInitializeSyncRoot)
					{
						if (!s_rotationSectionInitialized)
						{
							try
							{
								s_rotationSection = (RotationSection)ConfigurationManager.GetSection(@"rotation");
								
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
				return s_rotationSection;
			}
		}
	}
}
