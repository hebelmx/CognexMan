using System;
using System.IO;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class CredentialsRecord : CogNamerRecord
	{
		private string UserName;

		private string Password;

		private CredentialsRecord()
		{
			this.UserName = "";
			this.Password = "";
		}

		public CredentialsRecord(string userName, string password) : this()
		{
			base.Type = RecordType.Credentials;
			this.UserName = userName;
			this.Password = password;
		}

		public CredentialsRecord(byte[] recordBytes) : this()
		{
			if ((int)recordBytes.Length > 0)
			{
				int num = 0;
				byte[] buffer = CogNamerSerializer.GetBuffer(recordBytes, ref num);
				byte[] numArray = CogNamerSerializer.GetBuffer(recordBytes, ref num);
				buffer = CogNamerCrypt.av_decrypt(buffer);
				numArray = CogNamerCrypt.av_decrypt(numArray);
				this.UserName = Encoding.UTF8.GetString(buffer, 0, (int)buffer.Length);
				this.Password = Encoding.UTF8.GetString(numArray, 0, (int)numArray.Length);
			}
		}

		public override void Serialize(MemoryStream output)
		{
			byte[] numArray;
			byte[] bytesForBuffer = CogNamerSerializer.GetBytesForBuffer(CogNamerCrypt.av_encrypt(this.UserName));
			byte[] bytesForBuffer1 = CogNamerSerializer.GetBytesForBuffer(CogNamerCrypt.av_encrypt(this.Password));
			numArray = (!string.IsNullOrEmpty(this.UserName) || !string.IsNullOrEmpty(this.Password) ? CogNamerSerializer.JoinBuffers(bytesForBuffer, bytesForBuffer1) : new byte[0]);
			CogNamerRecord.Serialize(base.Type, numArray, output);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(500);
			stringBuilder.AppendFormat("CredentialsRecord={{", new object[0]);
			stringBuilder.AppendFormat("UserName={0}, ", this.UserName);
			stringBuilder.AppendFormat("Password={0}", this.Password);
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}