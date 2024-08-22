using System;

namespace Cognex.DataMan.CogNamer
{
	public enum CommandType
	{
		Noop,
		Hello,
		Identify,
		Bootup,
		IPAssign,
		FactoryReset,
		SetAttribute,
		Flash,
		QueryCache,
		Restart,
		GetAttribute,
		ResetPassword
	}
}