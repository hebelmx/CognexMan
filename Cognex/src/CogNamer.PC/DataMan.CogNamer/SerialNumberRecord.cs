using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class SerialNumberRecord : CogNamerRecord
	{
		public string SerialNumber
		{
			get;
			private set;
		}

		private SerialNumberRecord()
		{
			base.Type = RecordType.SerialNumber;
			this.SerialNumber = "";
		}

		public SerialNumberRecord(string serialNumber) : this()
		{
			this.SerialNumber = serialNumber;
		}

		public SerialNumberRecord(byte[] recordBytes) : this()
		{
			this.SerialNumber = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.SerialNumber);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("SerialNumberRecord={{", new object[0]);
			stringBuilder.AppendFormat("SerialNumber={0}", this.SerialNumber);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}