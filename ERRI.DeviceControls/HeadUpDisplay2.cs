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
using EERIL.DeviceControls.HeadUpControls;
using System.Collections;

namespace EERIL.DeviceControls
{
    public class HeadUpDisplay2 : Control
    {
        private ICollection<Gauge> gauges = new List<Gauge>();
        public ICollection<Gauge> Gauges{
            get
            {
                return gauges;
            }
            set
            {
                gauges = value;
            }
        }

        public ushort Scale
        {

            get;
            set;
        }

        protected void OnRenderGauges(DrawingContext context)
        {
            DrawingVisual gaugeVisual;
            DrawingContext gaugeContext;
            foreach (Gauge gauge in gauges)
            {
                gaugeVisual = new DrawingVisual();
                gaugeContext = gaugeVisual.RenderOpen();
            }

        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);
            this.OnRenderGauges(context);

        }

        protected override Size MeasureOverride(Size constraint)
        {
            double gaugeWidth = this.ActualWidth / (gauges.Count > 10 ? gauges.Count : 10) * Scale;
            Size gaugeConstraint = new Size(gaugeWidth, gaugeWidth);

            IEnumerator children = this.LogicalChildren;
            while(children.MoveNext())
            {
                object child = children.Current;
                (child as Control).Measure(child.GetType().IsSubclassOf(typeof(Gauge)) ? gaugeConstraint : constraint);
            }
 	        return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            int rightGaugeOffset = 0, leftGaugeOffset = 0, centerGaugeOffset = 0;

            IEnumerator children = this.LogicalChildren;
            Gauge gauge;
            while (children.MoveNext())
            {
                object child = children.Current;
                gauge = child as Gauge;
                if (gauge != null)
                {
                }
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        static HeadUpDisplay2()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeadUpDisplay2), new FrameworkPropertyMetadata(typeof(HeadUpDisplay2)));
        }
    }
}
