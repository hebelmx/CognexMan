using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Cognex.DataMan.SDK.Utils
{
	public class Gui
	{
		public static double MaximumImageZoomFactor;

		static Gui()
		{
			Gui.MaximumImageZoomFactor = 16;
		}

		public Gui()
		{
		}

		public static byte[] BitmapToBytes(Bitmap image, System.Drawing.Imaging.ImageFormat format)
		{
			byte[] numArray;
			try
			{
				if (image != null)
				{
					MemoryStream memoryStream = new MemoryStream();
					image.Save(memoryStream, format);
					byte[] buffer = memoryStream.GetBuffer();
					memoryStream.Close();
					memoryStream = null;
					numArray = buffer;
				}
				else
				{
					numArray = null;
				}
			}
			catch
			{
				numArray = null;
			}
			return numArray;
		}

		public static Bitmap BytesToBitmap(byte[] imageData)
		{
			if (imageData == null)
			{
				return null;
			}
			return Gui.BytesToBitmap(imageData, 0, (int)imageData.Length);
		}

		public static Bitmap BytesToBitmap(byte[] buffer, int offset, int count)
		{
			Bitmap bitmap;
			try
			{
				if (buffer == null)
				{
					bitmap = null;
				}
				else if ((int)buffer.Length < 1 || count < 1)
				{
					bitmap = null;
				}
				else
				{
					MemoryStream memoryStream = new MemoryStream();
					memoryStream.Write(buffer, 0, (int)buffer.Length);
					memoryStream.Seek((long)0, SeekOrigin.Begin);
					Bitmap bitmap1 = new Bitmap(memoryStream);
					Bitmap bitmap2 = new Bitmap(bitmap1);
					bitmap1.Dispose();
					bitmap1 = null;
					memoryStream.Close();
					memoryStream = null;
					bitmap = bitmap2;
				}
			}
			catch
			{
				bitmap = null;
			}
			return bitmap;
		}

		public static Size FitImageInControl(Size imageSize, Size controlSize, out double zoomFactor)
		{
			zoomFactor = Gui.GetZoomFactorForImageInControl(imageSize, controlSize);
			return new Size((int)Math.Round((double)imageSize.Width * zoomFactor), (int)Math.Round((double)imageSize.Height * zoomFactor));
		}

		public static Size FitImageInControl(Size imageSize, Size controlSize)
		{
			double num;
			return Gui.FitImageInControl(imageSize, controlSize, out num);
		}

		public static Rectangle FitImageInControl(Size imageSize, Rectangle controlSize, out double zoomFactor)
		{
			Rectangle rectangle;
			Size size = Gui.FitImageInControl(imageSize, controlSize.Size, out zoomFactor);
			int num = Math.Max(0, size.Width - controlSize.Width);
			int num1 = Math.Max(0, size.Height - controlSize.Height);
			rectangle = (num1 >= num ? new Rectangle(0, controlSize.Top + num1 / 2, size.Width, size.Height) : new Rectangle(controlSize.Left + num / 2, 0, size.Width, size.Height));
			return rectangle;
		}

		public static Rectangle FitImageInControl(Size imageSize, Rectangle controlSize)
		{
			double num;
			return Gui.FitImageInControl(imageSize, controlSize, out num);
		}

		public static double GetZoomFactorForImageInControl(Size imageSize, Size controlSize)
		{
			if (imageSize.Height <= 0 || imageSize.Width <= 0 || controlSize.Height <= 0 || controlSize.Width <= 0)
			{
				return 1;
			}
			double width = (double)imageSize.Width / (double)imageSize.Height;
			if ((double)controlSize.Width / (double)controlSize.Height < width)
			{
				return Math.Min(Gui.MaximumImageZoomFactor, (double)controlSize.Width / (double)imageSize.Width);
			}
			return Math.Min(Gui.MaximumImageZoomFactor, (double)controlSize.Height / (double)imageSize.Height);
		}

		public static Bitmap ResizeImageToBitmap(Image image, Size desiredSize)
		{
			Bitmap bitmap = new Bitmap(desiredSize.Width, desiredSize.Height);
			using (Graphics graphic = Graphics.FromImage(bitmap))
			{
				graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphic.DrawImage(image, new Rectangle(0, 0, desiredSize.Width, desiredSize.Height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
			}
			return bitmap;
		}

		public static Bitmap StreamToBitmap(Stream imageStream, int imageDataSize)
		{
			Bitmap bitmap;
			try
			{
				if (imageStream == null || !imageStream.CanRead)
				{
					bitmap = null;
				}
				else if (imageDataSize >= 1)
				{
					byte[] numArray = new byte[imageDataSize];
					if (imageStream.Read(numArray, 0, imageDataSize) == imageDataSize)
					{
						bitmap = Gui.BytesToBitmap(numArray);
					}
					else
					{
						bitmap = null;
					}
				}
				else
				{
					bitmap = null;
				}
			}
			catch
			{
				bitmap = null;
			}
			return bitmap;
		}
	}
}