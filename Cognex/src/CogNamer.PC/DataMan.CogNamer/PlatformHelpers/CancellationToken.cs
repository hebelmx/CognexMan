using System;

namespace Cognex.DataMan.CogNamer.PlatformHelpers
{
	internal class CancellationToken
	{
		private EventWaitHandlePortable _cancellationEvent;

		public bool IsCancellationRequested
		{
			get
			{
				if (this._cancellationEvent.IsDisposed)
				{
					return true;
				}
				return this._cancellationEvent.WaitOne(0);
			}
		}

		public EventWaitHandlePortable WaitHandle
		{
			get
			{
				return this._cancellationEvent;
			}
		}

		internal CancellationToken(EventWaitHandlePortable cancellationEvent)
		{
			this._cancellationEvent = cancellationEvent;
		}

		public void Cancel()
		{
			this._cancellationEvent.Set();
		}

		public bool WaitOne(int millisecondsTimeout)
		{
			return this._cancellationEvent.WaitOne(millisecondsTimeout);
		}
	}
}