using System;
using System.Threading;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	public class EventWaitHandlePortable : IDisposable
	{
		private bool _isDisposed;

		private EventWaitHandle _eventWaitHandle;

		public bool IsDisposed
		{
			get
			{
				return this._isDisposed;
			}
		}

		public EventWaitHandlePortable(bool manualReset)
		{
			this._isDisposed = false;
			this._eventWaitHandle = new EventWaitHandle(false, (manualReset ? EventResetMode.ManualReset : EventResetMode.AutoReset));
		}

		public void Close()
		{
			if (this._eventWaitHandle != null)
			{
				this._eventWaitHandle.Close();
				this._eventWaitHandle = null;
			}
		}

		public void Dispose()
		{
			this.DoDispose(true);
			GC.SuppressFinalize(this);
		}

		private void DoDispose(bool disposeManagedResources)
		{
			if (this._isDisposed)
			{
				return;
			}
			this._isDisposed = true;
			if (this._eventWaitHandle != null)
			{
				this.Close();
			}
		}

		~EventWaitHandlePortable()
		{
			this.DoDispose(false);
		}

		public void Reset()
		{
			if (this._eventWaitHandle != null)
			{
				this._eventWaitHandle.Reset();
			}
		}

		public void Set()
		{
			if (this._eventWaitHandle != null)
			{
				this._eventWaitHandle.Set();
			}
		}

		public override string ToString()
		{
			if (this._eventWaitHandle == null)
			{
				return "<null>";
			}
			return ((int)this._eventWaitHandle.Handle).ToString();
		}

		public static int WaitAny(EventWaitHandlePortable[] waitHandles)
		{
			if (waitHandles == null)
			{
				throw new ArgumentNullException("Argument cannot be null");
			}
			EventWaitHandle[] eventWaitHandleArray = new EventWaitHandle[(int)waitHandles.Length];
			for (int i = 0; i < (int)waitHandles.Length; i++)
			{
				eventWaitHandleArray[i] = waitHandles[i]._eventWaitHandle;
			}
			return WaitHandle.WaitAny(eventWaitHandleArray, -1);
		}

		public bool WaitOne(int millisecondsTimeout)
		{
			if (this._eventWaitHandle == null)
			{
				return false;
			}
			return this._eventWaitHandle.WaitOne(millisecondsTimeout);
		}
	}
}