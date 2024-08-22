using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class NetworkSettingsRecord : CogNamerRecord
	{
		public IPAddress DnsServer
		{
			get;
			private set;
		}

		public string Domain
		{
			get;
			private set;
		}

		public IPAddress Gateway
		{
			get;
			private set;
		}

		public bool IsDhcpBased
		{
			get;
			private set;
		}

		public bool IsLinkLocalIp
		{
			get;
			private set;
		}

		public bool SkipSerializingDetailsForDhcp
		{
			get;
			set;
		}

		public IPAddress SubNetMask
		{
			get;
			private set;
		}

		private NetworkSettingsRecord()
		{
			base.Type = RecordType.NetworkSettings;
			this.SkipSerializingDetailsForDhcp = false;
			this.IsDhcpBased = true;
			this.IsLinkLocalIp = false;
			byte[] numArray = new byte[] { 255, 255, 0, 0 };
			this.SubNetMask = new IPAddress(numArray);
			this.Gateway = new IPAddress(new byte[4]);
			this.DnsServer = new IPAddress(new byte[4]);
			this.Domain = "";
		}

		public NetworkSettingsRecord(bool isDhcpBased, bool isLinkLocalIp, IPAddress subNetMask, IPAddress gateway, IPAddress dnsServer, string domain) : this()
		{
			this.IsDhcpBased = isDhcpBased;
			this.IsLinkLocalIp = isLinkLocalIp;
			this.SubNetMask = new IPAddress(subNetMask.GetAddressBytes());
			this.Gateway = new IPAddress(gateway.GetAddressBytes());
			this.DnsServer = new IPAddress(dnsServer.GetAddressBytes());
			this.Domain = domain;
		}

		public NetworkSettingsRecord(byte[] recordBytes) : this()
		{
			int num = 0;
			uint u8 = CogNamerSerializer.GetU8(recordBytes, ref num);
			this.IsDhcpBased = (u8 & 1) == 1;
			this.IsLinkLocalIp = (u8 & 2) == 2;
			if (num < (int)recordBytes.Length)
			{
				this.SubNetMask = CogNamerSerializer.GetIPAddress(recordBytes, ref num);
				this.Gateway = CogNamerSerializer.GetIPAddress(recordBytes, ref num);
				this.DnsServer = CogNamerSerializer.GetIPAddress(recordBytes, ref num);
				this.Domain = CogNamerSerializer.GetStringWithLength(recordBytes, ref num);
			}
		}

		public override void Serialize(MemoryStream output)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte num = 0;
				if (this.IsDhcpBased)
				{
					num = (byte)(num | 1);
				}
				if (this.IsLinkLocalIp)
				{
					num = (byte)(num | 2);
				}
				memoryStream.WriteByte(num);
				if (!this.IsDhcpBased || !this.SkipSerializingDetailsForDhcp)
				{
					this.SerializeBytes(CogNamerSerializer.GetBytes(this.SubNetMask), memoryStream);
					this.SerializeBytes(CogNamerSerializer.GetBytes(this.Gateway), memoryStream);
					this.SerializeBytes(CogNamerSerializer.GetBytes(this.DnsServer), memoryStream);
					this.SerializeBytes(CogNamerSerializer.GetBytesWithLength(this.Domain), memoryStream);
				}
				CogNamerRecord.Serialize(base.Type, memoryStream, output);
			}
		}

		private void SerializeBytes(byte[] bytes, MemoryStream output)
		{
			output.Write(bytes, 0, (int)bytes.Length);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("NetworkSettingsRecord={{", new object[0]);
			stringBuilder.AppendFormat("DhcpBased={0}, ", (this.IsDhcpBased ? "y" : "n"));
			stringBuilder.AppendFormat("LinkLocalIp={0}, ", (this.IsLinkLocalIp ? "y" : "n"));
			stringBuilder.AppendFormat("SubNetMask={0}, ", this.SubNetMask);
			stringBuilder.AppendFormat("Gateway={0}, ", this.Gateway);
			stringBuilder.AppendFormat("DnsServer={0}, ", this.DnsServer);
			stringBuilder.AppendFormat("Domain={0}", this.Domain);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}