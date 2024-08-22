using System;
using System.IO;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class NoneRecord : CogNamerRecord
	{
		internal NoneRecord()
		{
			base.Type = RecordType.None;
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] numArray = new byte[0];
			CogNamerRecord.Serialize(base.Type, numArray, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("NoneRecord={{", new object[0]);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}