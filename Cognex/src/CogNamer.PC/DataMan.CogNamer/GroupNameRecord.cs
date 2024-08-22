using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class GroupNameRecord : CogNamerRecord
	{
		public string GroupName
		{
			get;
			private set;
		}

		private GroupNameRecord()
		{
			base.Type = RecordType.GroupName;
			this.GroupName = "";
		}

		public GroupNameRecord(string groupNameRecord) : this()
		{
			this.GroupName = groupNameRecord;
		}

		public GroupNameRecord(byte[] recordBytes) : this()
		{
			this.GroupName = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.GroupName);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("GroupNameRecord={{", new object[0]);
			stringBuilder.AppendFormat("GroupName={0}", this.GroupName);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}