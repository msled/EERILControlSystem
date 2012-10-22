using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;

namespace EERIL.DeviceControls
{
    public class HeadUpDisplay : Canvas
    {
        private const double RADIAN_TO_ANGLE_MULTIPLIER = 57.2957795;
        private readonly Brush brush = Brushes.Green;
        private readonly Brush warningBrush = Brushes.Red;
        private readonly Brush warning2Brush = Brushes.Orange;
        private readonly Brush screenshotBrush = Brushes.LightBlue;
        private readonly TranslateTransform pitchTransform = new TranslateTransform(0, 0);
        private readonly TranslateTransform invertedPitchTransform = new TranslateTransform(0, 0);
        private readonly RotateTransform rollTransform = new RotateTransform(0);
        private readonly RotateTransform invertedRollTransform = new RotateTransform(0);
        private readonly TranslateTransform yawTransform = new TranslateTransform(0, 0);
        private readonly TranslateTransform invertedYawTransform = new TranslateTransform(0, 0);
        private Point angleLeftLeft;
        private Point angleLeftRight;
        private Point angleRightLeft;
        private Point angleRightRight;
        private Pen baselinePen;
        private Pen warningPen;
        private Pen warning2Pen;
        private DrawingGroup compass;
        private RectangleGeometry compassClippingRectangle = new RectangleGeometry();
        private GeometryGroup compassLines;
        private GeometryGroup compassText;
        private float current;
        private FormattedText currentFormattedText;
        private Point directionBottom;
        private Point directionLine10Bottom;
        private Point directionLine90Bottom;
        private FormattedText[] directionLine90FormattedText = new FormattedText[0];
        private Point directionLine90TextTop;
        private Point directionLineLeftBoundBottom;
        private Point directionLineLeftBoundTop;
        private Point directionLineRightBoundBottom;
        private Point directionLineRightBoundTop;
        private double directionLineSpacing;
        private Point directionLineTop;
        private Point directionTop;
        private double fontSize;
        private double symbolfontSize;
        private readonly Typeface typeface = new Typeface("Courier");
        private Brush gaugeBrush = Brushes.Green;
        private Pen gaugePen;
        private float humidity;
        private FormattedText humidityFormattedText;
        private double ratioToDisplayAngleMultiplier;
        private float temperature;
        private const double THICKNESS_BASELINE = 2;
        private int thrust;
        private FormattedText thrustFormattedText;
        private float voltage;
        private FormattedText voltageFormattedText;
        private float fps;
        private FormattedText fpsFormattedText;
        private float salinity;
        private FormattedText salinityFormattedText;
        private float depth;
        private FormattedText depthFormattedText;
        private float extTemp;
        private FormattedText extTempFormattedText;
        private FormattedText warningFormattedText;
        private FormattedText warning2FormattedText;
        private FormattedText screenshotFormattedText;
        private FormattedText depthValueFormattedText;
       
        private bool screenshotAck;
        private bool lowVoltage;

        double sideGaugeCenter;
        double sideGaugeTop;
        double sideGaugeBottom;
        private FormattedText temperatureFormattedText;
        private readonly DrawingGroup pitchGauge = new DrawingGroup();
        private readonly DrawingGroup falseHorizonCrosshairs = new DrawingGroup();
        private Brush falseHorizonBrush;
        private Pen falseHorizonPen;
        PathGeometry maxInnerClippingRectangle;
        private readonly GeometryGroup mapGeometryGroup = new GeometryGroup();
        private double mapScale;
        private Pen mapPen;
        private Brush mapBrush;
        private double mapRadius;
        private readonly GeometryGroup locationGeometryGroup = new GeometryGroup();
        private readonly RotateTransform locationYawTransform = new RotateTransform();
        private readonly TranslateTransform locationTranslateTransform = new TranslateTransform();

        private byte Scale { get; set; }

        public double Roll
        {
            get { return rollTransform.Angle; }
            set
            {
                invertedRollTransform.Angle = (rollTransform.Angle = value * 57.2957795) * -1;
                InvalidateVisual();
            }
        }

        public double Pitch
        {
            get { return pitchTransform.Y / ratioToDisplayAngleMultiplier; }
            set
            {
                invertedPitchTransform.Y = (pitchTransform.Y = (value + PitchOffset) * ratioToDisplayAngleMultiplier) * -2;
                InvalidateVisual();
            }
        }

        public double PitchOffset
        {
            get;
            set;
        }

        public double Yaw
        {
            get { return yawTransform.X / ratioToDisplayAngleMultiplier; }
            set
            {
                yawTransform.X = (value + YawOffset) * ratioToDisplayAngleMultiplier;
                locationYawTransform.Angle = yawTransform.X;
                invertedYawTransform.X = yawTransform.X * -1;
                InvalidateVisual();
            }
        }

        public double YawOffset
        {
            get;
            set;
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

        public float Fps
        {
            get { return fps; }
            set
            {
                fps = value;
                InvalidateVisual();
            }
        }

        public float Salinity
        {
            get { return salinity; }
            set
            {
                salinity = value;
                InvalidateVisual();
            }
        }

        public float Depth
        {
            get { return depth; }
            set
            {
                depth = value;
                InvalidateVisual();
            }
        }

        public float ExtTemp
        {
            get { return extTemp; }
            set
            {
                extTemp = value;
                InvalidateVisual();
            }
        }

        public bool ScreenshotAck
        {
            get { return screenshotAck; }
            set
            {
                screenshotAck = value;
                InvalidateVisual();
            }
        }

        public bool LowVoltage
        {
            get { return lowVoltage; }
            set
            {
                lowVoltage = value;
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

            RenderGauge(context, Humidity.ToString("0.00"), 2, humidityFormattedText);
            RenderGauge(context, Temperature.ToString("0.00"), 6, temperatureFormattedText);
            RenderGauge(context, Fps.ToString("0.0"), 18, fpsFormattedText);
            RenderGauge(context, Voltage.ToString("0.00"), 4, voltageFormattedText);
            RenderGauge(context, Current.ToString("0.00"), 16, currentFormattedText);
            RenderGauge(context, Thrust.ToString("0"), 10, thrustFormattedText);
            RenderGauge(context, Salinity.ToString("0.00"), 8, salinityFormattedText);
            RenderGauge(context, Depth.ToString("0.00"), 12, depthFormattedText);
            RenderGauge(context, ExtTemp.ToString("0.00"), 14, extTempFormattedText);

            RenderDepthGauge(context);
            RenderBatteryBar(context);

            if (ScreenshotAck == true)
                RenderCTDValues(context, "Captured!", 2, screenshotFormattedText);
            RenderCompass(context);
            RenderFalseHorizon(context);
            //RenderMap(context);
            //RenderLocation(context);
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
            if (Math.Abs(height - 0.0) < double.Epsilon || Math.Abs(width - 0.0) < double.Epsilon)
            {
                return;
            }

            double halfWidth = width / 2;
            double halfHeight = height / 2;
            double quarterWidth = width / 4;
            double baseLine = height * .035;
            fontSize = height / (1024 / 15);
            symbolfontSize = height / (1024 / 70);
            ratioToDisplayAngleMultiplier = width / 360 * RADIAN_TO_ANGLE_MULTIPLIER;
            baselinePen = new Pen(brush, THICKNESS_BASELINE);
            warningPen = new Pen(warningBrush, THICKNESS_BASELINE);
            warning2Pen = new Pen(warning2Brush, THICKNESS_BASELINE);
            gaugeBrush = brush.Clone();
            gaugeBrush.Opacity = .5;
            gaugePen = new Pen(gaugeBrush, THICKNESS_BASELINE);
            falseHorizonBrush = brush.Clone();
            falseHorizonBrush.Opacity = 1;
            falseHorizonPen = new Pen(falseHorizonBrush, THICKNESS_BASELINE);
            compassLines = new GeometryGroup();
            compassText = new GeometryGroup();
            directionTop = new Point(halfWidth, 0);
            directionBottom = new Point(halfWidth, baseLine);
            directionLineTop = new Point(0, 0);
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
            Rect compassClippingRect = new Rect
            {
                Height = directionLine90TextTop.Y + fontSize,
                Width = halfWidth,
                X = halfWidth / 2
            };
            compassClippingRectangle = new RectangleGeometry(compassClippingRect);

            angleLeftLeft = new Point(0, halfHeight);
            angleLeftRight = new Point(baseLine, halfHeight);
            angleRightLeft = new Point(width - baseLine, halfHeight);
            angleRightRight = new Point(width, halfHeight);

            warningFormattedText = new FormattedText("LOW VOLTAGE!", CultureInfo.CurrentUICulture,
                                                      FlowDirection.LeftToRight, typeface, fontSize,
                                                      warningBrush)
            {
                TextAlignment = TextAlignment.Center
                
            };
            screenshotFormattedText = new FormattedText("Captured!", CultureInfo.CurrentUICulture,
                                                      FlowDirection.LeftToRight, typeface, fontSize,
                                                      screenshotBrush)
            {
                TextAlignment = TextAlignment.Center
            };
            temperatureFormattedText = new FormattedText("Temp C", CultureInfo.CurrentUICulture,
                                                      FlowDirection.LeftToRight, typeface, fontSize,
                                                      brush)
            {
                TextAlignment = TextAlignment.Center
            };
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
            fpsFormattedText = new FormattedText("FPS", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            thrustFormattedText = new FormattedText("Thrust", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            salinityFormattedText = new FormattedText("Salinity", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            depthFormattedText = new FormattedText("Depth", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };
            extTempFormattedText = new FormattedText("Ext Temperature", CultureInfo.CurrentUICulture,
                                                    FlowDirection.LeftToRight, new Typeface("Courier"), fontSize, brush)
            {
                TextAlignment = TextAlignment.Center
            };

            falseHorizonCrosshairs.Children.Clear();
            falseHorizonCrosshairs.Children.Add(new GeometryDrawing(falseHorizonBrush, falseHorizonPen, new GeometryGroup
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
            }));
            falseHorizonCrosshairs.Transform = rollTransform;
            rollTransform.CenterX = halfWidth;
            rollTransform.CenterY = halfHeight;
            invertedRollTransform.CenterX = halfWidth;
            invertedRollTransform.CenterY = halfHeight;

            double pitchGaugeLeftLineStart = width * .15;
            double pitchGaugeLeftLineEnd = width * .17;
            double pitchGaugeRightLineStart = width - pitchGaugeLeftLineEnd;
            double pitchGaugeRightLineEnd = width - pitchGaugeLeftLineStart;
            double pitchGaugeYSpace = height / 10;
            double pitchGaugeCurrentY = pitchGaugeYSpace * -31;
            FormattedText angleText;
            pitchGauge.Children.Clear();
            for (int passes = 3, angle = 0, increment = 10; passes > 0; passes--)
            {
                for (; (passes > 1 && angle > -180) || angle >= 0; angle -= increment)
                {
                    pitchGauge.Children.Add(new GeometryDrawing(falseHorizonBrush, falseHorizonPen, new LineGeometry(
                        new Point(pitchGaugeLeftLineStart, pitchGaugeCurrentY),
                        new Point(pitchGaugeLeftLineEnd, pitchGaugeCurrentY)
                        )
                    ));
                    pitchGauge.Children.Add(new GeometryDrawing(falseHorizonBrush, falseHorizonPen, new LineGeometry(
                        new Point(pitchGaugeRightLineStart, pitchGaugeCurrentY),
                        new Point(pitchGaugeRightLineEnd, pitchGaugeCurrentY)
                        )
                    ));
                    angleText = new FormattedText(angle.ToString(CultureInfo.InvariantCulture) + '°', CultureInfo.CurrentUICulture,
                                                         FlowDirection.LeftToRight, typeface, fontSize, baselinePen.Brush)
                    {
                        TextAlignment = TextAlignment.Center
                    };
                    pitchGauge.Children.Add(new GeometryDrawing(falseHorizonBrush, null, angleText.BuildGeometry(new Point(pitchGaugeRightLineEnd + width02, pitchGaugeCurrentY - (angleText.Height / 2)))));
                    pitchGauge.Children.Add(new GeometryDrawing(falseHorizonBrush, null, angleText.BuildGeometry(new Point(pitchGaugeLeftLineStart - width02, pitchGaugeCurrentY - (angleText.Height / 2)))));
                    pitchGaugeCurrentY += pitchGaugeYSpace;
                }
                angle = 180;
            }
            pitchGauge.Transform = new TransformGroup
            {
                Children = new TransformCollection{
                    pitchTransform,
                    rollTransform
                }
            };
            maxInnerClippingRectangle = new PathGeometry(new PathFigureCollection {
                new PathFigure(new Point(0,0), new PathSegmentCollection{
                    new LineSegment(new Point(0, 0), false),
                    new LineSegment(compassClippingRect.TopLeft, false),
                    new LineSegment(compassClippingRect.BottomLeft, false),
                    new LineSegment(compassClippingRect.BottomRight, false),
                    new LineSegment(compassClippingRect.TopRight, false),
                    new LineSegment(new Point(width, 0), false),
                    new LineSegment(new Point(width, sideGaugeTop), false),
                    new LineSegment(new Point(width - width015, sideGaugeTop), false),
                    new LineSegment(new Point(width - width015, sideGaugeBottom), false),
                    new LineSegment(new Point(width, sideGaugeBottom), false),
                    new LineSegment(new Point(width, height - (fontSize * 3)), false),
                    new LineSegment(new Point(0, height - (fontSize * 3)), false),
                    new LineSegment(new Point(0, sideGaugeBottom), false),
                    new LineSegment(new Point(width015, sideGaugeBottom), false),
                    new LineSegment(new Point(width015, sideGaugeTop), false),
                    new LineSegment(new Point(0, sideGaugeTop), false)
                },
                true)
            });

            mapGeometryGroup.Children.Clear();
            mapRadius = height * 0.05;
            double locationSideLength = 2 * mapRadius * Math.Cos(30);
            Point mapCenter = new Point(halfWidth, height - mapRadius - 5);
            mapBrush = falseHorizonBrush.Clone();
            mapBrush.Opacity = 0.25;
            mapPen = new Pen(mapBrush, baselinePen.Thickness);
            mapGeometryGroup.Children.Add(new EllipseGeometry(mapCenter, mapRadius, mapRadius));
            mapGeometryGroup.Children.Add(new EllipseGeometry(mapCenter, 1, 1));

            locationGeometryGroup.Children.Clear();
            Point locationB = new Point(mapCenter.X - locationSideLength, mapCenter.Y + locationSideLength);
            Point locationC = new Point(mapCenter.X + locationSideLength, mapCenter.Y + locationSideLength);
            locationGeometryGroup.Children.Add(new LineGeometry(mapCenter, locationB));
            locationGeometryGroup.Children.Add(new LineGeometry(mapCenter, locationC));
            locationYawTransform.CenterX = mapCenter.X;
            locationYawTransform.CenterY = mapCenter.Y;
            TransformGroup locationTransformGroup = new TransformGroup();
            locationTransformGroup.Children.Add(locationYawTransform);
            locationTransformGroup.Children.Add(locationTranslateTransform);
            InvalidateVisual();
        }

        protected void RenderDepthGauge(DrawingContext context)
        {
            int x = 100;
            double height = ActualHeight;
            double i = (0.1 * height), heightPoint = 0;
            Point GaugePoint1 = new Point(50, (0.1 * height));
            Point GaugePoint2 = new Point(50, (0.9 * height));

            if (gaugePen == null)
                return;

            context.DrawLine(baselinePen, GaugePoint1, GaugePoint2);
            DrawingVisual pointer = new DrawingVisual();
            using (pointer.RenderOpen())
            {
                Point start = new Point(60, ((height / 2000) * x)+(0.1*height));
                LineSegment[] segments = new LineSegment[] { new LineSegment(new Point(80, (((height / 2000) * x) - 10)+ (0.1 * height)), true), new LineSegment(new Point(80, (((height / 2000) * x) + 10) + (0.1 * height)), true) };
                PathFigure figure = new PathFigure(start, segments, true);
                PathGeometry geo = new PathGeometry(new PathFigure[] { figure });
                context.DrawGeometry(gaugeBrush, gaugePen, geo);
            }
            
            while (i <= (0.9 * height))
            {
                Point drawPoint1 = new Point(50, i);
                Point drawPoint2 = new Point(65, i);
                Point textPoint = new Point(30, i-10);
                

                depthValueFormattedText = new FormattedText(heightPoint.ToString(), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                     typeface, fontSize, brush)
                {
                    TextAlignment = TextAlignment.Center
                };

                context.DrawLine(baselinePen, drawPoint1, drawPoint2);
                context.DrawText(depthValueFormattedText, textPoint);
                
                i += (0.05 * height);
                heightPoint += 100;                
            }
        }

        protected void RenderBatteryBar(DrawingContext context)
        {
            int x = 16;
            double height = ActualHeight;
            double width = ActualWidth;
            Point WarningPoint = new Point((0.9*width) , (0.04*height));
            Rect BatteryBar = new Rect((0.85*width), (0.025*height), (width/(10*(1-1.5*(x-16)))), (height/100));
            if (Voltage < 11.00)
            {
                context.DrawRectangle(warningBrush, warningPen, BatteryBar);
                context.DrawText(warningFormattedText, WarningPoint);
            }
            else if (Voltage > 11.00 && Voltage < 12.00)
                context.DrawRectangle(warning2Brush, warning2Pen, BatteryBar);
            else
                context.DrawRectangle(brush, baselinePen, BatteryBar);
        }

        protected void RenderCompass(DrawingContext context)
        {
            if (baselinePen == null)
            {
                return;
            }
            compassClippingRectangle.Transform = invertedYawTransform;
            compass.Transform = yawTransform;
            compass.ClipGeometry = compassClippingRectangle;
            context.DrawDrawing(compass);
        }

        protected void RenderFalseHorizon(DrawingContext context)
        {
            if (falseHorizonPen == null)
            {
                return;
            }
            context.DrawDrawing(falseHorizonCrosshairs);
            context.DrawDrawing(pitchGauge);
        }

        protected void RenderLocation(DrawingContext context)
        {

            if (baselinePen == null)
            {
                return;
            }
            locationGeometryGroup.Transform = locationYawTransform;
            context.DrawGeometry(null, baselinePen, locationGeometryGroup);
        }

        protected void RenderCTDValues(DrawingContext context, string value, int position, FormattedText label)
        {
            if (falseHorizonPen != null)
            {
                var location = new Point(ActualWidth / 20 * position, 40);
                context.DrawText(label, location);

            }
            
        }

        protected void RenderGauge(DrawingContext context, string value, int position, FormattedText label)
        {
            if (falseHorizonPen != null)
            {
                var location = new Point(ActualWidth / 20 * position,
                                         label == null ? ActualHeight : ActualHeight - (label.Height * 1.5));
                context.DrawText(label, location);
                location.Y -= label == null ? 0 : label.Height;
                context.DrawText(
                    new FormattedText(value.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                      typeface, fontSize, brush)
                    {
                        TextAlignment = TextAlignment.Center
                    }, location);
            }
        }

    }
}