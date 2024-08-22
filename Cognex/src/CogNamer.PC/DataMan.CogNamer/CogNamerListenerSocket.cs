using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.CogNamer
{
	internal class CogNamerListenerSocket
	{
		public System.Net.Sockets.Socket Socket
		{
			get;
			private set;
		}

		public CogNamerListenerType Type
		{
			get;
			private set;
		}

		public CogNamerListenerSocket(CogNamerListenerType type, System.Net.Sockets.Socket socket)
		{
			this.Type = type;
			this.Socket = socket;
		}

		public void CloseAndDisposeSocket()
		{
			lock (this)
			{
				try
				{
					System.Net.Sockets.Socket socket = this.Socket;
					if (socket != null)
					{
						this.Socket = null;
						socket.Shutdown(SocketShutdown.Both);
						socket.Close();
					}
					else
					{
						return;
					}
				}
				catch (Exception exception)
				{
				}
			}
		}
	}
}