namespace LoginApplication
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label_current_time = new System.Windows.Forms.Label();
            this.btn_Time = new System.Windows.Forms.Button();
            this.label_day_time = new System.Windows.Forms.Label();
            this.label_month_time = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.button_logout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_current_time
            // 
            this.label_current_time.AutoSize = true;
            this.label_current_time.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label_current_time.Location = new System.Drawing.Point(265, 154);
            this.label_current_time.Name = "label_current_time";
            this.label_current_time.Size = new System.Drawing.Size(212, 55);
            this.label_current_time.TabIndex = 0;
            this.label_current_time.Text = "00:00:00";
            // 
            // btn_Time
            // 
            this.btn_Time.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Time.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Time.Location = new System.Drawing.Point(299, 293);
            this.btn_Time.Name = "btn_Time";
            this.btn_Time.Size = new System.Drawing.Size(150, 32);
            this.btn_Time.TabIndex = 4;
            this.btn_Time.Text = "Start time";
            this.btn_Time.UseVisualStyleBackColor = true;
            this.btn_Time.Click += new System.EventHandler(this.btn_Time_Click);
            // 
            // label_day_time
            // 
            this.label_day_time.AutoSize = true;
            this.label_day_time.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label_day_time.Location = new System.Drawing.Point(440, 74);
            this.label_day_time.Name = "label_day_time";
            this.label_day_time.Size = new System.Drawing.Size(120, 31);
            this.label_day_time.TabIndex = 5;
            this.label_day_time.Text = "00:00:00";
            // 
            // label_month_time
            // 
            this.label_month_time.AutoSize = true;
            this.label_month_time.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label_month_time.Location = new System.Drawing.Point(12, 74);
            this.label_month_time.Name = "label_month_time";
            this.label_month_time.Size = new System.Drawing.Size(120, 31);
            this.label_month_time.TabIndex = 6;
            this.label_month_time.Text = "00:00:00";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(12, 31);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(338, 31);
            this.label4.TabIndex = 7;
            this.label4.Text = "Total worked in this month:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.Location = new System.Drawing.Point(440, 31);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(252, 31);
            this.label5.TabIndex = 8;
            this.label5.Text = "Total worked today:";
            // 
            // button_logout
            // 
            this.button_logout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_logout.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_logout.Location = new System.Drawing.Point(585, 293);
            this.button_logout.Name = "button_logout";
            this.button_logout.Size = new System.Drawing.Size(150, 32);
            this.button_logout.TabIndex = 9;
            this.button_logout.Text = "Logout";
            this.button_logout.UseVisualStyleBackColor = true;
            this.button_logout.Click += new System.EventHandler(this.button_logout_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(747, 337);
            this.Controls.Add(this.button_logout);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label_month_time);
            this.Controls.Add(this.label_day_time);
            this.Controls.Add(this.btn_Time);
            this.Controls.Add(this.label_current_time);
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Time Tracking";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_current_time;
        private System.Windows.Forms.Button btn_Time;
        private System.Windows.Forms.Label label_day_time;
        private System.Windows.Forms.Label label_month_time;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button_logout;
    }
}