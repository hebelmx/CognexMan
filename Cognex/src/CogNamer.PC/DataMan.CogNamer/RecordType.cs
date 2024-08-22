using System;

namespace Cognex.DataMan.CogNamer
{
	public enum RecordType
	{
		None = 0,
		KnownSystems = 1,
		Credentials = 2,
		DeviceType = 32,
		MACAddress = 33,
		HostName = 34,
		IPAddress = 35,
		NetworkSettings = 36,
		ModelNumber = 37,
		SerialNumber = 38,
		FirmwareVersion = 39,
		Description = 40,
		GroupName = 41,
		OrderingNumber = 42,
		Services = 43,
		LanguageID = 44,
		Comments = 45
	}
}