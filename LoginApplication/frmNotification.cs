using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LoginApplication
{
    public partial class frmNotification : Form
    {
        private FlowLayoutPanel flowPanel;

        public frmNotification(string json_data)
        {
            InitializeComponent();

            this.MaximizeBox = false;
            this.MinimizeBox = true;

            FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                close_timer.Enabled = false;
            };

            Paint += (object sender, PaintEventArgs e) =>
            {
                Rectangle r = new Rectangle(this.DisplayRectangle.X, this.DisplayRectangle.Y, this.DisplayRectangle.Width - 1, this.DisplayRectangle.Height - 1);
                e.Graphics.DrawRectangle(new Pen(Color.Black, 1), r);
            };

            m_CloseCount = 5;

            close_timer = new System.Windows.Forms.Timer();
            close_timer.Tick += new EventHandler((object sender, EventArgs e_args) =>
            {
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

            // parse json

            JavaScriptSerializer js = new JavaScriptSerializer();

            notification = js.Deserialize<JSON_NOTIFICATION>(json_data);

            if (notification.text.Length == 0 &&
                notification.pic.Length == 0 &&
                notification.link.Length == 0
            )
            {
                m_bShow = false;
            }
            else
            {
                this.AutoSize = true;
                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                this.SuspendLayout();

                flowPanel = new FlowLayoutPanel();
                flowPanel.AutoSize = true;
                flowPanel.AutoSizeMode = AutoSizeMode.GrowOnly;
                flowPanel.FlowDirection = FlowDirection.TopDown;
                flowPanel.Location = new System.Drawing.Point(5, 5);

                // flowPanel.BackColor = Color.Red;

                this.Controls.Add(flowPanel);

                if (notification.text.Length > 0)
                {
                    var text_label = new Label
                    {
                        AutoSize = true,
                        Name = "text_label",
                        Text = notification.text
                    };

                    text_label.Font = new System.Drawing.Font(
                        "Microsoft Sans Serif",
                        11F, System.Drawing.FontStyle.Regular,
                        System.Drawing.GraphicsUnit.Point, ((byte)(204)));

                    flowPanel.Controls.Add(text_label);
                }

                if (notification.pic.Length > 0)
                {
                    try
                    {
                        var base64Data = Regex.Match(notification.pic, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                        var binData = Convert.FromBase64String(base64Data);

                        using (var stream = new MemoryStream(binData))
                        {
                            var pictureBox = new System.Windows.Forms.PictureBox();
                            pictureBox.Name = "pictureBox";
                            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                            pictureBox.Size = new System.Drawing.Size(200, 200);
                            pictureBox.TabIndex = 5;
                            pictureBox.TabStop = false;
                            pictureBox.Image = new Bitmap(stream);

                            flowPanel.Controls.Add(pictureBox);
                        }

                        //Image image;
                        //using (MemoryStream ms = new MemoryStream(binData))
                        //{
                        //    image = Image.FromStream(ms);
                        //}
                        //image.Save("img.png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch (Exception ex)
                    {
                        int debug = 0;
                    }
                }

                if (notification.link.Length > 0)
                {
                    var link_label = new Label
                    {
                        AutoSize = true,
                        Name = "link_label",
                        Text = notification.link
                    };

                    link_label.Font = new System.Drawing.Font(
                        "Microsoft Sans Serif",
                        11F,
                        System.Drawing.FontStyle.Regular,
                        System.Drawing.GraphicsUnit.Point, ((byte)(204)));

                    link_label.ForeColor = System.Drawing.Color.Blue;

                    link_label.Font = new Font(link_label.Font, FontStyle.Underline);

                    link_label.Cursor = Cursors.Hand;

                    link_label.Click += (object sender, EventArgs e) =>
                    {
                        System.Diagnostics.Process.Start(link_label.Text);
                    };

                    flowPanel.Controls.Add(link_label);

                    if (notification.force_open.Length > 0 && notification.force_open.ToLower() == "true")
                    {
                        System.Diagnostics.Process.Start(link_label.Text);
                    }
                }

                this.ResumeLayout(false);
                this.PerformLayout();

                m_bShow = true;
            }
        }

        public void ShowModal()
        {
            if (m_bShow)
            {
                Program.SetWindowPos(this.Handle, Program.HWND_TOPMOST, 0, 0, 0, 0, Program.TOPMOST_FLAGS);
                this.Invalidate();
                this.Update();
                this.Refresh();
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

        private bool m_bShow = false;
        private uint m_CloseCount = 0;
        System.Windows.Forms.Timer close_timer;

        JSON_NOTIFICATION notification;

        public class JSON_NOTIFICATION
        {
            public string text { get; set; }
            public string pic { get; set; }
            public string link { get; set; }
            public string force_open { get; set; }
        }
    }
}
