
namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Standard;
    using System.Diagnostics;

    public class DelayedResizeDecorator : Decorator
    {
        private Size _previousDesiredSize;
        private Size _previousRenderSize;

        private Size _previousMeasureSize;
        private Size _previousArrangeSize;
        private int _allowedLayoutCount;
        private bool AllowLayout { get { return _allowedLayoutCount > 0; } }
        
        private DispatcherTimer _dt;
        private EventHandler _rendering;

        public DelayedResizeDecorator()
        {
            _dt = new DispatcherTimer();
            _dt.Interval = new TimeSpan(0, 0, 0, 0, 250 /* ms */);
            _dt.Tick += new EventHandler(dt_Tick);
            _rendering = new EventHandler(CompositionTarget_Rendering);
        }

        void dt_Tick(object sender, EventArgs e)
        {
            _dt.Stop();

            Assert.IsFalse(this.AllowLayout);

            this._allowedLayoutCount = 1;
            this.InvalidateMeasure();
            this.InvalidateArrange();
            CompositionTarget.Rendering += _rendering;

            //Debug.WriteLine("Allow Rendering Turned On");
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            this._allowedLayoutCount--;

            if (!this.AllowLayout)
            {
                CompositionTarget.Rendering -= _rendering;

                //Debug.WriteLine("Allow Rendering Turned Off");
            }
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            UIElement child = this.Child;

            if (child != null)
            {
                //Debug.WriteLine(String.Format("MO: constraint {0} prev {1}", constraint, _previousMeasureSize));

                if (this.AllowLayout || 
                    (DoubleUtilities.AreClose(this._previousMeasureSize.Width, constraint.Width) &&
                     DoubleUtilities.AreClose(this._previousMeasureSize.Height, constraint.Height)))
                {
                    child.Measure(constraint);
                    _previousMeasureSize = constraint;

                    //Debug.WriteLine(String.Format("M: prevDes {0} Des {1}", _previousDesiredSize, child.DesiredSize));
                    _previousDesiredSize = child.DesiredSize;
                }
                else
                {
                    //Debug.WriteLine(String.Format("MX:{0} == {1}: {2}", constraint, _previousMeasureSize, (this._previousMeasureSize == constraint)));

                    _dt.Stop();
                    _dt.Start();
                }
            }

            return _previousDesiredSize;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size constraint)
        {
            UIElement child = this.Child;

            if (child != null)
            {
                //Debug.WriteLine(String.Format("AO: constraint {0} prev {1}", constraint, _previousArrangeSize));

                if (this.AllowLayout || 
                    (DoubleUtilities.AreClose(this._previousArrangeSize.Width, constraint.Width) &&
                     DoubleUtilities.AreClose(this._previousArrangeSize.Height, constraint.Height)) ||
                    (DoubleUtilities.AreClose(this._previousDesiredSize.Width, constraint.Width) &&
                     DoubleUtilities.AreClose(this._previousDesiredSize.Height, constraint.Height)))
                {
                    child.Arrange(new Rect(constraint));
                    _previousArrangeSize = constraint;
                    
                    //Debug.WriteLine(String.Format("A: prevArr {0} Arr {1}", _previousRenderSize, child.RenderSize));

                    _previousRenderSize = child.RenderSize;
                }
                else
                {
                    /*
                     Debug.WriteLine(String.Format("AX: CON {0} == PAR {1}: {2}, CON {3} == PDS {4}: {5}", 
                        constraint, _previousArrangeSize, (constraint == _previousArrangeSize),
                        constraint, _previousDesiredSize, (constraint == _previousDesiredSize)));
                     */

                    _dt.Stop();
                    _dt.Start();
                }
            }

            return _previousRenderSize;
        }
    }
}
