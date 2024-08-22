using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class UnknownCognamerRecord : CogNamerRecord
	{
		public byte[] RecordBytes
		{
			get;
			private set;
		}

		private UnknownCognamerRecord()
		{
			base.Type = RecordType.None;
			this.RecordBytes = new byte[0];
		}

		public UnknownCognamerRecord(RecordType recordType, byte[] recordBytes) : this()
		{
			base.Type = recordType;
			this.RecordBytes = recordBytes;
		}

		public override void Serialize(MemoryStream output)
		{
			CogNamerRecord.Serialize(base.Type, this.RecordBytes, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("UnknownCognamerRecord={{Type=0x{0:X}, Data[{1}]=<...>", (uint)base.Type, (int)this.RecordBytes.Length);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}