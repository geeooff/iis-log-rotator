using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace IisLogRotator
{
    /// <summary>
    /// Windows features detection helper
    /// </summary>
    /// <seealso cref="https://msdn.microsoft.com/en-us/library/ee309383(v=vs.85).aspx"/>
    internal class WindowsFeatures
    {
        private WindowsFeatures()
        {

        }

        internal bool IisWebServerRole { get; private set; }

        internal bool IisWebServer { get; private set; }

        internal bool Iis6ManagementCompatibility { get; private set; }

        public bool IisFtpServer { get; private set; }

        public bool IisFtpSvc { get; private set; }

        internal static WindowsFeatures GetFeatures()
        {
            ManagementScope scope = new ManagementScope(@"\\localhost\root\cimv2");

            // TODO detect windows server roles/features first (Win32_ServerFeature), then client features (Win32_OptionalFeature)
            // https://msdn.microsoft.com/en-us/library/cc280268(v=vs.85).aspx

            WqlObjectQuery query = new WqlObjectQuery(@"
				SELECT
					Name,
					InstallState
				FROM
					Win32_OptionalFeature
				WHERE
					Name LIKE 'IIS%'
					OR Name LIKE 'Smtpsvc%'
			");

            Dictionary<string, uint> features;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                features = searcher
                    .Get()
                    .Cast<ManagementObject>()
                    .ToDictionary(
                        obj => (string)obj.GetPropertyValue("Name"),
                        obj => (uint)obj.GetPropertyValue("InstallState")
                    );
            }

#if DEBUG
			Debug.WriteLine("Detected IIS features:");
			Debug.Indent();

			foreach (var feature in features)
			{
				string state;

				switch (feature.Value)
				{
					case 1: state = "enabled"; break;
					case 2: state = "disabled"; break;
					case 3: state = "absent"; break;
					default: state = "unknown"; break;
				}

				Debug.WriteLine(
					"{0} = {1}",
					feature.Key,
					state
				);
			}

			Debug.Unindent();
#endif

            return new WindowsFeatures()
            {
                IisWebServerRole = HasFeatureEnabled(features, "IIS-WebServerRole"),
                IisWebServer = HasFeatureEnabled(features, "IIS-WebServer"),
                Iis6ManagementCompatibility = HasFeatureEnabled(features, "IIS-IIS6ManagementCompatibility"),
                IisFtpServer = HasFeatureEnabled(features, "IIS-FTPServer"),
                IisFtpSvc = HasFeatureEnabled(features, "IIS-FTPSvc")
            };
        }

        internal static bool HasFeatureEnabled(Dictionary<string, uint> features, string name)
        {
            return features.ContainsKey(name) ? (features[name] == 1) : false;
        }
    }
}
