using System;

namespace Cognex.DataMan.SDK
{
	internal interface ICommandInfo
	{
		bool ExpectBinaryResponseForCommand
		{
			get;
		}

		void SetError();
	}
}