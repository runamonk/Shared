using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Utility
{
    public class Toggle : CheckBox
    {
        private Color offBackColor = SystemColors.ControlDark;
        private Color offToggleColor = Color.Red;

        private Color onBackColor = SystemColors.ControlDark;
        private Color onToggleColor = Color.Green;
        private bool solidStyle;
        private readonly Color ToggleColorDisabled = SystemColors.ControlDarkDark;

        public Toggle()
        {
            MinimumSize = new Size(45, 22);
        }

        public Color OnBackColor
        {
            get => onBackColor;

            set
            {
                onBackColor = value;
                Invalidate();
            }
        }

        public Color OnToggleColor
        {
            get => onToggleColor;

            set
            {
                onToggleColor = value;
                Invalidate();
            }
        }

        public Color OffBackColor
        {
            get => offBackColor;

            set
            {
                offBackColor = value;
                Invalidate();
            }
        }

        public Color OffToggleColor
        {
            get => offToggleColor;

            set
            {
                offToggleColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public override string Text
        {
            get => "";

            set => base.Text = "";
        }

        [DefaultValue(true)]
        public bool SolidStyle
        {
            get => solidStyle;

            set
            {
                solidStyle = value;
                Invalidate();
            }
        }

        private GraphicsPath GetFigurePath()
        {
            var arcSize = Height - 1;
            var leftArc = new Rectangle(0, 0, arcSize, arcSize);
            var rightArc = new Rectangle(Width - arcSize - 2, 0, arcSize, arcSize);

            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(leftArc, 90, 180);
            path.AddArc(rightArc, 270, 180);
            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var toggleSize = Height - 5;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent.BackColor);

            if (Checked)
            {
                if (solidStyle)
                    e.Graphics.FillPath(new SolidBrush(onBackColor), GetFigurePath());
                else
                    e.Graphics.DrawPath(new Pen(onBackColor, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(onToggleColor),
                    new Rectangle(Width - Height + 1, 2, toggleSize, toggleSize));
            }
            else
            {
                if (solidStyle)
                    e.Graphics.FillPath(new SolidBrush(Enabled ? offBackColor : ToggleColorDisabled), GetFigurePath());
                else
                    e.Graphics.DrawPath(new Pen(Enabled ? offBackColor : ToggleColorDisabled, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(Enabled ? offToggleColor : ToggleColorDisabled),
                    new Rectangle(2, 2, toggleSize, toggleSize));
            }
        }
    }
}