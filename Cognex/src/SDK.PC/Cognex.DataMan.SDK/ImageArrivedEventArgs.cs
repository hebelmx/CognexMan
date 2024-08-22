using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class ImageArrivedEventArgs : EventArgs
	{
		private System.Drawing.Image _image;

		public System.Drawing.Image Image
		{
			get
			{
				if (this._image != null)
				{
					return this._image;
				}
				this._image = ImageArrivedEventArgs.GetImageFromImageBytes(this.ImageBytes);
				return this._image;
			}
			private set
			{
				this._image = value;
			}
		}

		public byte[] ImageBytes
		{
			get;
			private set;
		}

		public bool IsImageCreatedFromImageBytes
		{
			get
			{
				return this._image != null;
			}
		}

		public int ResultId
		{
			get;
			private set;
		}

		public ImageArrivedEventArgs()
		{
			this.ResultId = 0;
			this._image = null;
		}

		public ImageArrivedEventArgs(int resultId, System.Drawing.Image image)
		{
			this.ResultId = resultId;
			this._image = image;
		}

		public ImageArrivedEventArgs(int resultId, byte[] imageBytes)
		{
			this.ResultId = resultId;
			this.ImageBytes = imageBytes;
		}

		public static System.Drawing.Image GetImageFromImageBytes(byte[] imageBytes)
		{
			System.Drawing.Image image;
			try
			{
				if (imageBytes == null || (int)imageBytes.Length == 0)
				{
					image = null;
				}
				else
				{
					image = System.Drawing.Image.FromStream(new MemoryStream(imageBytes));
				}
			}
			catch
			{
				return null;
			}
			return image;
		}
	}
}