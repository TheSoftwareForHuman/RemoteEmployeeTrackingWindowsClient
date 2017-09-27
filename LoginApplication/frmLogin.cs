using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;

using System.Threading;

using System.Text;
using System.Net;
using System.ComponentModel;

using System.Web.Script.Serialization;

namespace LoginApplication
{
    public partial class frmLogin : Form
    {
        public frmLogin()
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
        }

        public event LogInEvent LogIn;

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.btn_SubmitHandler(sender, e);
            }
        }

        private void btn_SubmitHandler(object sender, EventArgs e)
        {
            if (!this.btn_Submit.Enabled)
                return;

            if (txt_UserName.Text == "" || txt_Password.Text == "")
            {
                MessageBox.Show("Please provide UserName and Password");
                return;
            }
            try
            {
                string login = txt_UserName.Text;
                string password = txt_Password.Text;

                LogInEventArgs arg = new LogInEventArgs(login, password);

                LogIn.Invoke(this, arg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public string login { get { return txt_UserName.Text; } set { txt_UserName.Text = value; } }
        public string password { get { return txt_Password.Text; } set { txt_Password.Text = value; } }
        
        public bool   login_enabled { get { return this.btn_Submit.Enabled; } set { this.btn_Submit.Enabled = value; } }
    }

    public class LogInEventArgs : EventArgs
    {
        public string login { get { return _l; } }
        public string password { get { return _p; } }

        public LogInEventArgs(string l, string p)
        {
            _l = l;
            _p = p;
        }

        string _l,_p;
    }

    public delegate void LogInEvent(object sender, LogInEventArgs e);
}
