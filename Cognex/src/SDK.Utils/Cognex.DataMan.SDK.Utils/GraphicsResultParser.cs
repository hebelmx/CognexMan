using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cognex.DataMan.SDK.Utils
{
	public static class GraphicsResultParser
	{
		private static Regex m_RegexpViewBox;

		static GraphicsResultParser()
		{
			GraphicsResultParser.m_RegexpViewBox = new Regex("(\\d+)\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)");
		}

		public static ResultGraphics Parse(string svgData, Rectangle displayControlRect)
		{
			double num;
			ResultGraphics resultGraphic = new ResultGraphics()
			{
				OriginalSvgData = svgData
			};
			int num1 = svgData.IndexOf("viewBox=\"");
			if (num1 > 0)
			{
				Match match = GraphicsResultParser.m_RegexpViewBox.Match(svgData, num1);
				if (match.Groups.Count > 4)
				{
					resultGraphic.ViewBoxSize = new Size(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
				}
			}
			Rectangle rectangle = Gui.FitImageInControl(resultGraphic.ViewBoxSize, displayControlRect, out num);
			Point point = new Point((displayControlRect.Width - rectangle.Width) / 2, (displayControlRect.Height - rectangle.Height) / 2);
			int length = svgData.Length;
			int num2 = svgData.IndexOf("points", 0, length);
			int num3 = svgData.IndexOf("stroke=\"#", 0, length);
			while (num2 != -1)
			{
				ResultPolygon resultPolygon = new ResultPolygon();
				bool flag = false;
				num3 = svgData.IndexOf("stroke=\"#", num3, length - num3);
				if (num3 >= 0)
				{
					try
					{
						uint num4 = uint.Parse(svgData.Substring(num3 + 9, 6), NumberStyles.HexNumber);
						resultPolygon.Color = GraphicsResultParser.UIntToColor(num4);
						num3 += 9;
						if (resultPolygon.Color.R == 0 && resultPolygon.Color.G == 0 && resultPolygon.Color.B == 255)
						{
							flag = true;
						}
					}
					catch
					{
					}
				}
				List<Point> points = new List<Point>();
				int num5 = svgData.IndexOf("points", num2, length - num2) + 8;
				int num6 = svgData.IndexOf('\"', num5, length - num5) - 1;
				string str = svgData.Substring(num5, num6 - num5);
				char[] chrArray = new char[] { ' ', ',' };
				string[] strArrays = str.Split(chrArray);
				Point point1 = new Point();
				for (int i = 0; i < (int)strArrays.Length; i += 2)
				{
					int num7 = (int)Math.Round((double)Convert.ToInt32(strArrays[i]) * num) + point.X;
					int num8 = (int)Math.Round((double)Convert.ToInt32(strArrays[i + 1]) * num) + point.Y;
					if (flag)
					{
						if (num7 != 0)
						{
							num7--;
						}
						if (num8 != 0)
						{
							num8--;
						}
					}
					point1 = new Point(num7, num8);
					points.Add(point1);
				}
				if (points.Count > 0)
				{
					points.Add(points[0]);
				}
				resultPolygon.Set(points.ToArray());
				resultGraphic.Polygons.Add(resultPolygon);
				num2 = svgData.IndexOf("points", num2 + 1, length - num2 - 1);
			}
			return resultGraphic;
		}

		public static Color UIntToColor(uint argbValue)
		{
			return Color.FromArgb(255, (int)(argbValue >> 16 & 255), (int)(argbValue >> 8 & 255), (int)(argbValue & 255));
		}
	}
}