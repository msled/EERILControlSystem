using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EERIL.DeviceControls
{
    public class HeadsUpDisplay : Canvas
    {
        private const double RadianToAngleMultiplier = 57.2957795;
        private readonly Brush brush = Brushes.Green;
        private readonly TranslateTransform invertedYawTransform = new TranslateTransform(0, 0);
        private readonly RotateTransform pitchTransform = new RotateTransform(0);
        private readonly RotateTransform rollTransform = new RotateTransform(0);
        private readonly TranslateTransform yawTransform = new TranslateTransform(0, 0);
        private Pen accentedPen;
        private Point angleLeftLeft;
        private Point angleLeftRight;
        private Point angleRightLeft;
        private Point angleRightRight;
        private Pen baselinePen;
        private DrawingGroup compass;
        private Geometry compassClippingRectangle = new RectangleGeometry();
        private GeometryGroup compassLines;
        private GeometryGroup compassText;
        private float current;
        private FormattedText currentFormattedText;
        private Point directionBottom;
        private Point directionLine10Bottom;
        private Point directionLine90Bottom;
        private FormattedText[] directionLine90FormattedText = new FormattedText[0];
        private Point directionLine90TextTop;
        private Point directionLineBottom;
        private Point directionLineLeftBoundBottom;
        private Point directionLineLeftBoundTop;
        private Point directionLineRightBoundBottom;
        private Point directionLineRightBoundTop;
        private double directionLineSpacing;
        private Point directionLineTop;
        private Point directionTop;
        private Geometry falseHorizon;
        private Brush falseHorizonBrush = Brushes.Green;
        private Pen falseHorizonPen;
        private double fontSize;
        private Brush gaugeBrush = Brushes.Green;
        private Pen gaugePen;
        private float humidity;
        private FormattedText humidityFormattedText;
        private OrientationMatrix orientation;
        private double ratioToDisplayAngleMultiplier;
        private float temperature;
        private FormattedText temperatureFormattedText;
        private double thicknessBaseline = 2;
        private int thrust;
        private FormattedText thrustFormattedText;
        private float voltage;
        private FormattedText voltageFormattedText;
        private double yawOffset;
        private byte Scale { get; set; }

        public double Roll
        {
            get { return rollTransform.Angle; }
            set
            {
                rollTransform.Angle = value*57.2957795;
                InvalidateVisual();
            }
        }

        public double Pitch
        {
            get { return pitchTransform.Angle/57.2957795; }
            set
            {
                pitchTransform.Angle = value*57.2957795;
                InvalidateVisual();
            }
        }

        public double Yaw
        {
            get { return yawTransform.X/ratioToDisplayAngleMultiplier; }
            set
            {
                invertedYawTransform.X = (yawTransform.X = (value + yawOffset)*ratioToDisplayAngleMultiplier)*-1;
                InvalidateVisual();
            }
        }

        public double YawOffset
        {
            get { return yawOffset; }
            set { yawOffset = value; }
        }

        public float Temperature
        {
            get { return temperature; }
            set
            {
                temperature = value;
                InvalidateVisual();
            }
        }

        public float Humidity
        {
            get { return humidity; }
            set
            {
                humidity = value;
                InvalidateVisual();
            }
        }

        public float Current
        {
            get { return current; }
            set
            {
                current = value;
                InvalidateVisual();
            }
        }

        public float Voltage
        {
            get { return voltage; }
            set
            {
                voltage = value;
                InvalidateVisual();
            }
        }

        public int Thrust
        {
            get { return thrust; }
            set
            {
                thrust = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);
            context.DrawLine(baselinePen, angleLeftLeft, angleLeftRight);
            context.DrawLine(baselinePen, angleRightLeft, angleRightRight);
            context.DrawLine(baselinePen, directionLineLeftBoundTop, directionLineLeftBoundBottom);
            context.DrawLine(baselinePen, directionLineRightBoundTop, directionLineRightBoundBottom);
            context.DrawLine(baselinePen, directionTop, directionBottom);
            RenderGauge(context, Math.Round(Temperature, 2), 1, temperatureFormattedText);
            RenderGauge(context, Math.Round(Humidity, 2), 2, humidityFormattedText);
            RenderGauge(context, Thrust, 5, thrustFormattedText);
            RenderGauge(context, Math.Round(Voltage, 2), 8, voltageFormattedText);
            RenderGauge(context, Math.Round(Current, 2), 9, currentFormattedText);
            RenderCompass(context);
            RenderFalseHorizon(context);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            double width = ActualWidth;
            double height = ActualHeight;
            if (height == 0.0 || width == 0.0)
            {
                return;
            }
            double halfWidth = width/2;
            double halfHeight = height/2;
            double quarterWidth = width/4;
            double quarterHeight = height/4;
            double baseLine = height*.035;
            fontSize = height/(1024/20);
            ratioToDisplayAngleMultiplier = width/360*RadianToAngleMultiplier;
            baselinePen = new Pen(brush, thicknessBaseline);
            accentedPen = new Pen(brush, thicknessBaseline*2);
            gaugeBrush = brush.Clone();
            gaugeBrush.Opacity = .5;
            gaugePen = new Pen(gaugeBrush, width*.01*thicknessBaseline);
            falseHorizonBrush = brush.Clone();
            falseHorizonBrush.Opacity = .5;
            falseHorizonPen = new Pen(falseHorizonBrush, thicknessBaseline);
            compassLines = new GeometryGroup();
            compassText = new GeometryGroup();
            directionTop = new Point(halfWidth, 0);
            directionBottom = new Point(halfWidth, baseLine);
            directionLineTop = new Point(0, 0);
            directionLineBottom = new Point(0, height*.01);
            directionLineLeftBoundTop = new Point(quarterWidth, height*0);
            directionLineLeftBoundBottom = new Point(quarterWidth, height*.03);
            directionLineRightBoundTop = new Point(halfWidth + quarterWidth, height*0);
            directionLineRightBoundBottom = new Point(halfWidth + quarterWidth, height*.03);
            directionLine10Bottom = new Point(0, height*.02);
            directionLine90Bottom = new Point(0, height*.03);
            directionLine90TextTop = new Point(0, height*.04);
            directionLine90FormattedText = new[]
                                               {
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("N", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("E", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("S", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("N", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("E", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("S", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, new Typeface("Courier"),
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       }
                                               };
            directionLineSpacing = width/180;

            Point lineBottom;
            double compassOffset = width/2 + directionLineSpacing*7;
            for (int i = 0; i <= 360; i++)
            {
                double spacing = i*directionLineSpacing - compassOffset;
                directionLineTop.X = spacing;
                if ((i%45) == 0)
                {
                    directionLine90Bottom.X = spacing;
                    lineBottom = directionLine90Bottom;
                    if (directionLine90FormattedText.Length > 0)
                    {
                        directionLine90TextTop.X = spacing;
                        compassText.Children.Add(directionLine90FormattedText[i/45].BuildGeometry(directionLine90TextTop));
                    }
                }
                else if (i%5 == 0)
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
            compassClippingRectangle = new RectangleGeometry(new Rect
                                                                 {
                                                                     Height = directionBottom.Y*2,
                                                                     Width = halfWidth,
                                                                     X = halfWidth/2
                                                                 });

            angleLeftLeft = new Point(0, halfHeight);
            angleLeftRight = new Point(baseLine, halfHeight);
            angleRightLeft = new Point(width - baseLine, halfHeight);
            angleRightRight = new Point(width, halfHeight);
            temperatureFormattedText = new FormattedText("Temperature", CultureInfo.CurrentUICulture,
                                                         FlowDirection.LeftToRight, new Typeface("Courier"), fontSize,
                                                         brush)
                                           {
                                               TextAlignment = TextAlignment.Center
                                           };
            humidityFormattedText = new FormattedText("Humidity", CultureInfo.CurrentUICulture,
                                                      FlowDirection.LeftToRight, new Typeface("Courier"), fontSize,
                                                      brush)
                                        {
                                            TextAlignment = TextAlignment.Center
                                        };
            currentFormattedText = new FormattedText("Current", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                     new Typeface("Courier"), fontSize, brush)
                                       {
                                           TextAlignment = TextAlignment.Center
                                       };
            voltageFormattedText = new FormattedText("Voltage", CultureInfo.CurrentUICulture,
                                                     FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
                                       {
                                           TextAlignment = TextAlignment.Center
                                       };
            thrustFormattedText = new FormattedText("Thrust", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
                                      {
                                          TextAlignment = TextAlignment.Center
                                      };
            falseHorizon = new GeometryGroup
                               {
                                   Children = new GeometryCollection
                                                  {
                                                      new LineGeometry(new Point(0, halfHeight),
                                                                       new Point(width*.1, halfHeight)),
                                                      new LineGeometry(new Point(width*.1, halfHeight),
                                                                       new Point(width*.13, halfHeight + halfHeight*.13)),
                                                      new LineGeometry(new Point(width*.90, halfHeight),
                                                                       new Point(width, halfHeight)),
                                                      new LineGeometry(
                                                          new Point(width*.87, halfHeight + halfHeight*.13),
                                                          new Point(width*.90, halfHeight)),
                                                      new LineGeometry(new Point(halfWidth - (width*.03), halfHeight),
                                                                       new Point(halfWidth + (width*.03), halfHeight)),
                                                      new LineGeometry(
                                                          new Point(halfWidth - (width*.02),
                                                                    halfHeight + (halfHeight*.05)),
                                                          new Point(halfWidth + (width*.02),
                                                                    halfHeight + (halfHeight*.05))),
                                                      new LineGeometry(
                                                          new Point(halfWidth - (width*.01),
                                                                    halfHeight + (halfHeight*.1)),
                                                          new Point(halfWidth + (width*.01),
                                                                    halfHeight + (halfHeight*.1)))
                                                  }
                               };
            rollTransform.CenterX = halfWidth;
            rollTransform.CenterY = halfHeight;
            falseHorizon.Transform = rollTransform;
            InvalidateVisual();
        }

        protected void RenderCompass(DrawingContext context)
        {
            if (baselinePen != null)
            {
                compassClippingRectangle.Transform = invertedYawTransform;
                compass.Transform = yawTransform;
                compass.ClipGeometry = compassClippingRectangle;
                context.DrawDrawing(compass);
            }
        }

        protected void RenderFalseHorizon(DrawingContext context)
        {
            if (falseHorizonPen != null)
            {
                falseHorizon.Transform = rollTransform;
                context.DrawGeometry(falseHorizonPen.Brush, falseHorizonPen, falseHorizon);
            }
        }

        protected void RenderGauge(DrawingContext context, double value, int position, FormattedText label)
        {
            if (falseHorizonPen != null)
            {
                var location = new Point(ActualWidth/10*position,
                                         label == null ? ActualHeight : ActualHeight - (label.Height*1.5));
                context.DrawText(label, location);
                location.Y -= label == null ? 0 : label.Height*1;
                context.DrawText(
                    new FormattedText(value.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                      new Typeface("Courier"), fontSize, brush)
                        {
                            TextAlignment = TextAlignment.Center
                        }, location);
            }
        }
    }
}