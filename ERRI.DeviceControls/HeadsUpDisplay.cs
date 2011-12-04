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
        private static double radianToAngleMultiplier = 57.2957795;
        private double ratioToDisplayAngleMultiplier = 0;
        private double fontSize;
        private ushort voltage;
        private ushort current;
        private double yawOffset;
        private int thrust;
		private Brush brush = Brushes.Green;
		private Brush gaugeBrush = Brushes.Green;
		private Brush falseHorizonBrush = Brushes.Green;
		private double thicknessBaseline = 2;
		private Pen baselinePen;
		private Pen accentedPen;
		private Pen gaugePen;
		private Pen falseHorizonPen;
        private Geometry falseHorizon;
        private DrawingGroup compass;
        private GeometryGroup compassLines;
        private GeometryGroup compassText;
		private Point directionTop = new Point();
		private Point directionBottom = new Point();
		private Point directionLineTop = new Point();
		private Point directionLineBottom = new Point();
        private Point directionLine10Bottom = new Point();
        private Point directionLine90Bottom = new Point();
        private Point directionLineLeftBoundTop = new Point();
        private Point directionLineLeftBoundBottom = new Point();
        private Point directionLineRightBoundTop = new Point();
        private Point directionLineRightBoundBottom = new Point();
		private FormattedText[] directionLine90FormattedText = new FormattedText[0];
		private Point directionLine90TextTop = new Point();
		private double directionLineSpacing = 0;
		private Point angleLeftLeft = new Point();
		private Point angleLeftRight = new Point();
		private Point angleRightLeft = new Point();
        private Point angleRightRight = new Point();
        private RotateTransform rollTransform = new RotateTransform(0);
        private RotateTransform pitchTransform = new RotateTransform(0);
        private TranslateTransform yawTransform = new TranslateTransform(0, 0);
        private TranslateTransform invertedYawTransform = new TranslateTransform(0, 0);
        private Geometry compassClippingRectangle = new RectangleGeometry();
        private FormattedText temperatureFormattedText = null;
        private FormattedText voltageFormattedText = null;
        private FormattedText currentFormattedText = null;
        private FormattedText thrustFormattedText = null;
        private OrientationMatrix orientation;
		private byte Scale {
			get;
			set;
		}
        public double Roll
        {
            get
            {
                return rollTransform.Angle;
            }
            set
            {
                rollTransform.Angle = value * 57.2957795;
                this.InvalidateVisual();
            }
        }
        public double Pitch
        {
            get
            {
                return pitchTransform.Angle / 57.2957795;
            }
            set
            {
                pitchTransform.Angle = value * 57.2957795;
                this.InvalidateVisual();
            }
        }
        public double Yaw
        {
            get
            {
                return yawTransform.X / ratioToDisplayAngleMultiplier;
            }
            set
            {
                invertedYawTransform.X = (yawTransform.X = (value + yawOffset) * ratioToDisplayAngleMultiplier) * -1;
                this.InvalidateVisual();
            }
        }
        public double YawOffset
        {
            get
            {
                return yawOffset;
            }
            set
            {
                yawOffset = value;
            }
        }
        public ushort Temperature
        {
            get;
            private set;
        }
        public ushort Current
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                this.InvalidateVisual();
            }
        }
        public ushort Voltage
        {
            get
            {
                return voltage;
            }
            set
            {
                voltage = value;
                this.InvalidateVisual();
            }
        }

        public int Thrust
        {
            get
            {
                return thrust;
            }
            set
            {
                thrust = value;
                this.InvalidateVisual();
            }
        }

		protected override void OnRender(DrawingContext context) {
            base.OnRender(context);
			context.DrawLine(baselinePen, angleLeftLeft, angleLeftRight);
            context.DrawLine(baselinePen, angleRightLeft, angleRightRight);
            context.DrawLine(baselinePen, directionLineLeftBoundTop, directionLineLeftBoundBottom);
            context.DrawLine(baselinePen, directionLineRightBoundTop, directionLineRightBoundBottom);
            context.DrawLine(baselinePen, directionTop, directionBottom);
            RenderGauge(context, Temperature, 1, temperatureFormattedText);
            RenderGauge(context, Thrust, 5, thrustFormattedText);
            RenderGauge(context, Voltage, 8, voltageFormattedText);
            RenderGauge(context, Current, 9, currentFormattedText);
            RenderCompass(context);
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
            double quarterWidth = width / 4;
            double quarterHeight = height / 4;
			double baseLine = height * .035;
			fontSize = height / (1024 / 20);
            ratioToDisplayAngleMultiplier = width / 360 * radianToAngleMultiplier;
			baselinePen = new Pen(brush, thicknessBaseline);
			accentedPen = new Pen(brush, thicknessBaseline * 2);
			gaugeBrush = brush.Clone();
			gaugeBrush.Opacity = .5;
			gaugePen = new Pen(gaugeBrush, width * .01 * thicknessBaseline);
			falseHorizonBrush = brush.Clone();
			falseHorizonBrush.Opacity = .5;
            falseHorizonPen = new Pen(falseHorizonBrush, thicknessBaseline);
            compassLines = new GeometryGroup();
            compassText = new GeometryGroup();
            directionTop = new Point(halfWidth, 0);
            directionBottom = new Point(halfWidth, baseLine);
            directionLineTop = new Point(0, 0);
            directionLineBottom = new Point(0, height * .01);
            directionLineLeftBoundTop = new Point(quarterWidth, height * 0);
            directionLineLeftBoundBottom = new Point(quarterWidth, height * .03);
            directionLineRightBoundTop = new Point(halfWidth + quarterWidth, height * 0);
            directionLineRightBoundBottom = new Point(halfWidth + quarterWidth, height * .03);
            directionLine10Bottom = new Point(0, height * .02);
            directionLine90Bottom = new Point(0, height * .03);
            directionLine90TextTop = new Point(0, height * .04);
            directionLine90FormattedText = new FormattedText[]{
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
				},
				new FormattedText("W", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush){
					TextAlignment = TextAlignment.Center
				}
			};
            directionLineSpacing = width / 180;

            Point lineBottom;
            double compassOffset = width / 2 + directionLineSpacing * 7;
            for (int i = 0; i <= 360; i++)
            {
                double spacing = i * directionLineSpacing - compassOffset;
                directionLineTop.X = spacing;
                if ((i % 45) == 0)
                {
                    directionLine90Bottom.X = spacing;
                    lineBottom = directionLine90Bottom;
                    if (directionLine90FormattedText.Length > 0)
                    {
                        directionLine90TextTop.X = spacing;
                        compassText.Children.Add(directionLine90FormattedText[i / 45].BuildGeometry(directionLine90TextTop));
                    }
                }
                else if (i % 5 == 0)
                {
                    directionLine10Bottom.X = spacing;
                    lineBottom = directionLine10Bottom;
                }
                else
                {
                    continue;
                }
                compassLines.Children.Add(new LineGeometry(directionLineTop, lineBottom));
            }
            compass = new DrawingGroup();
            compass.Children.Add(new GeometryDrawing(baselinePen.Brush, baselinePen, compassLines));
            compass.Children.Add(new GeometryDrawing(brush, null, compassText));
            compassClippingRectangle = new RectangleGeometry(new Rect()
            {
                Height = directionBottom.Y * 2,
                Width = halfWidth,
                X = halfWidth / 2
            });

			angleLeftLeft = new Point(0, halfHeight);
			angleLeftRight = new Point(baseLine, halfHeight);
			angleRightLeft = new Point(width - baseLine, halfHeight);
			angleRightRight = new Point(width, halfHeight);
			temperatureFormattedText = new FormattedText("Temperature", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
			{
					TextAlignment = TextAlignment.Center
            };
            Temperature = 50;
            currentFormattedText = new FormattedText("Current", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            Current = 50;
            voltageFormattedText = new FormattedText("Voltage", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            Voltage = 50;
            thrustFormattedText = new FormattedText("Thrust", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            Thrust = 0;
			falseHorizon = new GeometryGroup()
			{
				Children = new GeometryCollection()
				{
					new LineGeometry(new Point(0, halfHeight), new Point(width * .1, halfHeight)),
					new LineGeometry(new Point(width * .1, halfHeight), new Point(width * .13, halfHeight + halfHeight * .13)),
					new LineGeometry(new Point(width * .90, halfHeight), new Point(width, halfHeight)),
					new LineGeometry(new Point(width * .87, halfHeight + halfHeight * .13), new Point(width * .90, halfHeight)),
					new LineGeometry(new Point(halfWidth - (width * .03), halfHeight), new Point(halfWidth + (width * .03), halfHeight)),
					new LineGeometry(new Point(halfWidth - (width * .02), halfHeight + (halfHeight * .05)), new Point(halfWidth + (width * .02), halfHeight + (halfHeight * .05))),
					new LineGeometry(new Point(halfWidth - (width * .01), halfHeight + (halfHeight * .1)), new Point(halfWidth + (width * .01), halfHeight + (halfHeight * .1)))
				}
			};
            rollTransform.CenterX = halfWidth;
            rollTransform.CenterY = halfHeight;
            falseHorizon.Transform = rollTransform;
			this.InvalidateVisual();
		}

        protected void RenderCompass(DrawingContext context) {
            if (baselinePen != null)
            {
                compassClippingRectangle.Transform = invertedYawTransform;
                compass.Transform = yawTransform;
                compass.ClipGeometry = compassClippingRectangle;
                context.DrawDrawing(compass);
            }
        }

		protected void RenderFalseHorizon(DrawingContext context) {
            if (falseHorizonPen != null)
            {
                falseHorizon.Transform = rollTransform;
				context.DrawGeometry(falseHorizonPen.Brush, falseHorizonPen, falseHorizon);
			}
		}

		protected void RenderGauge(DrawingContext context, double value, int position, FormattedText label) {
            if (falseHorizonPen != null)
            {
                Point location = new Point(this.ActualWidth / 10 * position, label == null ? this.ActualHeight : this.ActualHeight - (label.Height * 1.5));
                context.DrawText(label, location);
                location.Y -= label == null ? 0 : label.Height * 1;
                context.DrawText(new FormattedText(value.ToString(), System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
                {
                    TextAlignment = TextAlignment.Center
                }, location);
            }
		}
	}
}
