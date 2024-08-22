using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class MacAddressRecord : CogNamerRecord
	{
		public PhysicalAddress MacAddress
		{
			get;
			private set;
		}

		public long MacAddressLong
		{
			get
			{
				return MacAddressRecord.MacConvertBytesToLong(this.MacAddress.GetAddressBytes());
			}
			set
			{
				this.MacAddress = new PhysicalAddress(MacAddressRecord.MacConvertLongToBytes(value));
			}
		}

		private MacAddressRecord()
		{
			base.Type = RecordType.MACAddress;
			this.MacAddress = PhysicalAddress.None;
		}

		public MacAddressRecord(PhysicalAddress macAddress) : this()
		{
			this.MacAddress = macAddress;
		}

		public MacAddressRecord(long macAddress) : this()
		{
			this.MacAddress = new PhysicalAddress(MacAddressRecord.MacConvertLongToBytes(macAddress));
		}

		public MacAddressRecord(byte[] recordBytes) : this()
		{
			this.MacAddress = new PhysicalAddress(recordBytes);
		}

		public static long MacConvertBytesToLong(byte[] macAddressAsBytes)
		{
			byte[] numArray = new byte[8];
			Array.Copy(macAddressAsBytes, 0, numArray, 0, 6);
			return BitConverter.ToInt64(numArray, 0);
		}

		public static byte[] MacConvertLongToBytes(long macAddressAsLong)
		{
			byte[] bytes = BitConverter.GetBytes(macAddressAsLong);
			byte[] numArray = new byte[6];
			Array.Copy(bytes, 0, numArray, 0, 6);
			return numArray;
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] addressBytes = this.MacAddress.GetAddressBytes();
			CogNamerRecord.Serialize(base.Type, addressBytes, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("MacAddressRecord={{", new object[0]);
			stringBuilder.AppendFormat("MacAddress={0}", this.MacAddress.ToString());
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}