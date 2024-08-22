using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class ServicesRecord : CogNamerRecord
	{
		public Dictionary<string, int> Services
		{
			get;
			private set;
		}

		private ServicesRecord()
		{
			base.Type = RecordType.Services;
			this.Services = new Dictionary<string, int>();
		}

		public ServicesRecord(Dictionary<string, int> services) : this()
		{
			this.Services = new Dictionary<string, int>(services);
		}

		public ServicesRecord(byte[] recordBytes) : this()
		{
			try
			{
				int num = 0;
				while (num < (int)recordBytes.Length - 1)
				{
					string stringWithLength = CogNamerSerializer.GetStringWithLength(recordBytes, ref num);
					ushort u16 = CogNamerSerializer.GetU16(recordBytes, ref num);
					this.Services[stringWithLength] = u16;
				}
			}
			catch (Exception exception)
			{
				throw new Exception("Parsing error", exception);
			}
		}

		public override void Serialize(MemoryStream output)
		{
			MemoryStream memoryStream = new MemoryStream();
			foreach (KeyValuePair<string, int> service in this.Services)
			{
				byte[] bytesWithLength = CogNamerSerializer.GetBytesWithLength(service.Key);
				byte[] bytes = CogNamerSerializer.GetBytes((ushort)service.Value);
				memoryStream.Write(bytesWithLength, 0, (int)bytesWithLength.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
			}
			CogNamerRecord.Serialize(base.Type, memoryStream, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("ServicesRecord={{", new object[0]);
			foreach (KeyValuePair<string, int> service in this.Services)
			{
				stringBuilder.AppendFormat("{{'{0}'=>{1}}},", service.Key.ToString(), service.Value);
			}
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}