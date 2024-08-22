using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.CogNamer
{
	public abstract class CogNamerRecord
	{
		public const int MaxSuggestedRecordSize = 8150;

		public RecordType Type
		{
			get;
			protected set;
		}

		protected CogNamerRecord()
		{
		}

		public static CogNamerRecord CreateFromPacketBytes(byte[] packetBytes, ref int inputPosition)
		{
			CogNamerRecord noneRecord;
			RecordType num = (RecordType)CogNamerSerializer.GetInt(packetBytes, ref inputPosition);
			int num1 = CogNamerSerializer.GetInt(packetBytes, ref inputPosition);
			byte[] numArray = new byte[num1];
			Array.Copy(packetBytes, inputPosition, numArray, 0, num1);
			inputPosition += num1;
			RecordType recordType = num;
			switch (recordType)
			{
				case RecordType.None:
				{
					noneRecord = new NoneRecord();
					break;
				}
				case RecordType.KnownSystems:
				{
					noneRecord = new KnownSystemsRecord(numArray);
					break;
				}
				case RecordType.Credentials:
				{
					noneRecord = new CredentialsRecord(numArray);
					break;
				}
				default:
				{
					switch (recordType)
					{
						case RecordType.DeviceType:
						{
							noneRecord = new DeviceTypeRecord(numArray);
							break;
						}
						case RecordType.MACAddress:
						{
							noneRecord = new MacAddressRecord(numArray);
							break;
						}
						case RecordType.HostName:
						{
							noneRecord = new HostNameRecord(numArray);
							break;
						}
						case RecordType.IPAddress:
						{
							noneRecord = new IPAddressRecord(numArray);
							break;
						}
						case RecordType.NetworkSettings:
						{
							noneRecord = new NetworkSettingsRecord(numArray);
							break;
						}
						case RecordType.ModelNumber:
						{
							noneRecord = new ModelNumberRecord(numArray);
							break;
						}
						case RecordType.SerialNumber:
						{
							noneRecord = new SerialNumberRecord(numArray);
							break;
						}
						case RecordType.FirmwareVersion:
						{
							noneRecord = new FirmwareVersionRecord(numArray);
							break;
						}
						case RecordType.Description:
						{
							noneRecord = new DescriptionRecord(numArray);
							break;
						}
						case RecordType.GroupName:
						{
							noneRecord = new GroupNameRecord(numArray);
							break;
						}
						case RecordType.OrderingNumber:
						{
							noneRecord = new OrderingNumberRecord(numArray);
							break;
						}
						case RecordType.Services:
						{
							noneRecord = new ServicesRecord(numArray);
							break;
						}
						case RecordType.LanguageID:
						{
							noneRecord = new LanguageIDRecord(numArray);
							break;
						}
						case RecordType.Comments:
						{
							noneRecord = new CommentsRecord(numArray);
							break;
						}
						default:
						{
							noneRecord = new UnknownCognamerRecord(num, numArray);
							break;
						}
					}
					break;
				}
			}
			return noneRecord;
		}

		public abstract void Serialize(MemoryStream output);

		protected static void Serialize(RecordType recordType, MemoryStream recordData, MemoryStream output)
		{
			CogNamerRecord.Serialize(recordType, recordData.ToArray(), output);
		}

		protected static void Serialize(RecordType recordType, byte[] recordData, MemoryStream output)
		{
			byte[] bytes = CogNamerSerializer.GetBytes((int)recordType);
			byte[] numArray = CogNamerSerializer.GetBytes((int)recordData.Length);
			output.Write(bytes, 0, (int)bytes.Length);
			output.Write(numArray, 0, (int)numArray.Length);
			output.Write(recordData, 0, (int)recordData.Length);
		}
	}
}