using System;
using System.Net;
using System.Net.Sockets;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	public static class SocketPortable
	{
		private const int _receiveBufferLength = 102400;

		private static bool DisableUdpConnectionResetSupportedByPlatform
		{
			get
			{
				return true;
			}
		}

		public static Socket CreateListenerSocket()
		{
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
			{
				EnableBroadcast = true,
				ExclusiveAddressUse = false
			};
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 102400);
			return socket;
		}

		public static void DisableUdpConnectionReset(Socket socket)
		{
			if (SocketPortable.DisableUdpConnectionResetSupportedByPlatform)
			{
				try
				{
					byte[] num = new byte[] { Convert.ToByte(false) };
					socket.IOControl(-1744830452, num, null);
				}
				catch (Exception exception)
				{
					Console.WriteLine("DisableUdpConnectionReset: error: {0}", exception.Message);
				}
			}
		}

		private static bool IsUdpConnectionReset(ProtocolType protocolType, SocketException socket_ex)
		{
			if (protocolType != ProtocolType.Udp)
			{
				return false;
			}
			return socket_ex.SocketErrorCode == SocketError.ConnectionReset;
		}

		public static int Receive(Socket socket, byte[] buffer, out IPEndPoint remoteEndpoint, out int networkInterfaceIndex)
		{
			IPPacketInformation pPacketInformation;
			SocketFlags socketFlag = SocketFlags.None;
			EndPoint pEndPoint = new IPEndPoint(IPAddress.Any, 0);
			networkInterfaceIndex = -1;
			if (socket == null)
			{
				remoteEndpoint = pEndPoint as IPEndPoint;
				return 0;
			}
			int num = -1;
			try
			{
				num = socket.ReceiveMessageFrom(buffer, 0, (int)buffer.Length, ref socketFlag, ref pEndPoint, out pPacketInformation);
				networkInterfaceIndex = pPacketInformation.Interface;
			}
			catch (SocketException socketException1)
			{
				SocketException socketException = socketException1;
				bool flag = false;
				if (!SocketPortable.DisableUdpConnectionResetSupportedByPlatform && SocketPortable.IsUdpConnectionReset(socket.ProtocolType, socketException))
				{
					flag = true;
				}
				if (!flag)
				{
					throw;
				}
			}
			if (pEndPoint == null || !(pEndPoint is IPEndPoint))
			{
				remoteEndpoint = new IPEndPoint(IPAddress.None, 0);
			}
			else
			{
				remoteEndpoint = pEndPoint as IPEndPoint;
			}
			return num;
		}

		public static void SetSocketOptionsForBroadcastSending(Socket socket)
		{
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);
		}

		public static void SetSocketOptionsForGlobalSenderReceiver(Socket socket)
		{
			socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 102400);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
		}
	}
}