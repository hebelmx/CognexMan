using System;

namespace Cognex.DataMan.CogNamer
{
	public enum ErrorCode
	{
		Success,
		Failed,
		Unsupported,
		InvalidUsername,
		InvalidPassword,
		NoPermissions,
		MissingInputData,
		InvalidInputData,
		CommandOnlySupportedAtBootup,
		NonExistantRecord
	}
}