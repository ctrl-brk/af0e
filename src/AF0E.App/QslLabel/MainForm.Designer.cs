namespace QslLabel;

partial class MainForm
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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        gridLog = new DataGridView();
        groupBox1 = new GroupBox();
        cbQueued = new CheckBox();
        btnSearch = new Button();
        tbCall = new TextBox();
        label1 = new Label();
        groupBox2 = new GroupBox();
        btnSave = new Button();
        label3 = new Label();
        label2 = new Label();
        cmbTemplate = new ComboBox();
        cmbStartLabelNum = new ComboBox();
        saveDlg = new SaveFileDialog();
        ((System.ComponentModel.ISupportInitialize)gridLog).BeginInit();
        groupBox1.SuspendLayout();
        groupBox2.SuspendLayout();
        SuspendLayout();
        // 
        // gridLog
        // 
        gridLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        gridLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridLog.Location = new Point(10, 68);
        gridLog.Name = "gridLog";
        gridLog.Size = new Size(1204, 492);
        gridLog.TabIndex = 0;
        gridLog.TabStop = false;
        gridLog.Text = "dataGridView1";
        gridLog.CellContentClick += gridLog_CellContentClick;
        gridLog.CellFormatting += gridLog_CellFormatting;
        gridLog.CellMouseEnter += gridLog_CellMouseEnter;
        gridLog.CellMouseLeave += gridLog_CellMouseLeave;
        gridLog.DataBindingComplete += gridLog_DataBindingComplete;
        gridLog.SelectionChanged += gridLog_SelectionChanged;
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(cbQueued);
        groupBox1.Controls.Add(btnSearch);
        groupBox1.Controls.Add(tbCall);
        groupBox1.Controls.Add(label1);
        groupBox1.Location = new Point(10, 6);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new Size(367, 56);
        groupBox1.TabIndex = 1;
        groupBox1.TabStop = false;
        groupBox1.Text = "Filter";
        // 
        // cbQueued
        // 
        cbQueued.Location = new Point(190, 20);
        cbQueued.Name = "cbQueued";
        cbQueued.Size = new Size(72, 22);
        cbQueued.TabIndex = 1;
        cbQueued.Text = "Queued";
        cbQueued.UseVisualStyleBackColor = true;
        // 
        // btnSearch
        // 
        btnSearch.Location = new Point(288, 16);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(67, 28);
        btnSearch.TabIndex = 2;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = true;
        btnSearch.Click += btnSearch_Click;
        // 
        // tbCall
        // 
        tbCall.CharacterCasing = CharacterCasing.Upper;
        tbCall.Location = new Point(64, 19);
        tbCall.Name = "tbCall";
        tbCall.Size = new Size(108, 23);
        tbCall.TabIndex = 0;
        tbCall.KeyPress += tbCall_KeyPress;
        // 
        // label1
        // 
        label1.Location = new Point(12, 20);
        label1.Name = "label1";
        label1.Size = new Size(53, 20);
        label1.TabIndex = 0;
        label1.Text = "Callsign";
        label1.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // groupBox2
        // 
        groupBox2.Controls.Add(btnSave);
        groupBox2.Controls.Add(label3);
        groupBox2.Controls.Add(label2);
        groupBox2.Controls.Add(cmbTemplate);
        groupBox2.Controls.Add(cmbStartLabelNum);
        groupBox2.Location = new Point(404, 6);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new Size(480, 56);
        groupBox2.TabIndex = 2;
        groupBox2.TabStop = false;
        groupBox2.Text = "PDF";
        // 
        // btnSave
        // 
        btnSave.Enabled = false;
        btnSave.Location = new Point(381, 17);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(92, 28);
        btnSave.TabIndex = 2;
        btnSave.Text = "Save PDF";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(222, 24);
        label3.Name = "label3";
        label3.Size = new Size(62, 15);
        label3.TabIndex = 3;
        label3.Text = "Start Label";
        label3.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // label2
        // 
        label2.Location = new Point(10, 19);
        label2.Name = "label2";
        label2.Size = new Size(60, 23);
        label2.TabIndex = 2;
        label2.Text = "Template";
        label2.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbTemplate
        // 
        cmbTemplate.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbTemplate.FormattingEnabled = true;
        cmbTemplate.Items.AddRange(new object[] { "1.3 x 4", "2.0 x 4" });
        cmbTemplate.Location = new Point(72, 19);
        cmbTemplate.Name = "cmbTemplate";
        cmbTemplate.Size = new Size(132, 23);
        cmbTemplate.TabIndex = 0;
        cmbTemplate.SelectedValueChanged += cmbTemplate_SelectedValueChanged;
        // 
        // cmbStartLabelNum
        // 
        cmbStartLabelNum.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStartLabelNum.FormattingEnabled = true;
        cmbStartLabelNum.Location = new Point(291, 20);
        cmbStartLabelNum.Name = "cmbStartLabelNum";
        cmbStartLabelNum.Size = new Size(48, 23);
        cmbStartLabelNum.TabIndex = 1;
        // 
        // saveDlg
        // 
        saveDlg.DefaultExt = "pdf";
        saveDlg.Filter = "PDF|*.pdf|All files|*.*";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1223, 568);
        Controls.Add(groupBox2);
        Controls.Add(groupBox1);
        Controls.Add(gridLog);
        Name = "MainForm";
        Text = "QSL Label";
        WindowState = FormWindowState.Maximized;
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)gridLog).EndInit();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label label2;

    private System.Windows.Forms.CheckBox cbQueued;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.ComboBox cmbStartLabelNum;
    private System.Windows.Forms.ComboBox cmbTemplate;

    private System.Windows.Forms.DataGridView gridLog;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox tbCall;
    private System.Windows.Forms.Button btnSearch;

    #endregion

    private Button btnSave;
    private Label label3;
    private SaveFileDialog saveDlg;
}
