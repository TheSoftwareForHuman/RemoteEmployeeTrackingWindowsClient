using System;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace LoginApplication
{
    public partial class frmTakePicture : Form
    {
        public frmTakePicture(Bitmap bmp)
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.MinimizeBox = true;

            this.pictureBox1.Image = bmp;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                close_timer.Enabled = false;
            };

            Paint += (object sender, PaintEventArgs e) =>
            {
                Rectangle r = new Rectangle(this.DisplayRectangle.X, this.DisplayRectangle.Y, this.DisplayRectangle.Width - 1, this.DisplayRectangle.Height - 1);

                e.Graphics.DrawRectangle(new Pen(Color.Black, 1), r);
            };

            m_CloseCount = 10;

#if DEBUG
            m_CloseCount = 2;
#endif

            close_timer = new System.Windows.Forms.Timer();
            close_timer.Tick += new EventHandler((object sender, EventArgs e_args) =>
            {
                this.label_Close.Text = "Delete picture ? " + m_CloseCount.ToString() + " sec ...";

                if (m_CloseCount == 0)
                {
                    close_timer.Enabled = false;
                    this.Close();
                }

                m_CloseCount--;
            });

            close_timer.Interval = 1000; // 1 secs
            close_timer.Enabled = true;

            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.TopLevel = true;
        }

        public void ShowModal()
        {
            if (m_bShow)
            {
                Program.SetWindowPos(this.Handle, Program.HWND_TOPMOST, 0, 0, 0, 0, Program.TOPMOST_FLAGS);
                ShowDialog();
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                baseParams.ExStyle |= (int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

                return baseParams;
            }
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            close_timer.Enabled = false;
            m_bDelete = true;
            this.Close();
        }

        public bool IsDelete()
        {
            return m_bDelete;
        }

        private bool m_bShow = true;            
        private bool m_bDelete = false;
        private uint m_CloseCount = 0;
        System.Windows.Forms.Timer close_timer;
    }
}
