using System;

namespace Cognex.DataMan.SDK
{
	public class LoginFailedException : DataManException
	{
		public LoginFailedException()
		{
		}

		public override string ToString()
		{
			return "Login failed";
		}
	}
}