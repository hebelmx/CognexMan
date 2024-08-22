using System;
using System.Drawing;

namespace Cognex.DataMan.SDK.Utils
{
	public class ResultPolygon
	{
		public static System.Drawing.Color DefaultPolygonColor;

		public Point[] Points;

		public System.Drawing.Color Color;

		static ResultPolygon()
		{
			ResultPolygon.DefaultPolygonColor = System.Drawing.Color.LawnGreen;
		}

		public ResultPolygon()
		{
			this.Clear();
		}

		internal void Clear()
		{
			this.Points = new Point[0];
			this.Color = ResultPolygon.DefaultPolygonColor;
		}

		internal void Set(Point[] points)
		{
			this.Points = points;
		}
	}
}