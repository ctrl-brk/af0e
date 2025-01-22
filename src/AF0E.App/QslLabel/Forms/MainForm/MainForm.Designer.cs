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
        btnAnalyze = new Button();
        groupBox2 = new GroupBox();
        btnGenPdf = new Button();
        label3 = new Label();
        label2 = new Label();
        cmbTemplate = new ComboBox();
        cmbStartLabelNum = new ComboBox();
        saveDlg = new SaveFileDialog();
        lblStatus = new Label();
        gbAnalyze = new GroupBox();
        cbIncludeUS = new CheckBox();
        btnSave = new Button();
        ((System.ComponentModel.ISupportInitialize)gridLog).BeginInit();
        groupBox1.SuspendLayout();
        groupBox2.SuspendLayout();
        gbAnalyze.SuspendLayout();
        SuspendLayout();
        // 
        // gridLog
        // 
        gridLog.AllowUserToAddRows = false;
        gridLog.AllowUserToDeleteRows = false;
        gridLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        gridLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridLog.Location = new Point(10, 68);
        gridLog.Name = "gridLog";
        gridLog.Size = new Size(1577, 481);
        gridLog.TabIndex = 0;
        gridLog.TabStop = false;
        gridLog.Text = "dataGridView1";
        gridLog.CellClick += gridLog_CellClick;
        gridLog.CellContentClick += gridLog_CellContentClick;
        gridLog.CellFormatting += gridLog_CellFormatting;
        gridLog.CellMouseClick += gridLog_CellMouseClick;
        gridLog.CellMouseDown += gridLog_CellMouseDown;
        gridLog.CellMouseEnter += gridLog_CellMouseEnter;
        gridLog.CellPainting += gridLog_CellPainting;
        gridLog.ColumnAdded += gridLog_ColumnAdded;
        gridLog.CurrentCellDirtyStateChanged += gridLog_CurrentCellDirtyStateChanged;
        gridLog.DataBindingComplete += gridLog_DataBindingComplete;
        gridLog.EditingControlShowing += gridLog_EditingControlShowing;
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
        groupBox1.Size = new Size(368, 56);
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
        // btnAnalyze
        // 
        btnAnalyze.Location = new Point(117, 16);
        btnAnalyze.Name = "btnAnalyze";
        btnAnalyze.Size = new Size(67, 28);
        btnAnalyze.TabIndex = 1;
        btnAnalyze.Text = "Analyze";
        btnAnalyze.UseVisualStyleBackColor = true;
        btnAnalyze.Click += btnAnalyze_Click;
        // 
        // groupBox2
        // 
        groupBox2.Controls.Add(btnGenPdf);
        groupBox2.Controls.Add(label3);
        groupBox2.Controls.Add(label2);
        groupBox2.Controls.Add(cmbTemplate);
        groupBox2.Controls.Add(cmbStartLabelNum);
        groupBox2.Location = new Point(607, 6);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new Size(480, 56);
        groupBox2.TabIndex = 3;
        groupBox2.TabStop = false;
        groupBox2.Text = "PDF";
        // 
        // btnGenPdf
        // 
        btnGenPdf.Enabled = false;
        btnGenPdf.Location = new Point(381, 17);
        btnGenPdf.Name = "btnGenPdf";
        btnGenPdf.Size = new Size(92, 28);
        btnGenPdf.TabIndex = 2;
        btnGenPdf.Text = "Generate";
        btnGenPdf.UseVisualStyleBackColor = true;
        btnGenPdf.Click += btnGenPdf_Click;
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
        label2.TabIndex = 0;
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
        // lblStatus
        // 
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.Location = new Point(10, 552);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(1577, 15);
        lblStatus.TabIndex = 3;
        lblStatus.Text = "Ready";
        lblStatus.TextAlign = ContentAlignment.MiddleRight;
        // 
        // gbAnalyze
        // 
        gbAnalyze.Controls.Add(cbIncludeUS);
        gbAnalyze.Controls.Add(btnAnalyze);
        gbAnalyze.Location = new Point(393, 6);
        gbAnalyze.Name = "gbAnalyze";
        gbAnalyze.Size = new Size(197, 56);
        gbAnalyze.TabIndex = 2;
        gbAnalyze.TabStop = false;
        gbAnalyze.Text = "Analyze";
        // 
        // cbIncludeUS
        // 
        cbIncludeUS.AutoSize = true;
        cbIncludeUS.Location = new Point(10, 23);
        cbIncludeUS.Name = "cbIncludeUS";
        cbIncludeUS.Size = new Size(82, 19);
        cbIncludeUS.TabIndex = 0;
        cbIncludeUS.Text = "Include US";
        cbIncludeUS.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSave.Enabled = false;
        btnSave.Location = new Point(1513, 22);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(74, 28);
        btnSave.TabIndex = 4;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1596, 568);
        Controls.Add(btnSave);
        Controls.Add(gbAnalyze);
        Controls.Add(lblStatus);
        Controls.Add(groupBox2);
        Controls.Add(groupBox1);
        Controls.Add(gridLog);
        Name = "MainForm";
        Text = "QSL Label";
        WindowState = FormWindowState.Maximized;
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)gridLog).EndInit();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        gbAnalyze.ResumeLayout(false);
        gbAnalyze.PerformLayout();
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

    private Button btnGenPdf;
    private Label label3;
    private SaveFileDialog saveDlg;
    private Button btnAnalyze;
    private Label lblStatus;
    private GroupBox gbAnalyze;
    private CheckBox cbIncludeUS;
    private Button btnSave;
}
