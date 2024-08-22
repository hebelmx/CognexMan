using System;

namespace Cognex.DataMan.CogNamer
{
	public class SetAttributePacket : CogNamerPacket
	{
		public SetAttributePacket() : this(FlagType.None, ErrorCode.Success)
		{
		}

		public SetAttributePacket(FlagType flags, ErrorCode errorCode) : base(CommandType.SetAttribute, flags, errorCode)
		{
		}
	}
}