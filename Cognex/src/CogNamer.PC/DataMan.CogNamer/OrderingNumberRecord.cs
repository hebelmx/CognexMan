using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class OrderingNumberRecord : CogNamerRecord
	{
		public string OrderingNumber
		{
			get;
			private set;
		}

		private OrderingNumberRecord()
		{
			base.Type = RecordType.OrderingNumber;
			this.OrderingNumber = "";
		}

		public OrderingNumberRecord(string orderingNumber) : this()
		{
			this.OrderingNumber = orderingNumber;
		}

		public OrderingNumberRecord(byte[] recordBytes) : this()
		{
			this.OrderingNumber = CogNamerSerializer.GetStringFromAllInputBytes(recordBytes);
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] bytesWithoutLength = CogNamerSerializer.GetBytesWithoutLength(this.OrderingNumber);
			CogNamerRecord.Serialize(base.Type, bytesWithoutLength, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("OrderingNumberRecord={{", new object[0]);
			stringBuilder.AppendFormat("OrderingNumber={0}", this.OrderingNumber);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}