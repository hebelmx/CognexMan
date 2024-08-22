using System;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	public static class EnumUtils
	{
		public static bool HasFlag(Enum variable, Enum value)
		{
			ulong num = Convert.ToUInt64(value);
			return (Convert.ToUInt64(variable) & num) == num;
		}
	}
}