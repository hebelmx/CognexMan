using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class KnownSystemsRecord : CogNamerRecord
	{
		public List<KeyValuePair<IPAddress, string>> KnownSystems
		{
			get;
			private set;
		}

		private KnownSystemsRecord()
		{
			base.Type = RecordType.KnownSystems;
			this.KnownSystems = new List<KeyValuePair<IPAddress, string>>();
		}

		public KnownSystemsRecord(Dictionary<IPAddress, string> knownSystems) : this()
		{
			this.KnownSystems = new List<KeyValuePair<IPAddress, string>>(knownSystems);
		}

		public KnownSystemsRecord(byte[] recordBytes) : this()
		{
			try
			{
				int num = 0;
				while (num < (int)recordBytes.Length - 1)
				{
					IPAddress pAddress = CogNamerSerializer.GetIPAddress(recordBytes, ref num);
					string stringWithLength = CogNamerSerializer.GetStringWithLength(recordBytes, ref num);
					this.KnownSystems.Add(new KeyValuePair<IPAddress, string>(pAddress, stringWithLength));
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
			foreach (KeyValuePair<IPAddress, string> knownSystem in this.KnownSystems)
			{
				byte[] bytes = CogNamerSerializer.GetBytes(knownSystem.Key);
				byte[] bytesWithLength = CogNamerSerializer.GetBytesWithLength(knownSystem.Value);
				if ((int)memoryStream.Length + (int)bytes.Length + (int)bytesWithLength.Length > 8150)
				{
					break;
				}
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(bytesWithLength, 0, (int)bytesWithLength.Length);
			}
			CogNamerRecord.Serialize(base.Type, memoryStream, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("KnownSystemsRecord={{", new object[0]);
			foreach (KeyValuePair<IPAddress, string> knownSystem in this.KnownSystems)
			{
				stringBuilder.AppendFormat("{{{0}=>{1}}},", knownSystem.Key.ToString(), knownSystem.Value);
			}
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}