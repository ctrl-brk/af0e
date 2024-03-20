namespace N1MMLookup;

partial class ImgForm
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
        picBoxBig = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)picBoxBig).BeginInit();
        SuspendLayout();
        // 
        // picBoxBig
        // 
        picBoxBig.Dock = DockStyle.Fill;
        picBoxBig.Location = new Point(0, 0);
        picBoxBig.Margin = new Padding(1);
        picBoxBig.Name = "picBoxBig";
        picBoxBig.Size = new Size(298, 298);
        picBoxBig.SizeMode = PictureBoxSizeMode.AutoSize;
        picBoxBig.TabIndex = 0;
        picBoxBig.TabStop = false;
        picBoxBig.LoadCompleted += picBoxBig_LoadCompleted;
        // 
        // ImgForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CausesValidation = false;
        ClientSize = new Size(298, 298);
        ControlBox = false;
        Controls.Add(picBoxBig);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MinimumSize = new Size(300, 300);
        Name = "ImgForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ((System.ComponentModel.ISupportInitialize)picBoxBig).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private PictureBox picBoxBig;
}