using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class ModelNumberRecord : CogNamerRecord
	{
		public string ModelNumber
		{
			get;
			private set;
		}

		private ModelNumberRecord()
		{
			base.Type = RecordType.ModelNumber;
			this.ModelNumber = "";
		}

		public ModelNumberRecord(string modelNumber) : this()
		{
			this.ModelNumber = modelNumber;
		}

		public ModelNumberRecord(byte[] recordBytes) : this()
		{
			this.ModelNumber = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.ModelNumber);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("ModelNumberRecord={{", new object[0]);
			stringBuilder.AppendFormat("ModelNumber={0}", this.ModelNumber);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}