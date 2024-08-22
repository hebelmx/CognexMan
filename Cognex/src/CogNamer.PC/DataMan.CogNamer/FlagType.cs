using System;

namespace Cognex.DataMan.CogNamer
{
	[Flags]
	public enum FlagType
	{
		UnsupportedMask = -225,
		None = 0,
		SupportedProbe = 32,
		Broadcast = 64,
		Response = 128
	}
}