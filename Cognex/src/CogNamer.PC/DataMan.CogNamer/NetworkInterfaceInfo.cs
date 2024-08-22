using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.CogNamer
{
	public class NetworkInterfaceInfo
	{
		public System.Net.IPAddress DNSServer
		{
			get;
			private set;
		}

		public string Domain
		{
			get;
			private set;
		}

		public System.Net.IPAddress Gateway
		{
			get;
			private set;
		}

		public int InterfaceIndex
		{
			get;
			private set;
		}

		public System.Net.IPAddress IPAddress
		{
			get;
			private set;
		}

		public bool IsDhcpBased
		{
			get;
			private set;
		}

		public bool IsWireless
		{
			get;
			private set;
		}

		public long MacAddress
		{
			get;
			private set;
		}

		public string Name
		{
			get;
			private set;
		}

		public System.Net.IPAddress SubnetBroadcastAddress
		{
			get
			{
				byte[] addressBytes = this.IPAddress.GetAddressBytes();
				byte[] numArray = this.SubnetMask.GetAddressBytes();
				byte[] numArray1 = new byte[4];
				for (int i = 0; i < 4; i++)
				{
					numArray1[i] = (byte)(addressBytes[i] | ~numArray[i]);
				}
				return new System.Net.IPAddress(numArray1);
			}
		}

		public System.Net.IPAddress SubnetMask
		{
			get;
			private set;
		}

		public NetworkInterfaceInfo()
		{
			this.InterfaceIndex = -1;
			this.Name = "";
			this.MacAddress = (long)0;
			this.IsWireless = false;
			this.IPAddress = System.Net.IPAddress.None;
			this.SubnetMask = System.Net.IPAddress.None;
			this.DNSServer = System.Net.IPAddress.None;
			this.Gateway = System.Net.IPAddress.None;
			this.Domain = "";
			this.IsDhcpBased = false;
		}

		public NetworkInterfaceInfo(int intfIndex, string name, long macAddress, bool isWireless, System.Net.IPAddress iPAddress, System.Net.IPAddress subnetMask, System.Net.IPAddress dNSServer, System.Net.IPAddress gateway, string domain, bool isDhcpBased)
		{
			this.InterfaceIndex = intfIndex;
			this.Name = name;
			this.MacAddress = macAddress;
			this.IsWireless = isWireless;
			this.IPAddress = iPAddress;
			this.SubnetMask = subnetMask;
			this.DNSServer = dNSServer;
			this.Gateway = gateway;
			this.Domain = domain;
			this.IsDhcpBased = isDhcpBased;
		}
	}
}