using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class CommentsRecord : CogNamerRecord
	{
		public string Comments
		{
			get;
			private set;
		}

		private CommentsRecord()
		{
			base.Type = RecordType.Comments;
			this.Comments = "";
		}

		public CommentsRecord(string comments) : this()
		{
			this.Comments = comments;
		}

		public CommentsRecord(byte[] recordBytes) : this()
		{
			this.Comments = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.Comments);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("CommentsRecord={{", new object[0]);
			stringBuilder.AppendFormat("Comments={0}", this.Comments);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}