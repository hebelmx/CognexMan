using System;

namespace Cognex.DataMan.SDK
{
	internal interface IDmccPendingCommandContainer
	{
		ICommandInfo FindPendingCommand(int commandId);

		bool RemoveCommand(int commandId);
	}
}