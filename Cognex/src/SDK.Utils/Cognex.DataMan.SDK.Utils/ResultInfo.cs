using Cognex.DataMan.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK.Utils
{
	public class ResultInfo
	{
		private int _resultId;

		private int _imageId;

		private System.Drawing.Image _image;

		private string _imageGraphics;

		private string _readString;

		private string _xmlResult;

		private byte[] _imageBytes;

		private List<ResultInfo> _subResults;

		public bool HasImage
		{
			get
			{
				if (this._image != null)
				{
					return true;
				}
				return this._imageBytes != null;
			}
		}

		public bool HasImageId
		{
			get
			{
				bool flag;
				if (this._imageId > 0)
				{
					return true;
				}
				if (this._subResults != null)
				{
					List<ResultInfo>.Enumerator enumerator = this._subResults.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							if (!enumerator.Current.HasImageId)
							{
								continue;
							}
							flag = true;
							return flag;
						}
						return false;
					}
					finally
					{
						((IDisposable)enumerator).Dispose();
					}
					return flag;
				}
				return false;
			}
		}

		public System.Drawing.Image Image
		{
			get
			{
				if (this._image != null)
				{
					return this._image;
				}
				this._image = ImageArrivedEventArgs.GetImageFromImageBytes(this._imageBytes);
				return this._image;
			}
			set
			{
				this._image = value;
			}
		}

		public byte[] ImageBytes
		{
			get
			{
				return this._imageBytes;
			}
			set
			{
				this._imageBytes = value;
			}
		}

		public string ImageGraphics
		{
			get
			{
				return this._imageGraphics;
			}
			set
			{
				this._imageGraphics = value;
			}
		}

		public int ImageId
		{
			get
			{
				return this._imageId;
			}
			set
			{
				this._imageId = value;
			}
		}

		public string ReadString
		{
			get
			{
				return this._readString;
			}
			set
			{
				this._readString = value;
			}
		}

		public DateTime ResultArrivedAt
		{
			get;
			private set;
		}

		public int ResultId
		{
			get
			{
				return this._resultId;
			}
			set
			{
				this._resultId = value;
			}
		}

		public List<ResultInfo> SubResults
		{
			get
			{
				return this._subResults;
			}
			set
			{
				this._subResults = value;
			}
		}

		public string XmlResult
		{
			get
			{
				return this._xmlResult;
			}
			set
			{
				this._xmlResult = value;
			}
		}

		public ResultInfo(int resultId, int imageId, string imageGraphics, string readString, string readXml, System.Drawing.Image image, byte[] imageBytes) : this(resultId, imageId, image, imageGraphics, readString, readXml)
		{
			this.ImageBytes = imageBytes;
		}

		public ResultInfo(int resultId, int imageId, System.Drawing.Image image, string imageGraphics, string readString, string readXml)
		{
			this._resultId = resultId;
			this._imageId = imageId;
			this._image = image;
			this._imageGraphics = imageGraphics;
			this._readString = readString;
			this._xmlResult = readXml;
			this._subResults = null;
			this.ResultArrivedAt = DateTime.Now;
		}

		public bool IsResultComplete(ResultTypes requiredResultTypes)
		{
			bool flag;
			bool flag1 = false;
			if (this._subResults != null)
			{
				foreach (ResultInfo _subResult in this._subResults)
				{
					if (!_subResult.HasImageId)
					{
						continue;
					}
					flag1 = true;
					break;
				}
			}
			if (!flag1)
			{
				if ((requiredResultTypes & ResultTypes.Image) != ResultTypes.None && !this.HasImage)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.ImageGraphics) != ResultTypes.None && this.ImageGraphics == null)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.ReadString) != ResultTypes.None && this.ReadString == null)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.ReadXml) != ResultTypes.None && this.XmlResult == null)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.TrainingResults) != ResultTypes.None)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.CodeQualityData) != ResultTypes.None)
				{
					return false;
				}
				if ((requiredResultTypes & ResultTypes.XmlStatistics) != ResultTypes.None)
				{
					return false;
				}
			}
			else
			{
				List<ResultInfo>.Enumerator enumerator = this._subResults.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.IsResultComplete(requiredResultTypes))
						{
							continue;
						}
						flag = false;
						return flag;
					}
					return true;
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
				return flag;
			}
			return true;
		}
	}
}