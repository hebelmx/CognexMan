using System;
using System.Net.Sockets;

namespace Cognex.DataMan.SDK.Utils
{
	public static class SocketKeepAlive
	{
		public static void SetKeepAliveOptions(Socket socket, bool enabled, int timeout, int interval)
		{
			byte[] bytes = BitConverter.GetBytes((enabled ? 1 : 0));
			byte[] numArray = BitConverter.GetBytes(timeout);
			byte[] bytes1 = BitConverter.GetBytes(interval);
			byte[] numArray1 = new byte[12];
			Array.Copy(bytes, 0, numArray1, 0, (int)bytes.Length);
			Array.Copy(numArray, 0, numArray1, 4, (int)numArray.Length);
			Array.Copy(bytes1, 0, numArray1, 8, (int)bytes1.Length);
			socket.IOControl((IOControlCode)((long)-1744830460), numArray1, null);
		}
	}
}