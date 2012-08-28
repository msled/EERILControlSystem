using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace EERIL.DeviceControls
{
    class AccelerationBasedLocation : DependencyObject
    {
        private long initialTimestamp = -1;
        private long previousTimestamp;
        private readonly ConcurrentQueue<AccelerationSample> samples;
        private Point3D distance;
        private float furthestDistance;
        public event EventHandler ValuesUpdated;
        public static readonly DependencyProperty AverageXProperty = DependencyProperty.Register("AverageX", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty AverageYProperty = DependencyProperty.Register("AverageY", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty AverageZProperty = DependencyProperty.Register("AverageZ", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty CurrentXProperty = DependencyProperty.Register("CurrentX", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty CurrentYProperty = DependencyProperty.Register("CurrentY", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty CurrentZProperty = DependencyProperty.Register("CurrentZ", typeof(float), typeof(AccelerationBasedLocation), new PropertyMetadata(default(float)));
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(long), typeof(AccelerationBasedLocation), new PropertyMetadata(default(long)));
        private readonly Timer processingTimer = new Timer(100);

        public float AverageX
        {
            get
            {
                return (float)GetValue(AverageXProperty);
            }
            private set
            {
                SetValue(AverageXProperty, value);
            }
        }
        public float AverageY
        {
            get
            {
                return (float)GetValue(AverageYProperty);
            }
            private set
            {
                SetValue(AverageYProperty, value);
            }
        }
        public float AverageZ
        {
            get
            {
                return (float)GetValue(AverageZProperty);
            }
            private set
            {
                SetValue(AverageZProperty, value);
            }
        }

        public float CurrentX
        {
            get
            {
                return (float)GetValue(CurrentXProperty);
            }
            private set
            {
                SetValue(CurrentXProperty, value);
            }
        }
        public float CurrentY
        {
            get
            {
                return (float)GetValue(CurrentYProperty);
            }
            private set
            {
                SetValue(CurrentYProperty, value);
            }
        }
        public float CurrentZ
        {
            get
            {
                return (float)GetValue(CurrentZProperty);
            }
            private set
            {
                SetValue(CurrentZProperty, value);
            }
        }
        public Point3D Distance
        {
            get
            {
                return distance;
            }
        }
        public float FurthestDistance
        {
            get
            {
                return furthestDistance;
            }
        }
        public long Time
        {
            get
            {
                return (long)GetValue(TimeProperty);
            }
            private set
            {
                SetValue(TimeProperty, value);
            }
        }

        public IProducerConsumerCollection<AccelerationSample> Samples
        {
            get
            {
                return samples;
            }
        }

        public AccelerationBasedLocation()
        {
            samples = new ConcurrentQueue<AccelerationSample>();
            processingTimer.Elapsed += ProcessingTimerOnElapsed;
        }

        private void ProcessingTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            AccelerationSample sample;
            long elapsedTime;
            int count = samples.Count;
            float distanceXInverse;
            float distanceYInverse;
            float distanceZInverse;
            while (count-- > 0 && samples.TryDequeue(out sample))
            {
                if (initialTimestamp == -1)
                {
                    initialTimestamp = sample.Timestamp;
                    previousTimestamp = initialTimestamp;
                }
                elapsedTime = sample.Timestamp - previousTimestamp;
                CurrentX = CurrentX + sample.X * elapsedTime;
                CurrentY = CurrentY + sample.Y * elapsedTime;
                CurrentZ = CurrentZ + sample.Z * elapsedTime;
                AverageX = (AverageX + CurrentX * -1) / 2;
                AverageY = (CurrentY + CurrentY * -1) / 2;
                AverageZ = (AverageZ + CurrentZ * -1) / 2;
                distance.X = Time * AverageX;
                distance.Y = Time * AverageY;
                distance.Z = Time * AverageZ;
                distanceXInverse = distance.X;
                distanceYInverse = distance.Y;
                distanceZInverse = distance.Z;
                if (distance.X > furthestDistance || distanceXInverse > furthestDistance)
                {
                    furthestDistance = distanceXInverse > 0 ? distanceXInverse : distance.X;
                }
                if (distance.Y > furthestDistance || distanceYInverse > furthestDistance)
                {
                    furthestDistance = distanceYInverse > 0 ? distanceYInverse : distance.Y;
                }
                if (distance.Z > furthestDistance || distanceZInverse > furthestDistance)
                {
                    furthestDistance = distanceZInverse > 0 ? distanceZInverse : distance.Z;
                }
                previousTimestamp = sample.Timestamp;
            }
            Time = previousTimestamp - initialTimestamp;
            OnValuesUpdated();
        }

        protected void OnValuesUpdated()
        {
            if (ValuesUpdated != null)
            {
                EventHandler eventHandler = ValuesUpdated;
                Delegate[] delegates = eventHandler.GetInvocationList();
                EventArgs eventArgs = new EventArgs();
                foreach (EventHandler handler in delegates)
                {
                    var dispatcherObject = handler.Target as DispatcherObject;
                    if (dispatcherObject != null && !dispatcherObject.CheckAccess())
                    {
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, eventArgs);
                    }
                    else
                    {
                        handler(this, eventArgs);
                    }
                }
            }
        }
    }
}
