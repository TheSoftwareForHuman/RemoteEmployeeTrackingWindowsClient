using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;

using LoginApplication.Properties;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.ComponentModel;
using System.Data.SQLite;
using Microsoft.Win32;
using System.Xml;
using System.Collections;

using WebSocketSharp;

using System.Runtime.InteropServices;

namespace LoginApplication
{
    class SERVER_URL
    {
        public string REGISTER()
        {
            return server_url + "/api/register";
        }
        public string LOGIN()
        {
            return server_url + "/api/login";
        }
        public string LOGOUT()
        {
            return server_url + "/api/logout";
        }
        public string UPLOAD()
        {
            return server_url + "/api/upload";
        }
        public string WS_URL()
        {
            return ws_server_url;
        }
        public string SERVERURL()
        {
            return server_url;
        }

        public void Init(string in_server_url)
        {
            server_url = in_server_url;
        }

        public void InitWs(string in_server_url)
        {
            ws_server_url = in_server_url;
        }

        private string server_url;
        private string ws_server_url;
    }

    static class Program
    {
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        public const string db_name = "db.sqlite";

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [STAThread]
        static void Main(string[] args)
        {
            string regKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            string appKeyName = @"EmployeeTimeTrackingSoftware";

            if (args.Length > 0 && args[0] == "--remove")
            {
                RegistryKey rk_delete = Registry.CurrentUser.OpenSubKey(regKeyName, true);
                rk_delete.DeleteValue(appKeyName);
                rk_delete.Close();
            }
            else
            {
                var bRunning = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;

                if (!bRunning)
                {
                    string app_path = System.Reflection.Assembly.GetEntryAssembly().Location;

                    RegistryKey rk_add = Registry.CurrentUser.OpenSubKey(regKeyName, true);
                    rk_add.SetValue(appKeyName, "\"" + app_path + "\"");
                    rk_add.Close();

                    var fi = new FileInfo(Application.ExecutablePath);
                    Directory.SetCurrentDirectory(fi.DirectoryName);

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    ApplicationTrayContext ctx = new ApplicationTrayContext();

                    Application.Run(ctx);
                }
                else
                    MessageBox.Show("Application already started !", "Timetracker");
            }
        }

        public static void InitServerURL(string url)
        {
            m_SERVER_URL.Init(url);
        }
        public static void InitWSServerURL(string url)
        {
            m_SERVER_URL.InitWs(url);
        }
        public static SERVER_URL GetServerURL()
        {
            return m_SERVER_URL;
        }

        private static SERVER_URL m_SERVER_URL = new SERVER_URL();
    }

    public class ApplicationTrayContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        private frmLogin fm_Login;
        private frmMain fm_Main;
        private Form fm_Active;

        private string m_Token = string.Empty;

        private readonly Mutex m_FileSystemReadWriteMutex = new Mutex();

        MouseHookListener mouseHookManager;
        KeyboardHookListener keyboardHookManager;
        uint mouseClickCount = 0, keyDownCount = 0;

        bool m_bLoggining = false;
        bool m_bLogouting = false;

        System.Windows.Forms.Timer m_MainTimer;
        const int m_MainTimerInterval = 1000;
        bool m_bTackingScreenshoot = false;
        bool m_bZipAndUploading = false;
        bool m_bFailedMessageShown = false;
        bool m_bTackingHooks = false;

        System.Windows.Forms.Timer m_InternetConnectionChecker;
        bool m_bCheckingConnection = false;
        bool m_bLoginAndUploadForce = false;

        SQLiteConnection m_dbConnection;

        WebSocket m_wsConnection = null;
        System.Windows.Forms.Timer m_wsConnectionTimer;

        const int m_TrackHooksTimer_interval_initial = 1 * 60 * 1000; // 1 - minutes
        const int m_ScreenshootTimer_interval_initial = 3 * 60 * 1000; // 3 - minutes;
        const int m_ZipTimer_interval_initial = 9 * 60 * 1000; // 9 - minutes
        int m_ZipTimer_interval_additional_secs_initial = 0;

        const int m_InternetConnectionChecker_interval_initial = 15 * 1000; // 15 sec

        int m_ScreenshootTimer_interval = m_ScreenshootTimer_interval_initial;
        int m_TrackHooksTimer_interval = m_TrackHooksTimer_interval_initial;
        int m_ZipTimer_interval = m_ZipTimer_interval_initial; 

        int m_InternetConnectionChecker_interval = m_InternetConnectionChecker_interval_initial;

        public ApplicationTrayContext()
        {
            Hashtable settings = getSettings();

            Program.InitServerURL(settings["hostname"].ToString());
            Program.InitWSServerURL(settings["ws"].ToString());

            // create folders
            {
                string subPath = "data";
                bool bExists = System.IO.Directory.Exists(subPath);
                if (!bExists)
                    System.IO.Directory.CreateDirectory(subPath);
                subPath = "zip";
                bExists = System.IO.Directory.Exists(subPath);
                if (!bExists)
                    System.IO.Directory.CreateDirectory(subPath);
            }

            // create db
            {
#if DEBUG_table_create
                if (File.Exists("db.sqlite"))
                    File.Delete("db.sqlite");
#endif
                m_dbConnection = new SQLiteConnection("Data Source=" + Program.db_name + ";Version=3;");

                m_dbConnection.Open();

                string sql_users = "create table if not exists users" +
                "(" +
                " login TEXT NOT NULL," +
                " token TEXT NOT NULL," +
                " PRIMARY KEY (login, token)" +
                ");";

                // 4. In database keyboard and mouse click there is no start time and end time
                string sql_users_mouse_clicks = "create table if not exists users_click" +
                "(" +
                " id integer primary key autoincrement," +
                " login TEXT NOT NULL," +
                " mouse_clicks INTEGER," +
                " keyboard_clicks INTEGER," +
                " start_time TIMESTAMP default (CURRENT_TIMESTAMP)," + 
                " end_time TIMESTAMP default (CURRENT_TIMESTAMP)," + 
                " screenshoot TEXT," +
                " FOREIGN KEY (login) REFERENCES users(login)" +
                ");";

                string sql_work_month_tracking = "create table if not exists work_month_tracking" +
                "(" +
                " login TEXT NOT NULL," +
                " month TEXT NOT NULL," +
                " time INTEGER," + // full time in sec
                " PRIMARY KEY (login, month)" +
                " FOREIGN KEY (login) REFERENCES users(login)" +
                ");";

                string sql_work_current_day_tracking = "create table if not exists work_current_day_tracking" +
                "(" +
                " login TEXT NOT NULL," +
                " current_day TEXT NOT NULL," +
                " time INTEGER," + // full time in sec
                " PRIMARY KEY (login, current_day)" +
                " FOREIGN KEY (login) REFERENCES users(login)" +
                ");";

                string sql = sql_users + sql_users_mouse_clicks + sql_work_month_tracking + sql_work_current_day_tracking;

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();

                m_dbConnection.Close();
                GC.Collect();
            }

            // Forms
            // Tray icon
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
#if DEBUG_exit
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Exit", (object sender, EventArgs e)=>
                    {
                        trayIcon.Visible = false;
                        Application.Exit();
                    })
                    }),
#endif
                Visible = true
            };
            trayIcon.Text = "Time Tracker";
            trayIcon.Click += (object sender, EventArgs e) =>
            {
                fm_Active.Show();
            };

            // Main Time Tracking 
            fm_Main = new frmMain();
            fm_Main.Hide();
            fm_Main.LogOut += (object sender, LogOutEventArgs e) =>
            {
                if (m_wsConnection != null && m_wsConnection.IsAlive)
                {
                    m_wsConnection.Close();
                    m_wsConnection = null;
                    m_wsConnectionTimer.Enabled = false;
                }

                m_bLoginAndUploadForce = false;

                m_TrackHooksTimer_interval = m_TrackHooksTimer_interval_initial;
                m_ScreenshootTimer_interval = m_ScreenshootTimer_interval_initial;
                m_ZipTimer_interval = m_ZipTimer_interval_initial;
                m_ZipTimer_interval_additional_secs_initial = 0;

                m_MainTimer.Enabled = false;

                LogOut();
            };
            fm_Main.TrackTimeEvent += (object sender, TrackTimeEventArgs args) =>
            {
                m_MainTimer.Enabled = args.isTracking;

                TrackHooks(true, args.isTracking);
            };

            // Login form
            fm_Login = new frmLogin();
#if DEBUG
            fm_Login.login = "konstantin.eletskiy@gmail.com"; // same on server under debug
            fm_Login.password = "secret";
#endif
            fm_Login.Show();

            // 1. Login should be done by online.
            fm_Login.LogIn += (object sender, LogInEventArgs e) =>
            {
                LogIn(e.login, e.password);
            };

            fm_Active = fm_Login;

            // 3. We will track the number of time keyboard click
            keyboardHookManager = new KeyboardHookListener(new GlobalHooker());
            keyboardHookManager.Enabled = true;
            keyboardHookManager.KeyDown += (object sender, KeyEventArgs e) => 
            {
                if (m_MainTimer != null && m_MainTimer.Enabled)
                    keyDownCount++;
            };

            mouseHookManager = new MouseHookListener(new GlobalHooker());
            mouseHookManager.Enabled = true;
            mouseHookManager.MouseClick += (object sender, MouseEventArgs e) => 
            {
                if (m_MainTimer != null && m_MainTimer.Enabled)
                    mouseClickCount++; 
            };

            m_MainTimer = new System.Windows.Forms.Timer();
            m_MainTimer.Tick += new EventHandler((object sender, EventArgs e_args) =>
            {
                if (m_Token == null || m_Token.Length == 0)
                    return;

                // 3. Save number of time keyboard and mouse clicks it to sqlite3 database every 1 minute.
                m_TrackHooksTimer_interval -= m_MainTimerInterval;

                if (m_TrackHooksTimer_interval <= 0)
                {
                    m_TrackHooksTimer_interval = m_TrackHooksTimer_interval_initial;

                    TrackHooks(true, true);
                }
                else
                {
                    // Every 3 second update the database. In this way we also do not need to worry about button clicking or anything

                    int elapsed_secs = m_TrackHooksTimer_interval / 1000; // seconds

                    int mod = elapsed_secs % 3;

                    if (mod == 0)
                    {
                        TrackHooks(false, false);
                    }
                }

                // 2. Every 3 minutes we will take one screenshot and save it in the file system.
                m_ScreenshootTimer_interval -= m_MainTimerInterval;

                if (m_ScreenshootTimer_interval <= 0)
                {
                    m_ScreenshootTimer_interval = m_ScreenshootTimer_interval_initial;

                    TakeScreenShootsTimerHandler();
                }

                // 4. Every 9 minutes we will zip all screenshot data and database and upload to online. 
                m_ZipTimer_interval -= m_MainTimerInterval;

                if (m_ZipTimer_interval <= 0 && !m_bTackingScreenshoot)
                {
                    m_ZipTimer_interval = m_ZipTimer_interval_initial - m_ZipTimer_interval_additional_secs_initial;

                    m_ZipTimer_interval_additional_secs_initial = 0;

                    ZipAndUploadFunct();
                }

                if (m_ZipTimer_interval <= 0 && m_bTackingScreenshoot)
                {
                    m_ZipTimer_interval_additional_secs_initial += m_MainTimerInterval;
                    // Upload data to mysql only user decision about saving screenshot
                }
            });
            m_MainTimer.Interval = m_MainTimerInterval;
            m_MainTimer.Enabled = false;

            // Internals timers 
            m_wsConnectionTimer = new System.Windows.Forms.Timer();
            m_wsConnectionTimer.Tick += new EventHandler((object sender, EventArgs e_args) =>
            {
                if (m_wsConnection != null && m_wsConnection.IsAlive)
                {
                    m_wsConnection.Send("ping");
                }
            });
            m_wsConnectionTimer.Interval = 5000; // 5 sec
            m_wsConnectionTimer.Enabled = false;

            // 4. Once the computer will go to online we will upload all the data and clear it from the local database.
            m_InternetConnectionChecker = new System.Windows.Forms.Timer(); 
            m_InternetConnectionChecker.Tick += new EventHandler(CheckForInternetConnectionTimerHandler);
            m_InternetConnectionChecker.Interval = m_InternetConnectionChecker_interval;
            m_InternetConnectionChecker.Enabled = true;
        }

        void LogOut()
        {
            if (m_bLogouting)
                return;

            m_bLogouting = true;

            fm_Login.login_enabled = false;

            var bw = new BackgroundWorker();

            string url = Program.GetServerURL().LOGOUT();

            url = url + "?token=" + m_Token;

            m_Token = string.Empty;

            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                bw_e.Result = string.Empty;

                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);

                    request.Method = "GET";
                    request.Timeout = 15 * 1000;

                    request.CookieContainer = new CookieContainer();

                    var responseString = string.Empty;
                    var webResponse = (HttpWebResponse)request.GetResponse();
                    var responseStream = webResponse.GetResponseStream();

                    using (var reader = new StreamReader(responseStream))
                    {
                        responseString = reader.ReadToEnd();
                    }

                    bw_e.Result = responseString;
                }
                catch (WebException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_args) =>
            {
                if (m_wsConnection != null && m_wsConnection.IsAlive)
                {
                    m_wsConnection.Close();
                    m_wsConnection = null;
                    m_wsConnectionTimer.Enabled = false;
                }

                fm_Main.Hide();
                fm_Login.Show();

                fm_Active = fm_Login;

                fm_Login.login_enabled = true;

                m_bLogouting = false;
            };

            bw.RunWorkerAsync();
        }

        private static Hashtable getSettings()
        {
            Hashtable _ret = new Hashtable();

            string path = "config.xml";

            if (!File.Exists(path))
            {
                XmlDocument doc = new XmlDocument();

                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = doc.DocumentElement;
                doc.InsertBefore(xmlDeclaration, root);

                XmlElement element1 = doc.CreateElement(string.Empty, "Settings", string.Empty);
                doc.AppendChild(element1);

                XmlElement element2 = doc.CreateElement(string.Empty, "add", string.Empty);
                element2.SetAttribute("key", "hostname");
                element2.SetAttribute("value", "http://server.local");
                element1.AppendChild(element2);

                XmlElement element3 = doc.CreateElement(string.Empty, "add", string.Empty);
                element3.SetAttribute("key", "ws");
                element3.SetAttribute("value", "ws://server.local/");
                element1.AppendChild(element3);

                doc.Save(path);
            }

            {
                StreamReader reader = new StreamReader
                (
                    new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read)
                );

                XmlDocument doc = new XmlDocument();
                string xmlIn = reader.ReadToEnd();
                reader.Close();
                doc.LoadXml(xmlIn);
                foreach (XmlNode child in doc.ChildNodes)
                    if (child.Name.Equals("Settings"))
                        foreach (XmlNode node in child.ChildNodes)
                            if (node.Name.Equals("add"))
                                _ret.Add
                                (
                                    node.Attributes["key"].Value,
                                    node.Attributes["value"].Value
                                );
            }

            return (_ret);
        }

        private void TakeScreenShootsTimerHandler()
        {
            if (m_bTackingScreenshoot)
                return;

            m_bTackingScreenshoot = true;

            int screenshoot_timer_interval_sec = m_ScreenshootTimer_interval / 1000;

            string current_time_str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string screenshoot_time_before_current_time_str = DateTime.Now.AddSeconds(-screenshoot_timer_interval_sec).ToString("yyyy-MM-dd HH:mm:ss");

            string current_time_filename_str = DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss");

            var bw = new BackgroundWorker();

            // define the event handlers
            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                bw_e.Result = null;

                int screenLeft = SystemInformation.VirtualScreen.Left;
                int screenTop = SystemInformation.VirtualScreen.Top;
                int screenWidth = SystemInformation.VirtualScreen.Width;
                int screenHeight = SystemInformation.VirtualScreen.Height;

                bw_e.Result = new Bitmap(screenWidth, screenHeight);

                using (Graphics g = Graphics.FromImage((Bitmap)bw_e.Result))
                {
                    g.CopyFromScreen(screenLeft, screenTop, 0, 0, ((Bitmap)bw_e.Result).Size);
                }

                ((Bitmap)bw_e.Result).Tag = current_time_filename_str + "-screenshoot.jpg";

                frmTakePicture frm_TakePicture = new frmTakePicture((Bitmap)bw_e.Result);

                frm_TakePicture.StartPosition = FormStartPosition.Manual;
                Rectangle workingArea = Screen.GetWorkingArea(frm_TakePicture);
                frm_TakePicture.Location = new Point(workingArea.Right - frm_TakePicture.Width - 1, workingArea.Bottom - frm_TakePicture.Height - 1);

                frm_TakePicture.ShowModal(); // modal !

                if (frm_TakePicture.IsDelete())
                {
                    bw_e.Result = null;
                }
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_args) =>
            {
                if (m_Token != null && m_Token.Length > 0)
                {
                    if (bw_args.Result != null)
                    {
                        Bitmap bmp = (Bitmap)bw_args.Result;

                        string sreenshoot_name = (string)bmp.Tag;

                        m_FileSystemReadWriteMutex.WaitOne();

                        bmp.Save("data\\" + sreenshoot_name, ImageFormat.Jpeg);

                        m_dbConnection.Open();

                        try
                        {
                            string sql = "update users_click set screenshoot = '" + sreenshoot_name + "' where start_time < '" + current_time_str + "' and end_time >= '" + current_time_str + "';";

                            //string sql = "update users_click "+
                            //    " set screenshoot = '" + sreenshoot_name + "' " + 
                            //    " where strftime('%s', '" + current_time_str + "') " +
                            //    " between strftime('%s',start_time) and strftime('%s',end_time);";

                            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        m_dbConnection.Close();
                        GC.Collect();

                        m_FileSystemReadWriteMutex.ReleaseMutex();
                    }
                    else
                    {
                        // 5. Deleting screenshot is not deleting time.Like if I delete a screenshot it will delete time since last screenshot. In database, there should be field of screenshot name

                        int zero = 0;
                        string zero_str = zero.ToString();

                        string login = fm_Login.login;

                        int time_current = fm_Main.current_time_seconds - screenshoot_timer_interval_sec;

                        fm_Main.current_time_seconds = time_current > 0 ? time_current : 0;

                        int time_current_day = fm_Main.current_day_seconds - screenshoot_timer_interval_sec;

                        fm_Main.current_day_seconds = time_current_day > 0 ? time_current_day : 0;

                        int time_current_month = fm_Main.current_month_seconds - screenshoot_timer_interval_sec;

                        fm_Main.current_month_seconds = time_current_month > 0 ? time_current_month : 0;

                        fm_Main.updateTimeLabels();

                        m_FileSystemReadWriteMutex.WaitOne();

                        m_dbConnection.Open();

                        try
                        {
                            string sql = "delete from users_click where end_time >= '" + screenshoot_time_before_current_time_str + "';"; // and start_time < '" + current_time_str + "';";

                            sql += "insert into users_click (login, mouse_clicks, keyboard_clicks, start_time, end_time) values (" +
                            "'" + login + "'," +
                            zero_str + "," +
                            zero_str + "," +
                            "'" + current_time_str + "'," +
                            "'" + current_time_str + "'" +
                            ");";

                            sql += "insert or replace into work_month_tracking (login, month, time) values ('" + login + "', strftime('%Y-%m','now'), " + fm_Main.current_month_seconds + ");";
                            sql += "insert or replace into work_current_day_tracking (login, current_day, time) values ('" + login + "', strftime('%Y-%m-%d','now'), " + fm_Main.current_day_seconds + ");";

                            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        m_dbConnection.Close();
                        GC.Collect();

                        m_FileSystemReadWriteMutex.ReleaseMutex();
                    }
                }

                m_bTackingScreenshoot = false;
            };

            bw.RunWorkerAsync();
        }

        private void TrackHooks(bool bFullUpdate = true, bool bAddNewRecord = true)
        {
            if (m_Token == null || m_Token.Length == 0)
                return;

            if (m_bTackingHooks)
                return;

            m_bTackingHooks = true;

            int zero = 0;
            string zero_str = zero.ToString();

            string m_clicks = mouseClickCount.ToString();
            string k_clikcs = keyDownCount.ToString();

            string login = fm_Login.login;
            string token = m_Token;

            string time_current_day = fm_Main.current_day_seconds.ToString();
            string time_current_month = fm_Main.current_month_seconds.ToString();

            string current_time_str = DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss");

            m_FileSystemReadWriteMutex.WaitOne();

            m_dbConnection.Open();

            try
            {
                string sql = "";

                if (bFullUpdate)
                {
                    sql += "insert or replace into users (login, token) values ('" + login + "', '" + token + "');";

                    sql += "update users_click set " +
                        "end_time = '" + current_time_str + "', " +
                        "mouse_clicks = " + m_clicks + ", " +
                        "keyboard_clicks = " + k_clikcs +
                        " where id = (SELECT MAX(id) FROM users_click) and end_time = start_time;";

                    mouseClickCount = 0;
                    keyDownCount = 0;

                    if (bAddNewRecord)
                    {
                        sql += "insert into users_click (login, mouse_clicks, keyboard_clicks, start_time, end_time) values (" +
                            "'" + login + "'," +
                            zero_str + "," +
                            zero_str + "," +
                            "'" + current_time_str + "'," +
                            "'" + current_time_str + "'" +
                            ");";
                    }
                }

                sql += "insert or replace into work_month_tracking (login, month, time) values ('" + login + "', strftime('%Y-%m','now'), " + time_current_month + ");";

                sql += "insert or replace into work_current_day_tracking (login, current_day, time) values ('" + login + "', strftime('%Y-%m-%d','now'), " + time_current_day + ");";

                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            m_dbConnection.Close();
            GC.Collect();

            m_FileSystemReadWriteMutex.ReleaseMutex();

            m_bTackingHooks = false;
        }

        private void ZipAndUploadFunct()
        {
            if (m_bZipAndUploading)
                return;

            m_bZipAndUploading = true;

            m_bLoginAndUploadForce = false;

            string upload_url = Program.GetServerURL().UPLOAD();
            string token_recieved = m_Token;

            int i_60_seconds = 60;

            string current_time_minus_minute_str = DateTime.Now.AddSeconds(-i_60_seconds).ToString(@"yyyy-MM-dd HH:mm:ss");

            var bw = new BackgroundWorker();

            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                bw_e.Result = false;

                if (token_recieved == null || token_recieved.Length == 0)
                    return;

                bool bDone = true;

                string time = DateTime.Now.ToString(@"MM.dd.yyyy-HH.mm.ss");

                string start_path = "data";
                string zip_tmp_path = "zip_tmp";
                string zip_path = "zip\\" + time + ".zip";

                m_FileSystemReadWriteMutex.WaitOne();

                System.IO.DirectoryInfo data_di = new DirectoryInfo(start_path);

                List<string> prepared_files = new List<string>();

                foreach (FileInfo file in data_di.GetFiles())
                {
                    if (file.Extension.ToLower().CompareTo(".jpg") == 0 )
                    {
                        prepared_files.Add(file.FullName);
                    }
                }

                if (prepared_files.Count > 0)
                {
                    try
                    {
                        if (Directory.Exists(zip_tmp_path))
                        {
                            Directory.Delete(zip_tmp_path, true);
                        }

                        Directory.CreateDirectory(zip_tmp_path);

                        foreach (string file_path in prepared_files)
                        {
                            string dest_path = zip_tmp_path + "\\" + Path.GetFileName(file_path);

                            File.Copy(file_path, dest_path);
                        }

                        File.Copy(Program.db_name, zip_tmp_path + "\\" + Program.db_name);

                        ZipFile.CreateFromDirectory(zip_tmp_path, zip_path);

                        Directory.Delete(zip_tmp_path, true);

                        foreach (string file_path in prepared_files)
                        {
                            File.Delete(file_path);
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        MessageBox.Show(ex.Message);
#endif
                        bDone = false;
                    }
                }

                m_FileSystemReadWriteMutex.ReleaseMutex();

                if (bDone)
                {
                    try
                    {
                        System.IO.DirectoryInfo zip_di = new DirectoryInfo("zip");

                        foreach (FileInfo file in zip_di.GetFiles())
                        {
                            if (file.Extension.ToLower().CompareTo(".zip") == 0)
                            {
                                bDone = UploadFileByHttp(upload_url, token_recieved, file.FullName, "application/x-zip-compressed");

                                if (bDone)
                                {
                                    m_FileSystemReadWriteMutex.WaitOne();

                                    try
                                    {
                                        file.Delete();
                                    }
                                    catch (Exception ex)
                                    {
                                        bDone = false;
                                    }

                                    m_FileSystemReadWriteMutex.ReleaseMutex();
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch (WebException ex)
                    {
                        bDone = false;
                    }
                    catch (Exception ex)
                    {
                        bDone = false;
                    }
                }

                bw_e.Result = bDone;
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_args) =>
            {
                if ((bool)bw_args.Result)
                {
                    m_FileSystemReadWriteMutex.WaitOne();

                    m_dbConnection.Open();

                    try
                    {
                        string sql = "delete from users_click where end_time <= '" + current_time_minus_minute_str + "';";

                        SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    m_dbConnection.Close();
                    GC.Collect();

                    m_FileSystemReadWriteMutex.ReleaseMutex();

                    if (m_bFailedMessageShown)
                    {
                        MessageBox.Show("All files was uploaded succesfully");
                        m_bFailedMessageShown = false;
                    }
                }
                else
                {
                    if (!m_bFailedMessageShown)
                    {
                        MessageBox.Show("Could not upload data to the server - check your internet connection");
                        m_bFailedMessageShown = true;
                    }

                    m_bLoginAndUploadForce = true;
                }

                m_bZipAndUploading = false;
            };

            bw.RunWorkerAsync();
        }

        // 4 ( from the end ) Once the computer will go to online we will upload all the data and clear it from the local database.
        public void CheckForInternetConnectionTimerHandler(object sender, EventArgs e_args)
        {
            if (m_bCheckingConnection)
                return;

            m_bCheckingConnection = true;

            m_InternetConnectionChecker.Enabled = false;

            var bw = new BackgroundWorker();

            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                bool bDone = false;

                try
                {
                    using (var client = new WebClient())
                    {
                        using (client.OpenRead("https://www.google.com"))
                        {
                            bDone = true;
                        }
                    }
                }
                catch
                {
                    bDone = false;
                }

                bw_e.Result = bDone;
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_args) =>
            {
                if ((bool)bw_args.Result)
                {
                    if (m_bLoginAndUploadForce)
                    {
                        m_bLoginAndUploadForce = false;

                        this.LogIn(fm_Login.login, fm_Login.password, true);
                    }
                }

                m_bCheckingConnection = false;

                m_InternetConnectionChecker.Enabled = true;
            };

            bw.RunWorkerAsync();
        }

        // 1. Login should be done by online.
        private void LogIn(string login, string password, bool bForceUploadFiles = false)
        {
            if (m_bLoggining)
                return;

            m_bLoggining = true;

            if (m_wsConnection != null && m_wsConnection.IsAlive)
            {
                m_wsConnection.Close();
                m_wsConnection = null;
                m_wsConnectionTimer.Enabled = false;
            }

            fm_Login.login_enabled = false;

            var bw = new BackgroundWorker();

            string url = Program.GetServerURL().LOGIN();

            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                bw_e.Result = string.Empty;

                try
                {
                    string body = "{\"login\":\"" + login + "\",\"password\":\"" + password + "\"}";

                    var contentBytes = Encoding.UTF8.GetBytes(body);
                    var request = (HttpWebRequest)WebRequest.Create(url);

                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Timeout = 15 * 1000;
                    request.ContentLength = contentBytes.Length;

                    request.CookieContainer = new CookieContainer();

                    using (var requestWritter = request.GetRequestStream())
                    {
                        requestWritter.Write(contentBytes, 0, (int)request.ContentLength);
                    }

                    var responseString = string.Empty;
                    var webResponse = (HttpWebResponse)request.GetResponse();
                    var responseStream = webResponse.GetResponseStream();

                    using (var reader = new StreamReader(responseStream))
                    {
                        responseString = reader.ReadToEnd();
                    }

                    bw_e.Result = responseString;
                }
                catch (WebException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_args) =>
            {
                if (bw_args.Error != null)
                    MessageBox.Show(bw_args.Error.ToString());

                var json = bw_args.Result.ToString();

                if (json.Length > 0)
                {
                    int month_time = 0;
                    int day_time = 0;

                    try
                    {
                        m_FileSystemReadWriteMutex.WaitOne();

                        m_dbConnection.Open();

                        {
                            string sql_month_time = "select time from work_month_tracking where login='" + login + "' and month=strftime('%Y-%m','now');";

                            SQLiteCommand command = new SQLiteCommand(sql_month_time, m_dbConnection);
                            SQLiteDataReader reader = command.ExecuteReader();

                            if (reader.Read())
                            {
                                string time_str = reader["time"].ToString();
                                month_time = Convert.ToInt32(time_str, 10);
                                reader.Close();
                            }
                        }

                        {
                            string sql_current_day_time = "select time from work_current_day_tracking where login='" + login + "' and current_day=strftime('%Y-%m-%d','now');";

                            SQLiteCommand command = new SQLiteCommand(sql_current_day_time, m_dbConnection);
                            SQLiteDataReader reader = command.ExecuteReader();

                            if (reader.Read())
                            {
                                string time_str = reader["time"].ToString();
                                day_time = Convert.ToInt32(time_str, 10);
                                reader.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        m_dbConnection.Close();
                        GC.Collect();

                        m_FileSystemReadWriteMutex.ReleaseMutex();
                    }

                    fm_Main.current_month_seconds = month_time;
                    fm_Main.current_day_seconds = day_time;
                    fm_Main.updateTimeLabels();

                    // 1. Once the login is done server will generate an access key 
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    m_Token = js.Deserialize<string[]>(json)[0];

                    fm_Login.Hide();
                    fm_Main.Show();
                    fm_Active = fm_Main;

                    //  8. If upload fails once , it is not uploading again.
                    //  9. If there is no internet connection, it shows message no internet. but after internet comes alive it is not uploading again
                    if (bForceUploadFiles)
                    {
                        ZipAndUploadFunct();
                    }

                    try
                    {
                        string ws_url = Program.GetServerURL().WS_URL();

                        m_wsConnection = new WebSocket(ws_url);

                        m_wsConnection.OnMessage += (sender, e) =>
                        {
                            var bw_notification = new BackgroundWorker();

                            bw_notification.DoWork += (object bw_notif_sender, DoWorkEventArgs bw_notif_e) =>
                            {
                                frmNotification frm_Notification = new frmNotification(e.Data);

                                frm_Notification.StartPosition = FormStartPosition.Manual;
                                Rectangle workingArea = Screen.GetWorkingArea(frm_Notification);
                                frm_Notification.Location = new Point(workingArea.Right - frm_Notification.Width - 1, workingArea.Bottom - frm_Notification.Height - 1);

                                frm_Notification.ShowModal();
                            };

                            bw_notification.RunWorkerCompleted += (object bw_notif_sender, RunWorkerCompletedEventArgs bw_notif_args) =>
                            {
                            };

                            bw_notification.RunWorkerAsync();
                        };

                        m_wsConnection.Connect();

                        if (m_wsConnection.IsAlive)
                        {
                            m_wsConnection.Send(login);
                            m_wsConnectionTimer.Enabled = true;
                        }
                        else
                            MessageBox.Show("notification connection failed");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                m_bLoggining = false;

                fm_Login.login_enabled = true;
            };

            bw.RunWorkerAsync();
        }

        public bool UploadFileByHttp(string url, string token, string filepath, string contentType)
        {
            bool bDone = false;

            url = url + "?token=" + token;

            string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x"); //Identificate separator
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);

            wr.Method = "POST";
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Timeout = 2 * 60 * 1000;

            Stream rs = wr.GetRequestStream();

            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, "filename", Path.GetFileName(filepath), contentType);

            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();
            rs = null;

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream = wresp.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string responseData = reader.ReadToEnd();

                bDone = true;
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            finally
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
                wr = null;
            }
            return bDone;
        }
    }
}