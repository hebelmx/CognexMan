using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class DeviceTypeRecord : CogNamerRecord
	{
		public int DeviceType
		{
			get;
			private set;
		}

		private DeviceTypeRecord()
		{
			base.Type = RecordType.DeviceType;
			this.DeviceType = 0;
		}

		public DeviceTypeRecord(int deviceType) : this()
		{
			this.DeviceType = deviceType;
		}

		public DeviceTypeRecord(byte[] recordBytes) : this()
		{
			this.DeviceType = recordBytes[0] << 8 | recordBytes[1];
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] deviceType = new byte[] { (byte)(this.DeviceType / 256), (byte)(this.DeviceType % 256) };
			CogNamerRecord.Serialize(base.Type, deviceType, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("DeviceTypeRecord={{", new object[0]);
			stringBuilder.AppendFormat("DeviceType=0x{0:X}", this.DeviceType);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}