using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class DescriptionRecord : CogNamerRecord
	{
		public string Description
		{
			get;
			private set;
		}

		private DescriptionRecord()
		{
			base.Type = RecordType.Description;
			this.Description = "";
		}

		public DescriptionRecord(string description) : this()
		{
			this.Description = description;
		}

		public DescriptionRecord(byte[] recordBytes) : this()
		{
			this.Description = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.Description);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("DescriptionRecord={{", new object[0]);
			stringBuilder.AppendFormat("Description={0}", this.Description);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}