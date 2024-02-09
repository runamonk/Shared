using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace Utility
{
    public partial class Toggle : CheckBox
    {

        private Color onBackColor = SystemColors.ControlDark;
        private Color onToggleColor = Color.Green;
        private Color offBackColor = SystemColors.ControlDark;
        private Color offToggleColor = Color.Red;
        private bool solidStyle = false;

        public Color OnBackColor
        {
            get
            {
                return onBackColor;
            }

            set
            {
                onBackColor = value;
                this.Invalidate();
            }
        }

        public Color OnToggleColor
        {
            get
            {
                return onToggleColor;
            }

            set
            {
                onToggleColor = value;
                this.Invalidate();
            }
        }

        public Color OffBackColor
        {
            get
            {
                return offBackColor;
            }

            set
            {
                offBackColor = value;
                this.Invalidate();
            }
        }

        public Color OffToggleColor
        {
            get
            {
                return offToggleColor;
            }

            set
            {
                offToggleColor = value;
                this.Invalidate();
            }
        }

        [Browsable(false)]
        public override string Text
        {
            get
            {
                return "";
            }

            set
            {
                base.Text = "";
            }
        }

        [DefaultValue(true)]
        public bool SolidStyle
        {
            get
            {
                return solidStyle;
            }

            set
            {
                solidStyle = value;
                this.Invalidate();
            }
        }

        public Toggle()
        {
            this.MinimumSize = new Size(45, 22);
        }

        private GraphicsPath GetFigurePath()
        {
            int arcSize = this.Height - 1;
            Rectangle leftArc = new Rectangle(0, 0, arcSize, arcSize);
            Rectangle rightArc = new Rectangle(this.Width - arcSize - 2, 0, arcSize, arcSize);

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(leftArc, 90, 180);
            path.AddArc(rightArc, 270, 180);
            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int toggleSize = this.Height - 5;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(this.Parent.BackColor);

            if (this.Checked)
            {
                if (solidStyle)
                    e.Graphics.FillPath(new SolidBrush(onBackColor), GetFigurePath());
                else 
                    e.Graphics.DrawPath(new Pen(onBackColor, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(onToggleColor), new Rectangle(this.Width - this.Height + 1, 2, toggleSize, toggleSize));
            }
            else 
            {
                if (solidStyle)
                    e.Graphics.FillPath(new SolidBrush(offBackColor), GetFigurePath());
                else 
                    e.Graphics.DrawPath(new Pen(offBackColor, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(offToggleColor),  new Rectangle(2, 2, toggleSize, toggleSize));
            }
        }
    }
}