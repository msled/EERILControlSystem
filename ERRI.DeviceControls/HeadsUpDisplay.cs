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
        private Typeface typeface = new Typeface("Courier");
        private Brush gaugeBrush = Brushes.Green;
        private Pen gaugePen;
        private float humidity;
        private FormattedText humidityFormattedText;
        private double ratioToDisplayAngleMultiplier;
        private float temperature;
        private double thicknessBaseline = 2;
        private int thrust;
        private float voltage;
        private FormattedText voltageFormattedText;
        private double yawOffset;
        private Geometry thrustGauge;
        private Rect thrustGaugeLine;
        double sideGaugeCenter;
        double sideGaugeTop;
        double sideGaugeBottom;
        private Geometry temperatureGauge;
        private Rect temperatureGaugeLine;
        private GeometryGroup pitchGauge;
        private byte Scale { get; set; }

        public double Roll
        {
            get { return rollTransform.Angle; }
            set
            {
                rollTransform.Angle = value * 57.2957795;
                InvalidateVisual();
            }
        }

        public double Pitch
        {
            get { return pitchTransform.Angle / 57.2957795; }
            set
            {
                pitchTransform.Angle = value * 57.2957795;
                InvalidateVisual();
            }
        }

        public double Yaw
        {
            get { return yawTransform.X / ratioToDisplayAngleMultiplier; }
            set
            {
                invertedYawTransform.X = (yawTransform.X = (value + yawOffset) * ratioToDisplayAngleMultiplier) * -1;
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
            RenderGauge(context, Math.Round(Humidity, 2), 1, humidityFormattedText);
            RenderGauge(context, Math.Round(Voltage, 2), 8, voltageFormattedText);
            RenderGauge(context, Math.Round(Current, 2), 9, currentFormattedText);
            RenderThrustGauge(context);
            RenderTemperatureGauge(context);
            RenderCompass(context);
            RenderFalseHorizon(context);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            double width = ActualWidth;
            double width005 = width * .005;
            double width015 = width * .015;
            double width02 = width * .02;
            double width995 = width - width005;
            double width985 = width - width015;
            double height = ActualHeight;
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
            ratioToDisplayAngleMultiplier = width / 360 * RadianToAngleMultiplier;
            baselinePen = new Pen(brush, thicknessBaseline);
            accentedPen = new Pen(brush, thicknessBaseline * 2);
            gaugeBrush = brush.Clone();
            gaugeBrush.Opacity = .5;
            gaugePen = new Pen(gaugeBrush, thicknessBaseline);
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
            directionLine90FormattedText = new[]
                                               {
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("N", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("E", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("S", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("N", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("E", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("S", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
                                                           TextAlignment = TextAlignment.Center
                                                       },
                                                   new FormattedText("W", CultureInfo.CurrentUICulture,
                                                                     FlowDirection.LeftToRight, typeface,
                                                                     fontSize, brush)
                                                       {
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
            compassClippingRectangle = new RectangleGeometry(new Rect
                                                                 {
                                                                     Height = directionBottom.Y * 2,
                                                                     Width = halfWidth,
                                                                     X = halfWidth / 2
                                                                 });

            angleLeftLeft = new Point(0, halfHeight);
            angleLeftRight = new Point(baseLine, halfHeight);
            angleRightLeft = new Point(width - baseLine, halfHeight);
            angleRightRight = new Point(width, halfHeight);
            humidityFormattedText = new FormattedText("Humidity %", CultureInfo.CurrentUICulture,
                                                      FlowDirection.LeftToRight, typeface, fontSize,
                                                      brush)
                                        {
                                            TextAlignment = TextAlignment.Center
                                        };
            currentFormattedText = new FormattedText("Current", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                     typeface, fontSize, brush)
                                       {
                                           TextAlignment = TextAlignment.Center
                                       };
            voltageFormattedText = new FormattedText("Voltage", CultureInfo.CurrentUICulture,
                                                     FlowDirection.LeftToRight, typeface, fontSize, brush)
                                       {
                                           TextAlignment = TextAlignment.Center
                                       };
            falseHorizon = new GeometryGroup
                               {
                                   Children = new GeometryCollection
                                                  {
                                                      new LineGeometry(new Point(0, halfHeight),
                                                                       new Point(width*.1, halfHeight)),
                                                      new LineGeometry(new Point(width*.90, halfHeight),
                                                                       new Point(width, halfHeight)),
                                                      new LineGeometry(new Point(halfWidth - (width*.03), halfHeight),
                                                                       new Point(halfWidth + (width*.03), halfHeight)),
                                                      new LineGeometry(
                                                          new Point(halfWidth,
                                                                    halfHeight - (height*.01)),
                                                          new Point(halfWidth,
                                                                    halfHeight + (height*.01)))
                                                  }
                               };
            rollTransform.CenterX = halfWidth;
            rollTransform.CenterY = halfHeight;
            falseHorizon.Transform = rollTransform;

            sideGaugeCenter = halfHeight;
            sideGaugeTop = sideGaugeCenter / 2;
            sideGaugeBottom = sideGaugeCenter + sideGaugeTop;
            double sideGaugeIncrement = (sideGaugeBottom - sideGaugeTop) / 18;
            double thrustGaugeCurrent = sideGaugeTop + sideGaugeIncrement;
            thrustGauge = new GeometryGroup
                               {
                                   Children = new GeometryCollection
                                                  {
                                                      new LineGeometry(
                                                          new Point(width985, sideGaugeTop),
                                                          new Point(width, sideGaugeTop)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += (sideGaugeIncrement * 2)),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width995, thrustGaugeCurrent += sideGaugeIncrement),
                                                          new Point(width, thrustGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width985, sideGaugeBottom),
                                                          new Point(width, sideGaugeBottom)
                                                          )
                                                  }
                               };
            thrustGaugeLine = new Rect(
                new Point(width - (width * .010), sideGaugeCenter),
                new Point(width, sideGaugeCenter)
            );

            double temperatureGaugeCurrent = sideGaugeTop + sideGaugeIncrement;
            temperatureGauge = new GeometryGroup
            {
                Children = new GeometryCollection
                                                  {
                                                      new LineGeometry(
                                                          new Point(width015, sideGaugeTop),
                                                          new Point(0, sideGaugeTop)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += (sideGaugeIncrement * 2)),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width005, temperatureGaugeCurrent += sideGaugeIncrement),
                                                          new Point(0, temperatureGaugeCurrent)
                                                          ),
                                                      new LineGeometry(
                                                          new Point(width015, sideGaugeBottom),
                                                          new Point(0, sideGaugeBottom)
                                                          )
                                                  }
            };
            temperatureGaugeLine = new Rect(
                new Point(width * .010, sideGaugeCenter),
                new Point(0, sideGaugeCenter)
            );

            pitchGauge = new GeometryGroup();
            double pitchGaugeLeftLineStart = width*.15;
            double pitchGaugeLeftLineEnd = width*.17;
            double pitchGaugeRightLineStart = width - pitchGaugeLeftLineEnd;
            double pitchGaugeRightLineEnd = width - pitchGaugeLeftLineStart;
            double pitchGaugeYSpace = height / 10;
            double pitchGaugeCurrentY = 0;
            FormattedText angleText;
            for (int angle = 36; angle > -37; angle--)
            {
                pitchGauge.Children.Add(new LineGeometry(
                    new Point(pitchGaugeLeftLineStart, pitchGaugeCurrentY),
                    new Point(pitchGaugeLeftLineEnd, pitchGaugeCurrentY)
                    )
                );
                pitchGauge.Children.Add(new LineGeometry(
                    new Point(pitchGaugeRightLineStart, pitchGaugeCurrentY),
                    new Point(pitchGaugeRightLineEnd, pitchGaugeCurrentY)
                    )
                );
                angleText = new FormattedText(angle.ToString() + "0°", CultureInfo.CurrentUICulture,
                                                     FlowDirection.LeftToRight, typeface, fontSize, baselinePen.Brush)
                                       {
                                           TextAlignment = TextAlignment.Center
                                       };
                pitchGauge.Children.Add(angleText.BuildGeometry(new Point(pitchGaugeRightLineEnd + width02, pitchGaugeCurrentY - (angleText.Height / 2))));
                pitchGauge.Children.Add(angleText.BuildGeometry(new Point(pitchGaugeLeftLineStart - width02, pitchGaugeCurrentY - (angleText.Height / 2))));
                pitchGaugeCurrentY += pitchGaugeYSpace;
            }

            InvalidateVisual();
        }

        protected void RenderTemperatureGauge(DrawingContext context)
        {
            if (baselinePen != null)
            {
                if (Temperature >= 0)
                {
                    temperatureGaugeLine.Y = ((sideGaugeBottom - sideGaugeTop) / 200 * Temperature * -1) + sideGaugeCenter;
                    temperatureGaugeLine.Height = sideGaugeCenter - temperatureGaugeLine.Y;
                }
                else
                {
                    temperatureGaugeLine.Y = sideGaugeCenter;
                    temperatureGaugeLine.Height = (sideGaugeBottom - sideGaugeTop) / 200 * Temperature * -1;
                }
                context.DrawGeometry(baselinePen.Brush, baselinePen, temperatureGauge);
                context.DrawRectangle(gaugeBrush, gaugePen, temperatureGaugeLine);
            }
        }

        protected void RenderThrustGauge(DrawingContext context)
        {
            if (baselinePen != null)
            {
                if (Thrust >= 0)
                {
                    thrustGaugeLine.Y = ((sideGaugeBottom - sideGaugeTop) / 180 * Thrust * -1) + sideGaugeCenter;
                    thrustGaugeLine.Height = sideGaugeCenter - thrustGaugeLine.Y;
                }
                else
                {
                    thrustGaugeLine.Y = sideGaugeCenter;
                    thrustGaugeLine.Height = (sideGaugeBottom - sideGaugeTop) / 180 * Thrust * -1;
                }
                context.DrawGeometry(baselinePen.Brush, baselinePen, thrustGauge);
                context.DrawRectangle(gaugeBrush, gaugePen, thrustGaugeLine);
            }
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
                context.DrawGeometry(falseHorizonBrush, falseHorizonPen, falseHorizon);
                context.DrawGeometry(falseHorizonBrush, falseHorizonPen, pitchGauge);
            }
        }

        protected void RenderGauge(DrawingContext context, double value, int position, FormattedText label)
        {
            if (falseHorizonPen != null)
            {
                var location = new Point(ActualWidth / 10 * position,
                                         label == null ? ActualHeight : ActualHeight - (label.Height * 1.5));
                context.DrawText(label, location);
                location.Y -= label == null ? 0 : label.Height * 1;
                context.DrawText(
                    new FormattedText(value.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                      typeface, fontSize, brush)
                        {
                            TextAlignment = TextAlignment.Center
                        }, location);
            }
        }
    }
}