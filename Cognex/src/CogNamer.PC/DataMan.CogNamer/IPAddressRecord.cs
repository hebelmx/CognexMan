using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class IPAddressRecord : CogNamerRecord
	{
		public IPAddress Address
		{
			get;
			private set;
		}

		private IPAddressRecord()
		{
			base.Type = RecordType.IPAddress;
			this.Address = IPAddress.None;
		}

		public IPAddressRecord(IPAddress address) : this()
		{
			this.Address = address;
		}

		public IPAddressRecord(byte[] recordBytes) : this()
		{
			this.Address = this.Deserialize(recordBytes);
		}

		private IPAddress Deserialize(byte[] address)
		{
			byte[] numArray = new byte[] { address[3], address[2], address[1], address[0] };
			return new IPAddress(numArray);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] addressBytes = this.Address.GetAddressBytes();
			byte[] numArray = new byte[] { addressBytes[3], addressBytes[2], addressBytes[1], addressBytes[0] };
			CogNamerRecord.Serialize(base.Type, numArray, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("IPAddressRecord={{", new object[0]);
			stringBuilder.AppendFormat("Address={0}", this.Address);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}