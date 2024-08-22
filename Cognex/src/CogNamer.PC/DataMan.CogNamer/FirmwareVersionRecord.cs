using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class FirmwareVersionRecord : CogNamerRecord
	{
		public string FirmwareVersion
		{
			get;
			private set;
		}

		private FirmwareVersionRecord()
		{
			base.Type = RecordType.FirmwareVersion;
			this.FirmwareVersion = "";
		}

		public FirmwareVersionRecord(string firmwareVersion) : this()
		{
			this.FirmwareVersion = firmwareVersion;
		}

		public FirmwareVersionRecord(byte[] recordBytes) : this()
		{
			this.FirmwareVersion = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.FirmwareVersion);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("FirmwareVersionRecord={{", new object[0]);
			stringBuilder.AppendFormat("FirmwareVersion={0}", this.FirmwareVersion);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}