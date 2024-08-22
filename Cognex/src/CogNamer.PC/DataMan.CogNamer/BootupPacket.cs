using System;
using System.Collections.Generic;
using System.Net;

namespace Cognex.DataMan.CogNamer
{
	public class BootupPacket : CogNamerPacket
	{
		public BootupPacket(FlagType flags, ErrorCode errorCode) : base(CommandType.Bootup, flags, errorCode)
		{
		}

		public BootupPacket(int cogNamerDeviceType, string modelNumber, string description, string firmwareVersion, string serialNumber, string hostname, IPAddress ipaddress, long macaddress, bool networkIsDhcpBased, bool networkIsLinkLocalIp, IPAddress networkSubNetMask, IPAddress networkGateway, IPAddress networkDnsServer, string networkDomain, int httpPort) : this(FlagType.None, ErrorCode.Success)
		{
			base.AddRecord(new DeviceTypeRecord(cogNamerDeviceType));
			base.AddRecord(new ModelNumberRecord(modelNumber));
			base.AddRecord(new DescriptionRecord(description));
			base.AddRecord(new FirmwareVersionRecord(firmwareVersion));
			base.AddRecord(new SerialNumberRecord(serialNumber));
			base.AddRecord(new HostNameRecord(hostname));
			base.AddRecord(new IPAddressRecord(ipaddress));
			base.AddRecord(new MacAddressRecord(macaddress));
			base.AddRecord(new NetworkSettingsRecord(networkIsDhcpBased, networkIsLinkLocalIp, networkSubNetMask, networkGateway, networkDnsServer, networkDomain));
			Dictionary<string, int> strs = new Dictionary<string, int>()
			{
				{ "cognamer-udp", 1069 },
				{ "http", httpPort }
			};
			base.AddRecord(new ServicesRecord(strs));
		}
	}
}