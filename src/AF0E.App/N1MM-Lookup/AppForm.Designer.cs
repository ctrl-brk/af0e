namespace N1MMLookup;

partial class AppForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblCall = new Label();
        lblAddr = new Label();
        lblCity = new Label();
        btnError = new Button();
        lblName = new Label();
        lblCountry = new Label();
        lnkQrz = new LinkLabel();
        picBox = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)picBox).BeginInit();
        SuspendLayout();
        // 
        // lblCall
        // 
        lblCall.AutoSize = true;
        lblCall.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 204);
        lblCall.ForeColor = Color.SteelBlue;
        lblCall.Location = new Point(-2, -2);
        lblCall.Margin = new Padding(0);
        lblCall.Name = "lblCall";
        lblCall.Size = new Size(47, 21);
        lblCall.TabIndex = 1;
        lblCall.Text = "CALL";
        // 
        // lblAddr
        // 
        lblAddr.AutoEllipsis = true;
        lblAddr.AutoSize = true;
        lblAddr.Location = new Point(1, 33);
        lblAddr.Margin = new Padding(0);
        lblAddr.Name = "lblAddr";
        lblAddr.Size = new Size(49, 15);
        lblAddr.TabIndex = 3;
        lblAddr.Text = "Address";
        // 
        // lblCity
        // 
        lblCity.AutoEllipsis = true;
        lblCity.AutoSize = true;
        lblCity.Location = new Point(1, 48);
        lblCity.Margin = new Padding(0);
        lblCity.Name = "lblCity";
        lblCity.Size = new Size(83, 15);
        lblCity.TabIndex = 4;
        lblCity.Text = "City, State, Zip";
        // 
        // btnError
        // 
        btnError.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnError.ForeColor = Color.Firebrick;
        btnError.Location = new Point(470, 272);
        btnError.Margin = new Padding(1);
        btnError.Name = "btnError";
        btnError.Size = new Size(59, 31);
        btnError.TabIndex = 7;
        btnError.Text = "Error";
        btnError.UseVisualStyleBackColor = true;
        btnError.Visible = false;
        btnError.Click += btnError_Click;
        // 
        // lblName
        // 
        lblName.AutoEllipsis = true;
        lblName.AutoSize = true;
        lblName.Location = new Point(1, 18);
        lblName.Margin = new Padding(0);
        lblName.Name = "lblName";
        lblName.Size = new Size(72, 15);
        lblName.TabIndex = 2;
        lblName.Text = "Name, Class";
        // 
        // lblCountry
        // 
        lblCountry.AutoEllipsis = true;
        lblCountry.AutoSize = true;
        lblCountry.Location = new Point(1, 63);
        lblCountry.Margin = new Padding(0);
        lblCountry.Name = "lblCountry";
        lblCountry.Size = new Size(50, 15);
        lblCountry.TabIndex = 5;
        lblCountry.Text = "Country";
        // 
        // lnkQrz
        // 
        lnkQrz.ActiveLinkColor = Color.SteelBlue;
        lnkQrz.AutoSize = true;
        lnkQrz.Enabled = false;
        lnkQrz.LinkColor = Color.SteelBlue;
        lnkQrz.Location = new Point(1, 77);
        lnkQrz.Margin = new Padding(0);
        lnkQrz.Name = "lnkQrz";
        lnkQrz.Size = new Size(50, 15);
        lnkQrz.TabIndex = 6;
        lnkQrz.TabStop = true;
        lnkQrz.Text = "qrz.com";
        lnkQrz.LinkClicked += lnkQrz_LinkClicked;
        lnkQrz.MouseLeave += lnkQrz_MouseLeave;
        lnkQrz.MouseHover += lnkQrz_MouseHover;
        // 
        // picBox
        // 
        picBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        picBox.BackColor = SystemColors.Control;
        picBox.Location = new Point(231, 3);
        picBox.Name = "picBox";
        picBox.Size = new Size(300, 300);
        picBox.SizeMode = PictureBoxSizeMode.Zoom;
        picBox.TabIndex = 10;
        picBox.TabStop = false;
        picBox.Visible = false;
        picBox.VisibleChanged += picBox_VisibleChanged;
        picBox.Click += picBox_Click;
        // 
        // AppForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CausesValidation = false;
        ClientSize = new Size(534, 306);
        Controls.Add(btnError);
        Controls.Add(lnkQrz);
        Controls.Add(lblCountry);
        Controls.Add(lblName);
        Controls.Add(lblCity);
        Controls.Add(lblAddr);
        Controls.Add(lblCall);
        Controls.Add(picBox);
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(250, 132);
        Name = "AppForm";
        ShowIcon = false;
        Text = "AFØE Lookup";
        FormClosing += AppForm_FormClosing;
        Load += AppForm_Load;
        SizeChanged += AppForm_SizeChanged;
        Move += AppForm_Move;
        ((System.ComponentModel.ISupportInitialize)picBox).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblCall;
    private Label lblAddr;
    private Label lblCity;
    private Button btnError;
    private Label lblName;
    private Label lblCountry;
    private LinkLabel lnkQrz;
    private PictureBox picBox;
}
