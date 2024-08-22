using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class LanguageIDRecord : CogNamerRecord
	{
		public string LanguageID
		{
			get;
			private set;
		}

		private LanguageIDRecord()
		{
			base.Type = RecordType.LanguageID;
			this.LanguageID = "";
		}

		public LanguageIDRecord(string languageId) : this()
		{
			this.LanguageID = languageId;
		}

		public LanguageIDRecord(byte[] recordBytes) : this()
		{
			this.LanguageID = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.LanguageID);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("LanguageIDRecord={{", new object[0]);
			stringBuilder.AppendFormat("LanguageID={0}", this.LanguageID);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}