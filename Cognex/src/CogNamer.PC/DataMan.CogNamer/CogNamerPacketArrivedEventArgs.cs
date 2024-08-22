using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.CogNamer
{
	public class CogNamerPacketArrivedEventArgs
	{
		public NetworkInterfaceInfo InterfaceInfo
		{
			get;
			private set;
		}

		public CogNamerPacket Packet
		{
			get;
			private set;
		}

		public IPEndPoint RemoteEP
		{
			get;
			private set;
		}

		public CogNamerPacketArrivedEventArgs(CogNamerPacket packet, IPEndPoint remoteEP, NetworkInterfaceInfo interfaceInfo)
		{
			this.Packet = packet;
			this.RemoteEP = remoteEP;
			this.InterfaceInfo = interfaceInfo;
		}
	}
}