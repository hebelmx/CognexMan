using Cognex.DataMan.SDK;
using System;

namespace Cognex.DataMan.SDK.Utils
{
	public class WindowsCeKeepAlive
	{
		private DataManSystem _hostDmSystem;

		private DataManSystem _keepAliveDmSystem;

		private int _timeout;

		private int _interval;

		public WindowsCeKeepAlive(DataManSystem system, int timeout, int interval)
		{
			this._hostDmSystem = system;
			this._timeout = timeout;
			this._interval = interval;
			this._hostDmSystem.SystemConnected += new SystemConnectedHandler(this.OnHostSystemConnected);
			this._hostDmSystem.SystemDisconnected += new SystemDisconnectedHandler(this.OnSystemDisconnected);
		}

		private void OnHostSystemConnected(object sender, EventArgs args)
		{
			if (this._keepAliveDmSystem == null)
			{
				EthSystemConnector ethSystemConnector = null;
				if (this._hostDmSystem.Connector is EthSystemConnector)
				{
					EthSystemConnector connector = this._hostDmSystem.Connector as EthSystemConnector;
					ethSystemConnector = new EthSystemConnector(connector.Address, connector.Port)
					{
						UserName = connector.UserName,
						Password = connector.Password
					};
					this._keepAliveDmSystem = new DataManSystem(ethSystemConnector);
					this._keepAliveDmSystem.SetResultTypes(ResultTypes.None);
					this._keepAliveDmSystem.SetKeepAliveOptions(true, this._timeout, this._interval);
				}
			}
			try
			{
				this._keepAliveDmSystem.SystemDisconnected += new SystemDisconnectedHandler(this.OnSystemDisconnected);
				this._keepAliveDmSystem.Connect();
			}
			catch
			{
				this._keepAliveDmSystem = null;
			}
		}

		private void OnSystemDisconnected(object sender, EventArgs args)
		{
			this._keepAliveDmSystem.SystemDisconnected -= new SystemDisconnectedHandler(this.OnSystemDisconnected);
			this._keepAliveDmSystem.Disconnect();
			this._hostDmSystem.Disconnect();
		}
	}
}