using System;
using System.Collections.Generic;
using System.Drawing;

namespace Cognex.DataMan.SDK.Utils
{
	public static class ResultGraphicsRenderer
	{
		public static void PaintResults(Graphics graphics, ResultGraphics resultGraphics)
		{
			if (resultGraphics != null && resultGraphics.Polygons != null && resultGraphics.Polygons.Count > 0)
			{
				Pen pen = new Pen(resultGraphics.Polygons[0].Color);
				foreach (ResultPolygon polygon in resultGraphics.Polygons)
				{
					if (!pen.Color.Equals(polygon.Color))
					{
						pen.Color = polygon.Color;
					}
					graphics.DrawLines(pen, polygon.Points);
				}
			}
		}
	}
}