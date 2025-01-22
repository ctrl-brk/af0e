namespace QslLabel.Forms.LogForm
{
    partial class LogForm
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
            lvLog = new ListView();
            SuspendLayout();
            // 
            // lvLog
            // 
            lvLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvLog.FullRowSelect = true;
            lvLog.GridLines = true;
            lvLog.Location = new Point(3, 5);
            lvLog.Name = "lvLog";
            lvLog.Size = new Size(476, 452);
            lvLog.TabIndex = 0;
            lvLog.UseCompatibleStateImageBehavior = false;
            lvLog.View = View.Details;
            lvLog.ColumnClick += lvLog_ColumnClick;
            lvLog.KeyPress += lvCountryLog_KeyPress;
            // 
            // LogForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 461);
            Controls.Add(lvLog);
            MinimizeBox = false;
            Name = "LogForm";
            Text = "LogForm";
            Load += CountryForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ListView lvLog;
    }
}
