using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cognex.DataMan.SDK.Utils
{
	public class ResultGraphics
	{
		public List<ResultPolygon> Polygons;

		public Size ViewBoxSize;

		public string OriginalSvgData;

		public ResultGraphics()
		{
			this.Polygons = new List<ResultPolygon>();
			this.ViewBoxSize = new Size(1280, 1024);
			this.OriginalSvgData = "";
		}
	}
}