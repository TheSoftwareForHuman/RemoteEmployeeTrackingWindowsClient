using System;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;

namespace LoginApplication
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                e.Cancel = true;
                Hide();
            };

            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler((object sender, EventArgs e_args) =>
            {
                _current_stop_watch_day_seconds = (int)m_StopWatch.Elapsed.TotalSeconds;

                int elapsed = _current_stop_watch_day_seconds - _prev_stop_watch_day_seconds;

                current_time_seconds += elapsed;
                current_day_seconds += elapsed;
                current_month_seconds += elapsed;

                _prev_stop_watch_day_seconds = _current_stop_watch_day_seconds;

                updateTimeLabels();
            });

            timer.Interval = 1000;
            timer.Enabled = false;
        }

        public event LogOutEvent LogOut;
        public event TrackTimeEvent TrackTimeEvent;

        public int current_time_seconds { get { return _current_time_seconds; } set { _current_time_seconds = value; } }
        public int current_month_seconds { get { return _current_month_seconds; } set { _current_month_seconds = value; } }
        public int current_day_seconds { get { return _current_day_seconds; } set { _current_day_seconds = value; } }

        public void TrackTime(bool bTrack)
        {
            m_bIsTracking = bTrack;

            m_StopWatch.Reset();

            if (m_bIsTracking)
            {
                m_StopWatch.Start();
                this.btn_Time.Text = "Stop Time";
            }
            else
            {
                m_StopWatch.Stop();
                this.btn_Time.Text = "Start Time";
            }

            timer.Enabled = m_bIsTracking;

            current_time_seconds = 0;

            _current_stop_watch_day_seconds = 0;
            _prev_stop_watch_day_seconds = 0;

            updateTimeLabels();

            TrackTimeEventArgs arg = new TrackTimeEventArgs(m_bIsTracking);
            TrackTimeEvent.Invoke(this, arg);
        }

        public void updateTimeLabels()
        {
            TimeSpan t = TimeSpan.FromSeconds(current_time_seconds);
            string output = string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)t.TotalHours,
                t.Minutes,
                t.Seconds);
            label_current_time.Text = output;

            t = TimeSpan.FromSeconds(current_day_seconds);
            output = string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)t.TotalHours,
                t.Minutes,
                t.Seconds);
            label_day_time.Text = output;


            t = TimeSpan.FromSeconds(current_month_seconds);
            output = string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)t.TotalHours,
                t.Minutes,
                t.Seconds);
            label_month_time.Text = output;
        }

        private void btn_Time_Click(object sender, EventArgs e)
        {
            m_bIsTracking = !m_bIsTracking;

            TrackTime(m_bIsTracking);

            // 7. When people will stop time to track it will force them to visit website Nasa73.
            if (!m_bIsTracking)
            {
                System.Diagnostics.Process.Start(Program.GetServerURL().SERVERURL());
            }
        }

        private bool m_bIsTracking = false;

        private System.Windows.Forms.Timer timer;
        Stopwatch m_StopWatch = new Stopwatch();

        private int _current_time_seconds = 0;
        private int _current_month_seconds = 0;
        private int _current_day_seconds = 0;

        private int _current_stop_watch_day_seconds = 0;
        private int _prev_stop_watch_day_seconds = 0;

        private void button_logout_Click(object sender, EventArgs e)
        {
            TrackTime(false);

            LogOutEventArgs arg = new LogOutEventArgs();
            LogOut.Invoke(sender, arg);
        }
    }

    public class TrackTimeEventArgs : EventArgs
    {
        public bool isTracking { get { return m_bIsTracking; } }

        public TrackTimeEventArgs(bool bIsTracking)
        {
            m_bIsTracking = bIsTracking;
        }
        private bool m_bIsTracking;
    }
    public delegate void TrackTimeEvent(object sender, TrackTimeEventArgs e);

    public class LogOutEventArgs : EventArgs { }
    public delegate void LogOutEvent(object sender, LogOutEventArgs e);
}
