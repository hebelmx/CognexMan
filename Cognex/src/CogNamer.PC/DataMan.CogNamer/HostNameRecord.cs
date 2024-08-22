using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class HostNameRecord : CogNamerRecord
	{
		public string HostName
		{
			get;
			private set;
		}

		private HostNameRecord()
		{
			base.Type = RecordType.HostName;
			this.HostName = "";
		}

		public HostNameRecord(string hostName) : this()
		{
			this.HostName = hostName;
		}

		public HostNameRecord(byte[] recordBytes) : this()
		{
			this.HostName = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.HostName);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("HostNameRecord={{", new object[0]);
			stringBuilder.AppendFormat("HostName={0}", this.HostName);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}