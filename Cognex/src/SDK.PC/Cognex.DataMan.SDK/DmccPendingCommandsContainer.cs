using System;
using System.Collections.Generic;

namespace Cognex.DataMan.SDK
{
	internal class DmccPendingCommandsContainer : IDmccPendingCommandContainer
	{
		private Dictionary<int, CommandInfo> _pendingCommands = new Dictionary<int, CommandInfo>();

		public DmccPendingCommandsContainer()
		{
		}

		public void Add(int commandId, CommandInfo command_info)
		{
			this._pendingCommands.Add(commandId, command_info);
		}

		public ICommandInfo FindPendingCommand(int commandId)
		{
			CommandInfo commandInfo;
			if (this._pendingCommands.TryGetValue(commandId, out commandInfo))
			{
				return commandInfo;
			}
			return null;
		}

		public bool RemoveCommand(int commandId)
		{
			return this._pendingCommands.Remove(commandId);
		}

		public List<CommandInfo> TakeAll()
		{
			List<CommandInfo> commandInfos = new List<CommandInfo>(this._pendingCommands.Values.Count);
			foreach (CommandInfo value in this._pendingCommands.Values)
			{
				commandInfos.Add(value);
			}
			this._pendingCommands.Clear();
			return commandInfos;
		}
	}
}