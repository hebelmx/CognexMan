using Cognex.DataMan.CogNamer.PlatformHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	public class CogNamerPacket
	{
		public const int COGNAMERDEVICETYPE_INVALID = -1;

		private List<CogNamerRecord> _records;

		public CommandType Command
		{
			get;
			private set;
		}

		public ErrorCode Error
		{
			get;
			private set;
		}

		public FlagType Flags
		{
			get;
			private set;
		}

		public int Length
		{
			get;
			protected set;
		}

		public IEnumerable<CogNamerRecord> Records
		{
			get
			{
				return this._records;
			}
		}

		public CogNamerPacket(CommandType command, FlagType flags, ErrorCode errorCode)
		{
			this._records = new List<CogNamerRecord>();
			this.Command = command;
			this.Flags = flags;
			this.Error = errorCode;
			this.Length = 0;
		}

		public void AddRecord(CogNamerRecord record)
		{
			this._records.Add(record);
		}

		public static CogNamerPacket CreateFromPacketBytes(byte[] packetBuffer, int numValidPacketBytes)
		{
			CogNamerPacket noopPacket;
			int num = 0;
			if (numValidPacketBytes < 9)
			{
				return null;
			}
			if (CogNamerSerializer.GetU32(packetBuffer, ref num) != 1296975683)
			{
				return null;
			}
			if (CogNamerSerializer.GetU8(packetBuffer, ref num) != 4)
			{
				return null;
			}
			FlagType u8 = (FlagType)CogNamerSerializer.GetU8(packetBuffer, ref num);
			CommandType commandType = (CommandType)CogNamerSerializer.GetInt(packetBuffer, ref num);
			ErrorCode errorCode = (ErrorCode)CogNamerSerializer.GetInt(packetBuffer, ref num);
			CogNamerSerializer.GetInt(packetBuffer, ref num);
			switch (commandType)
			{
				case CommandType.Noop:
				{
					noopPacket = new NoopPacket(u8, errorCode);
					break;
				}
				case CommandType.Hello:
				{
					noopPacket = new HelloPacket(u8, errorCode);
					break;
				}
				case CommandType.Identify:
				{
					noopPacket = new IdentifyPacket(u8, errorCode);
					break;
				}
				case CommandType.Bootup:
				{
					noopPacket = new BootupPacket(u8, errorCode);
					break;
				}
				case CommandType.IPAssign:
				{
					noopPacket = new IPAssignPacket(u8, errorCode);
					break;
				}
				case CommandType.FactoryReset:
				{
					noopPacket = new FactoryResetPacket(u8, errorCode);
					break;
				}
				case CommandType.SetAttribute:
				{
					noopPacket = new SetAttributePacket(u8, errorCode);
					break;
				}
				case CommandType.Flash:
				{
					noopPacket = new FlashPacket(u8, errorCode);
					break;
				}
				case CommandType.QueryCache:
				{
					noopPacket = new QueryCachePacket(u8, errorCode);
					break;
				}
				case CommandType.Restart:
				{
					noopPacket = new RestartPacket(u8, errorCode);
					break;
				}
				case CommandType.GetAttribute:
				{
					noopPacket = new GetAttributePacket(u8, errorCode);
					break;
				}
				case CommandType.ResetPassword:
				{
					noopPacket = new ResetPasswordPacket(u8, errorCode);
					break;
				}
				default:
				{
					noopPacket = new CogNamerPacket(commandType, u8, errorCode);
					break;
				}
			}
			while (num < numValidPacketBytes)
			{
				noopPacket.AddRecord(CogNamerRecord.CreateFromPacketBytes(packetBuffer, ref num));
			}
			noopPacket.Length = num;
			return noopPacket;
		}

		public int ExtractCogNamerDeviceType()
		{
			try
			{
				DeviceTypeRecord deviceTypeRecord = this.FindRecord(RecordType.DeviceType) as DeviceTypeRecord;
				if (deviceTypeRecord != null)
				{
					return deviceTypeRecord.DeviceType;
				}
			}
			catch
			{
			}
			return -1;
		}

		public string ExtractComments()
		{
			try
			{
				CommentsRecord commentsRecord = this.FindRecord(RecordType.Comments) as CommentsRecord;
				if (commentsRecord != null)
				{
					return commentsRecord.Comments;
				}
			}
			catch
			{
			}
			return "";
		}

		public string ExtractDescription()
		{
			try
			{
				DescriptionRecord descriptionRecord = this.FindRecord(RecordType.Description) as DescriptionRecord;
				if (descriptionRecord != null)
				{
					return descriptionRecord.Description;
				}
			}
			catch
			{
			}
			return "";
		}

		public string ExtractFirmwareVersion()
		{
			try
			{
				FirmwareVersionRecord firmwareVersionRecord = this.FindRecord(RecordType.FirmwareVersion) as FirmwareVersionRecord;
				if (firmwareVersionRecord != null)
				{
					return firmwareVersionRecord.FirmwareVersion;
				}
			}
			catch
			{
			}
			return "";
		}

		public string ExtractGroupName()
		{
			try
			{
				GroupNameRecord groupNameRecord = this.FindRecord(RecordType.GroupName) as GroupNameRecord;
				if (groupNameRecord != null)
				{
					return groupNameRecord.GroupName;
				}
			}
			catch
			{
			}
			return null;
		}

		public string ExtractHostName()
		{
			try
			{
				HostNameRecord hostNameRecord = this.FindRecord(RecordType.HostName) as HostNameRecord;
				if (hostNameRecord != null)
				{
					return hostNameRecord.HostName;
				}
			}
			catch
			{
			}
			return "";
		}

		public IPAddress ExtractIPAddress()
		{
			try
			{
				IPAddressRecord pAddressRecord = this.FindRecord(RecordType.IPAddress) as IPAddressRecord;
				if (pAddressRecord != null)
				{
					return pAddressRecord.Address;
				}
			}
			catch
			{
			}
			return IPAddress.None;
		}

		public PhysicalAddress ExtractMacAddress()
		{
			try
			{
				MacAddressRecord macAddressRecord = this.FindRecord(RecordType.MACAddress) as MacAddressRecord;
				if (macAddressRecord != null)
				{
					return macAddressRecord.MacAddress;
				}
			}
			catch
			{
			}
			return PhysicalAddress.None;
		}

		public string ExtractModelNumber()
		{
			try
			{
				ModelNumberRecord modelNumberRecord = this.FindRecord(RecordType.ModelNumber) as ModelNumberRecord;
				if (modelNumberRecord != null)
				{
					return modelNumberRecord.ModelNumber;
				}
			}
			catch
			{
			}
			return null;
		}

		public string ExtractSerialNumber()
		{
			try
			{
				SerialNumberRecord serialNumberRecord = this.FindRecord(RecordType.SerialNumber) as SerialNumberRecord;
				if (serialNumberRecord != null)
				{
					return serialNumberRecord.SerialNumber;
				}
			}
			catch
			{
			}
			return null;
		}

		public Dictionary<string, int> ExtractServicePorts()
		{
			try
			{
				ServicesRecord servicesRecord = this.FindRecord(RecordType.Services) as ServicesRecord;
				if (servicesRecord != null)
				{
					return servicesRecord.Services;
				}
			}
			catch
			{
			}
			return null;
		}

		public CogNamerRecord FindRecord(RecordType searchedRecordType)
		{
			CogNamerRecord cogNamerRecord;
			try
			{
				cogNamerRecord = this._records.Find((CogNamerRecord r) => r.Type == searchedRecordType);
			}
			catch
			{
				return null;
			}
			return cogNamerRecord;
		}

		public bool IsResponsePacketTo(CommandType command)
		{
			if (!EnumUtils.HasFlag(this.Flags, FlagType.Response))
			{
				return false;
			}
			return this.Command == command;
		}

		public byte[] Serialize()
		{
			MemoryStream memoryStream = new MemoryStream(512);
			memoryStream.WriteByte(67);
			memoryStream.WriteByte(71);
			memoryStream.WriteByte(78);
			memoryStream.WriteByte(77);
			memoryStream.WriteByte(4);
			memoryStream.WriteByte((byte)this.Flags);
			memoryStream.WriteByte((byte)this.Command);
			memoryStream.WriteByte((byte)this.Error);
			MemoryStream memoryStream1 = new MemoryStream(512);
			foreach (CogNamerRecord _record in this._records)
			{
				_record.Serialize(memoryStream1);
			}
			byte[] bytes = CogNamerSerializer.GetBytes((int)memoryStream1.Length);
			memoryStream.Write(bytes, 0, (int)bytes.Length);
			byte[] array = memoryStream1.ToArray();
			memoryStream.Write(array, 0, (int)array.Length);
			return memoryStream.ToArray();
		}

		public byte[] SerializeWithChangedFlags(FlagType flagsToAdd)
		{
			FlagType flags = this.Flags;
			CogNamerPacket cogNamerPacket = this;
			cogNamerPacket.Flags = cogNamerPacket.Flags | flagsToAdd;
			byte[] numArray = this.Serialize();
			this.Flags = flags;
			return numArray;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(2000);
			stringBuilder.AppendFormat("{0}{{", this.GetType().ToString().Replace("Cognex.DataMan.CogNamer.", ""));
			stringBuilder.AppendFormat("Command={0}; Flags={1}; Error={2}; ", this.Command.ToString(), this.Flags.ToString(), this.Error.ToString());
			if (this._records.Count > 0)
			{
				stringBuilder.AppendFormat("Records[{0}]={{\r\n", this._records.Count);
				for (int i = 0; i < this._records.Count; i++)
				{
					stringBuilder.AppendFormat("\t{0}\r\n", this._records[i].ToString());
				}
				stringBuilder.AppendFormat("}}", new object[0]);
			}
			stringBuilder.AppendFormat("}}", new object[0]);
			return stringBuilder.ToString();
		}
	}
}