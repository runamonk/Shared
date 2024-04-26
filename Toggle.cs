using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Utility
{
    public class Toggle : CheckBox
    {
        private readonly Color _toggleColorDisabled = SystemColors.ControlDarkDark;
        private Color _offBackColor = SystemColors.ControlDark;
        private Color _offToggleColor = Color.Red;

        private Color _onBackColor = SystemColors.ControlDark;
        private Color _onToggleColor = Color.Green;
        private bool _solidStyle;

        public Toggle() { MinimumSize = new Size(45, 22); }

        public Color OnBackColor
        {
            get => _onBackColor;
            set
            {
                _onBackColor = value;
                Invalidate();
            }
        }

        public Color OnToggleColor
        {
            get => _onToggleColor;
            set
            {
                _onToggleColor = value;
                Invalidate();
            }
        }

        public Color OffBackColor
        {
            get => _offBackColor;
            set
            {
                _offBackColor = value;
                Invalidate();
            }
        }

        public Color OffToggleColor
        {
            get => _offToggleColor;
            set
            {
                _offToggleColor = value;
                Invalidate();
            }
        }

        [Browsable(false)] public override string Text { get => ""; set => base.Text = ""; }

        [DefaultValue(true)]
        public bool SolidStyle
        {
            get => _solidStyle;
            set
            {
                _solidStyle = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int toggleSize = Height - 5;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent.BackColor);

            if (Checked)
            {
                if (_solidStyle)
                    e.Graphics.FillPath(new SolidBrush(_onBackColor), GetFigurePath());
                else
                    e.Graphics.DrawPath(new Pen(_onBackColor, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(_onToggleColor), new Rectangle(Width - Height + 1, 2, toggleSize, toggleSize));
            }
            else
            {
                if (_solidStyle)
                    e.Graphics.FillPath(new SolidBrush(Enabled ? _offBackColor : _toggleColorDisabled), GetFigurePath());
                else
                    e.Graphics.DrawPath(new Pen(Enabled ? _offBackColor : _toggleColorDisabled, 2), GetFigurePath());

                e.Graphics.FillEllipse(new SolidBrush(Enabled ? _offToggleColor : _toggleColorDisabled), new Rectangle(2, 2, toggleSize, toggleSize));
            }
        }

        private GraphicsPath GetFigurePath()
        {
            int arcSize = Height - 1;
            Rectangle leftArc = new Rectangle(0,                    0, arcSize, arcSize);
            Rectangle rightArc = new Rectangle(Width - arcSize - 2, 0, arcSize, arcSize);

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(leftArc,  90,  180);
            path.AddArc(rightArc, 270, 180);
            path.CloseFigure();

            return path;
        }
    }
}