using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.IO;

namespace IisLogRotator.Configuration
{
	public static class InstallerConfig
	{
		private static readonly Object s_initializeSyncRoot = new Object();

		private static bool s_initialized;
		private static Exception s_initializeException;
		private static string s_xmlFilePath;
		private static XmlDocument s_xmlDoc;
		private static XmlNode s_rotationNode;

		public static bool EnableEventLog
		{
			get
			{
				Initialize();
				XmlAttribute attr = s_rotationNode.Attributes["enableEventLog"];
				if (attr != null)
				{
					return Convert.ToBoolean(attr.Value);
				}
				return false;
			}
			set
			{
				Initialize();
				XmlAttribute attr = s_rotationNode.Attributes["enableEventLog"];
				if (value)
				{
					if (attr == null)
					{
						attr = s_xmlDoc.CreateAttribute("enableEventLog");
						s_rotationNode.Attributes.Append(attr);
					}
					attr.Value = "true";
				}
				else
				{
					if (attr != null)
					{
						s_rotationNode.Attributes.Remove(attr);
					}
				}
			}
		}

		private static void Initialize()
		{
			if (!s_initialized)
			{
				lock (s_initializeSyncRoot)
				{
					if (!s_initialized)
					{
						try
						{
							s_xmlFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".config";

							s_xmlDoc = new XmlDocument();
							s_xmlDoc.Load(s_xmlFilePath);

							s_rotationNode = s_xmlDoc.SelectSingleNode("configuration/rotation");
						}
						catch (Exception ex)
						{
							s_initializeException = ex;
						}
						finally
						{
							s_initialized = true;
						}
					}
				}
			}
			if (s_initializeException != null)
			{
				throw s_initializeException;
			}
		}

		public static void Save()
		{
			if (s_initialized)
			{
				s_xmlDoc.Save(s_xmlFilePath);
			}
		}
	}
}
