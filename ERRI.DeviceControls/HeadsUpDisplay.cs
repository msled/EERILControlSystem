using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EERIL.DeviceControls {
	public class HeadsUpDisplay : Canvas {
		private Brush brush = Brushes.Green;
		private Brush gaugeBrush = Brushes.Green;
		private Brush falseHorizonBrush = Brushes.Green;
		private Brush falseHorizonTransparentBrush = Brushes.Green;
		private double thicknessBaseline = 2;
		private Pen baselinePen;
		private Pen accentedPen;
		private Pen gaugePen;
		private Pen falseHorizonPen;
		private Pen falseHorizonTransparentPen;
		private Geometry falseHorizon;
		private Geometry falseHorizonTransparent;
		private Point directionTop = new Point();
		private Point directionBottom = new Point();
		private Point directionLineTop = new Point();
		private Point directionLineBottom = new Point();
		private Point directionLine10Bottom = new Point();
		private Point directionLine90Bottom = new Point();
		private FormattedText[] directionLine90FormattedText = new FormattedText[0];
		private Point directionLine90TextTop = new Point();
		private double directionLineSpacing = 0;
		private Point angleLeftLeft = new Point();
		private Point angleLeftRight = new Point();
		private Point angleRightLeft = new Point();
		private Point angleRightRight = new Point();
		private FormattedText temperatureFormattedText = null;
		private byte Scale {
			get;
			set;
		}
		public Point3D Acceleration {
			get;
			private set;
		}
		public Point3D Magnetometer {
			get;
			private set;
		}
		public Point3D AngleRate {
			get;
			private set;
		}
		public Matrix Orientation {
			get;
			private set;
		}
		public ushort Temperature {
			get;
			private set;
		}

		protected override void OnRender(DrawingContext context) {
            base.OnRender(context);
			context.DrawLine(baselinePen, directionTop, directionBottom);
			context.DrawLine(baselinePen, angleLeftLeft, angleLeftRight);
			context.DrawLine(baselinePen, angleRightLeft, angleRightRight);
			Point lineBottom;
			for (int i = 23; i < 50; i++) {
				double spacing = i * directionLineSpacing;
				directionLineTop.X = spacing;
				if (i == 0 || (i % 18) == 0) {
					directionLine90Bottom.X = spacing;
					lineBottom = directionLine90Bottom;
					if (directionLine90FormattedText.Length > 0) {
						directionLine90TextTop.X = spacing;
						context.DrawText(directionLine90FormattedText[i / 18], directionLine90TextTop);
					}
				} else if (i % 2 == 0) {
					directionLine10Bottom.X = spacing;
					lineBottom = directionLine10Bottom;
				} else {
					directionLineBottom.X = spacing;
					lineBottom = directionLineBottom;
				}
				context.DrawLine(baselinePen, directionLineTop, lineBottom);
			}
			RenderGauge(context, Temperature, 5, temperatureFormattedText);
			RenderFalseHorizon(context);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            double width = this.ActualWidth;
            double height = this.ActualHeight;
            if (height == 0.0 || width == 0.0)
            {
                return;
            }
			double halfWidth = width / 2;
			double halfHeight = height / 2;
			double baseLine = height * .05;
			double fontSize = height / (1024 / 14);
			baselinePen = new Pen(brush, thicknessBaseline);
			accentedPen = new Pen(brush, thicknessBaseline * 2);
			gaugeBrush = brush.Clone();
			gaugeBrush.Opacity = .5;
			gaugePen = new Pen(gaugeBrush, width * .01 * thicknessBaseline);
			falseHorizonBrush = brush.Clone();
			falseHorizonBrush.Opacity = .5;
			falseHorizonPen = new Pen(falseHorizonBrush, thicknessBaseline);
			falseHorizonTransparentBrush = brush.Clone();
			falseHorizonTransparentBrush.Opacity = .35;
			falseHorizonTransparentPen = new Pen(falseHorizonTransparentBrush, thicknessBaseline);
			directionTop = new Point(halfWidth, 0);
			directionBottom = new Point(halfWidth, baseLine);
			directionLineTop = new Point(0, 0);
			directionLineBottom = new Point(0, height * .025);
			directionLine10Bottom = new Point(0, height * .035);
			directionLine90Bottom = new Point(0, height * .045);
			directionLine90TextTop = new Point(0, height * .055);
			directionLine90FormattedText = new FormattedText[]{
				new FormattedText("S", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				},
				new FormattedText("W", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				},
				new FormattedText("N", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				},
				new FormattedText("E", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				},
				new FormattedText("S", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				}
			};
			directionLineSpacing = width / 72;
			angleLeftLeft = new Point(0, halfHeight);
			angleLeftRight = new Point(baseLine, halfHeight);
			angleRightLeft = new Point(width - baseLine, halfHeight);
			angleRightRight = new Point(width, halfHeight);
			temperatureFormattedText = new FormattedText("Temperature", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
			{
					TextAlignment = TextAlignment.Center
				};
			Temperature = 50;
			falseHorizon = new GeometryGroup()
			{
				Children = new GeometryCollection()
				{
					new LineGeometry(new Point(0, halfHeight), new Point(width * .1, halfHeight)),
					new LineGeometry(new Point(width * .1, halfHeight), new Point(width * .13, halfHeight + halfHeight * .13)),
					new LineGeometry(new Point(width * .90, halfHeight), new Point(width, halfHeight)),
					new LineGeometry(new Point(width * .87, halfHeight + halfHeight * .13), new Point(width * .90, halfHeight))
				}
			};
			falseHorizonTransparent = new GeometryGroup()
			{
				Children = new GeometryCollection()
				{
					new LineGeometry(new Point(halfWidth - (width * .03), halfHeight), new Point(halfWidth + (width * .03), halfHeight)),
					new LineGeometry(new Point(halfWidth - (width * .02), halfHeight + (halfHeight * .05)), new Point(halfWidth + (width * .02), halfHeight + (halfHeight * .05))),
					new LineGeometry(new Point(halfWidth - (width * .01), halfHeight + (halfHeight * .1)), new Point(halfWidth + (width * .01), halfHeight + (halfHeight * .1)))
				}
			};
			this.InvalidateVisual();
		}

		protected void RenderFalseHorizon(DrawingContext context) {
			if (falseHorizonPen != null) {
				context.DrawGeometry(falseHorizonPen.Brush, falseHorizonPen, falseHorizon);
				context.DrawGeometry(falseHorizonTransparentPen.Brush, falseHorizonTransparentPen, falseHorizonTransparent);
			}
		}

		protected void RenderGauge(DrawingContext context, double height, int position, FormattedText label) {
			Point location = new Point(this.ActualWidth / 10 * position, label == null ? this.ActualHeight : this.ActualHeight - (label.Height * 1.5));
			context.DrawText(label, location);
			location.Y -= label == null ? 0 : label.Height * .5;
			context.DrawLine(gaugePen, location, new Point(location.X, location.Y - height));
		}
	}
}
