using Cognex.DataMan.SDK.Utils.PlatformHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Cognex.DataMan.SDK.Utils
{
	internal static class DmccResponseParserUtils
	{
		private static string ApplySvgColonHack(string svgContents)
		{
			if (svgContents.Contains("xmlns:xlink=\"http://www.w3.org/1999/xlink\""))
			{
				return svgContents;
			}
			return svgContents.Replace("xlink:href", "xlinkhref");
		}

		private static XmlReader CreateXmlReaderForSvg(string svgXml)
		{
			return new XmlTextReader(new StringReader(svgXml))
			{
				Namespaces = false
			};
		}

		public static void ExtractIdsFromReadXml(string readXml, out HashSetPortable<int> containedResultIds, out HashSetPortable<int> referredImageIds, out int result_xml_version)
		{
			containedResultIds = new HashSetPortable<int>();
			referredImageIds = new HashSetPortable<int>();
			result_xml_version = -1;
			int num = -1;
			using (StringReader stringReader = new StringReader(readXml))
			{
				using (XmlTextReader xmlTextReader = new XmlTextReader(stringReader))
				{
					while (xmlTextReader.ReadToFollowing("result"))
					{
						int num1 = -1;
						if (DmccResponseParserUtils.TryParseIntAttribute(xmlTextReader, "id", ref num1) && num1 > 0)
						{
							containedResultIds.Add(num1);
							if (num <= 0)
							{
								num = num1;
							}
						}
						if (DmccResponseParserUtils.TryParseIntAttribute(xmlTextReader, "image_id", ref num1) && num1 > 0)
						{
							referredImageIds.Add(num1);
						}
						if (!DmccResponseParserUtils.TryParseIntAttribute(xmlTextReader, "version", ref num1))
						{
							continue;
						}
						result_xml_version = num1;
					}
				}
			}
			switch (result_xml_version)
			{
				case -1:
				{
					result_xml_version = 1;
					if (containedResultIds.Count <= 0 || referredImageIds.Count != 0)
					{
						return;
					}
					referredImageIds.Add(num);
					return;
				}
				case 0:
				case 1:
				case 2:
				case 3:
				{
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public static void ExtractIdsFromReadXmlExtended(string extendedResultXml, out HashSetPortable<int> containedResultIds, out HashSetPortable<int> referredImageIds, out int result_xml_version)
		{
			containedResultIds = new HashSetPortable<int>();
			referredImageIds = new HashSetPortable<int>();
			result_xml_version = -1;
			using (StringReader stringReader = new StringReader(extendedResultXml))
			{
				using (XmlTextReader xmlTextReader = new XmlTextReader(stringReader))
				{
					string name = null;
					while (xmlTextReader.Read())
					{
						switch (xmlTextReader.Depth)
						{
							case 0:
							{
								name = null;
								continue;
							}
							case 1:
							{
								name = xmlTextReader.Name;
								continue;
							}
							case 2:
							{
								if (!(xmlTextReader.Name == "id") || !(name == "result"))
								{
									continue;
								}
								try
								{
									int num = xmlTextReader.ReadElementContentAsInt();
									if (num > 0)
									{
										containedResultIds.Add(num);
									}
									continue;
								}
								catch
								{
									continue;
								}
								break;
							}
							default:
							{
								continue;
							}
						}
					}
				}
			}
		}

		public static void ExtractIdsFromSvg(float firmwareVersion, string svgXml, out HashSetPortable<int> referredResultIds, out int referredImageId)
		{
			referredImageId = -1;
			referredResultIds = new HashSetPortable<int>();
			int num = -1;
			using (XmlReader xmlReader = DmccResponseParserUtils.CreateXmlReaderForSvg(svgXml))
			{
				if (xmlReader.ReadToFollowing("svg"))
				{
					if (xmlReader.Depth == 0)
					{
						int num1 = -1;
						if (DmccResponseParserUtils.TryParseIntAttribute(xmlReader, "id", ref num1))
						{
							num = num1;
						}
					}
					while (xmlReader.ReadToFollowing("g"))
					{
						if (xmlReader.Depth != 1)
						{
							continue;
						}
						int num2 = -1;
						if (!DmccResponseParserUtils.TryParseIntAttribute(xmlReader, "id", ref num2))
						{
							continue;
						}
						referredResultIds.Add(num2);
					}
				}
			}
			bool flag = (double)firmwareVersion >= 5.4;
			bool flag1 = (double)firmwareVersion < 4.2;
			if (num > 0)
			{
				if (!flag)
				{
					referredImageId = num;
					referredResultIds.Add(num);
				}
				else
				{
					referredImageId = num;
				}
			}
			else if (referredResultIds.Count > 0 && !flag)
			{
				bool flag2 = true;
				foreach (int referredResultId in referredResultIds)
				{
					if (flag2 || referredImageId < referredResultId)
					{
						referredImageId = referredResultId;
					}
					flag2 = false;
				}
			}
			if (flag1 && referredResultIds.Count >= 2)
			{
				bool flag3 = true;
				foreach (int referredResultId1 in referredResultIds)
				{
					if (flag3 || referredImageId > referredResultId1)
					{
						referredImageId = referredResultId1;
					}
					flag3 = false;
				}
			}
		}

		private static bool TryParse(string input, out int result)
		{
			return int.TryParse(input, out result);
		}

		public static bool TryParseIntAttribute(XmlReader reader, string attrName, ref int value)
		{
			int num;
			try
			{
				string attribute = reader.GetAttribute(attrName);
				if (attribute != null && DmccResponseParserUtils.TryParse(attribute, out num))
				{
					value = num;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}
	}
}