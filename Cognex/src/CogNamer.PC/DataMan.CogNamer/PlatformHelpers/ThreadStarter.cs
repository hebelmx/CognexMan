using System;
using System.Threading;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	internal class ThreadStarter
	{
		private Thread _thread;

		private object _parameter;

		private ParameterizedThreadStart _threadFunc;

		private bool _isBackground;

		private string _name;

		public ThreadStarter(ParameterizedThreadStart threadFunc, bool isBackground, string name, object parameter)
		{
			this._threadFunc = threadFunc;
			this._isBackground = isBackground;
			this._name = name;
			this._parameter = parameter;
		}

		public void Abort()
		{
			if (this._thread == null)
			{
				return;
			}
			this._thread.Abort();
		}

		public void Join()
		{
			if (this._thread == null)
			{
				return;
			}
			this._thread.Join();
		}

		public bool Join(int millisecondsTimeout)
		{
			if (this._thread == null)
			{
				return false;
			}
			return this._thread.Join(millisecondsTimeout);
		}

		public void Start()
		{
			if (this._thread != null)
			{
				return;
			}
			this._thread = new Thread(new ThreadStart(this.StartImpl))
			{
				IsBackground = this._isBackground,
				Name = this._name
			};
			this._thread.Start();
		}

		private void StartImpl()
		{
			this._threadFunc(this._parameter);
		}
	}
}