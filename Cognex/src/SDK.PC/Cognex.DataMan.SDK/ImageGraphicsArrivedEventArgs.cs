using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class ImageGraphicsArrivedEventArgs : EventArgs
	{
		public string ImageGraphics
		{
			get;
			private set;
		}

		public int ResultId
		{
			get;
			private set;
		}

		public ImageGraphicsArrivedEventArgs()
		{
			this.ResultId = 0;
			this.ImageGraphics = null;
		}

		public ImageGraphicsArrivedEventArgs(int resultId, string imageGraphics)
		{
			this.ResultId = resultId;
			this.ImageGraphics = imageGraphics;
		}
	}
}