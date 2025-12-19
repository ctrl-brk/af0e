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
        components = new System.ComponentModel.Container();
        gridLog = new DataGridView();
        gbxFilter = new GroupBox();
        dtpEndDate = new DateTimePicker();
        lblPeriod = new Label();
        dtpStartDate = new DateTimePicker();
        cbQueued = new CheckBox();
        btnSearch = new Button();
        tbCall = new TextBox();
        lblCallsign = new Label();
        btnAnalyze = new Button();
        gbxPdf = new GroupBox();
        btnSelectPrint = new Button();
        btnGenPdf = new Button();
        label3 = new Label();
        label2 = new Label();
        cmbTemplate = new ComboBox();
        cmbStartLabelNum = new ComboBox();
        saveDlg = new SaveFileDialog();
        lblStatus = new Label();
        gbxAnalyze = new GroupBox();
        cbDxOnly = new CheckBox();
        btnSave = new Button();
        btnMarkSent = new Button();
        gbxUpdate = new GroupBox();
        gbxView = new GroupBox();
        cbShowWaiting = new CheckBox();
        cbViewMyLocation = new CheckBox();
        mainFormTooltip = new ToolTip(components);
        ((System.ComponentModel.ISupportInitialize)gridLog).BeginInit();
        gbxFilter.SuspendLayout();
        gbxPdf.SuspendLayout();
        gbxAnalyze.SuspendLayout();
        gbxUpdate.SuspendLayout();
        gbxView.SuspendLayout();
        SuspendLayout();
        // 
        // gridLog
        // 
        gridLog.AllowUserToAddRows = false;
        gridLog.AllowUserToDeleteRows = false;
        gridLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        gridLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        gridLog.Location = new Point(0, 107);
        gridLog.Name = "gridLog";
        gridLog.Size = new Size(1127, 851);
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
        gridLog.CellValueChanged += gridLog_CellValueChanged;
        gridLog.ColumnAdded += gridLog_ColumnAdded;
        gridLog.CurrentCellDirtyStateChanged += gridLog_CurrentCellDirtyStateChanged;
        gridLog.DataBindingComplete += gridLog_DataBindingComplete;
        gridLog.EditingControlShowing += gridLog_EditingControlShowing;
        gridLog.RowPostPaint += gridLog_RowPostPaint;
        gridLog.SelectionChanged += gridLog_SelectionChanged;
        // 
        // gbxFilter
        // 
        gbxFilter.Controls.Add(dtpEndDate);
        gbxFilter.Controls.Add(lblPeriod);
        gbxFilter.Controls.Add(dtpStartDate);
        gbxFilter.Controls.Add(cbQueued);
        gbxFilter.Controls.Add(btnSearch);
        gbxFilter.Controls.Add(tbCall);
        gbxFilter.Controls.Add(lblCallsign);
        gbxFilter.Location = new Point(10, 6);
        gbxFilter.Name = "gbxFilter";
        gbxFilter.Size = new Size(322, 95);
        gbxFilter.TabIndex = 1;
        gbxFilter.TabStop = false;
        gbxFilter.Text = "Filter";
        // 
        // dtpEndDate
        // 
        dtpEndDate.Checked = false;
        dtpEndDate.CustomFormat = "yyyy-MM-dd";
        dtpEndDate.Format = DateTimePickerFormat.Custom;
        dtpEndDate.Location = new Point(127, 63);
        dtpEndDate.Name = "dtpEndDate";
        dtpEndDate.ShowCheckBox = true;
        dtpEndDate.Size = new Size(100, 23);
        dtpEndDate.TabIndex = 2;
        // 
        // lblPeriod
        // 
        lblPeriod.AutoSize = true;
        lblPeriod.Location = new Point(126, 15);
        lblPeriod.Name = "lblPeriod";
        lblPeriod.Size = new Size(41, 15);
        lblPeriod.TabIndex = 0;
        lblPeriod.Text = "Period";
        // 
        // dtpStartDate
        // 
        dtpStartDate.Checked = false;
        dtpStartDate.CustomFormat = "yyyy-MM-dd";
        dtpStartDate.Format = DateTimePickerFormat.Custom;
        dtpStartDate.Location = new Point(127, 36);
        dtpStartDate.Name = "dtpStartDate";
        dtpStartDate.ShowCheckBox = true;
        dtpStartDate.Size = new Size(100, 23);
        dtpStartDate.TabIndex = 1;
        // 
        // cbQueued
        // 
        cbQueued.Location = new Point(243, 15);
        cbQueued.Name = "cbQueued";
        cbQueued.Size = new Size(72, 22);
        cbQueued.TabIndex = 3;
        cbQueued.Text = "Queued";
        cbQueued.UseVisualStyleBackColor = true;
        // 
        // btnSearch
        // 
        btnSearch.Location = new Point(241, 46);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(67, 28);
        btnSearch.TabIndex = 4;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = true;
        btnSearch.Click += btnSearch_Click;
        // 
        // tbCall
        // 
        tbCall.CharacterCasing = CharacterCasing.Upper;
        tbCall.Location = new Point(8, 36);
        tbCall.Name = "tbCall";
        tbCall.Size = new Size(108, 23);
        tbCall.TabIndex = 0;
        // 
        // lblCallsign
        // 
        lblCallsign.Location = new Point(6, 15);
        lblCallsign.Name = "lblCallsign";
        lblCallsign.Size = new Size(54, 20);
        lblCallsign.TabIndex = 0;
        lblCallsign.Text = "Callsign";
        lblCallsign.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnAnalyze
        // 
        btnAnalyze.Location = new Point(89, 18);
        btnAnalyze.Name = "btnAnalyze";
        btnAnalyze.Size = new Size(67, 28);
        btnAnalyze.TabIndex = 1;
        btnAnalyze.Text = "Analyze";
        btnAnalyze.UseVisualStyleBackColor = true;
        btnAnalyze.Click += btnAnalyze_Click;
        // 
        // gbxPdf
        // 
        gbxPdf.Controls.Add(btnSelectPrint);
        gbxPdf.Controls.Add(btnGenPdf);
        gbxPdf.Controls.Add(label3);
        gbxPdf.Controls.Add(label2);
        gbxPdf.Controls.Add(cmbTemplate);
        gbxPdf.Controls.Add(cmbStartLabelNum);
        gbxPdf.Location = new Point(517, 6);
        gbxPdf.Name = "gbxPdf";
        gbxPdf.Size = new Size(326, 95);
        gbxPdf.TabIndex = 3;
        gbxPdf.TabStop = false;
        gbxPdf.Text = "PDF";
        // 
        // btnSelectPrint
        // 
        btnSelectPrint.Location = new Point(151, 52);
        btnSelectPrint.Name = "btnSelectPrint";
        btnSelectPrint.Size = new Size(58, 28);
        btnSelectPrint.TabIndex = 2;
        btnSelectPrint.Text = "Select";
        mainFormTooltip.SetToolTip(btnSelectPrint, "Ctrl to invert");
        btnSelectPrint.UseVisualStyleBackColor = true;
        btnSelectPrint.Click += btnSelectPrint_Click;
        // 
        // btnGenPdf
        // 
        btnGenPdf.Enabled = false;
        btnGenPdf.Location = new Point(227, 35);
        btnGenPdf.Name = "btnGenPdf";
        btnGenPdf.Size = new Size(92, 28);
        btnGenPdf.TabIndex = 3;
        btnGenPdf.Text = "Generate";
        btnGenPdf.UseVisualStyleBackColor = true;
        btnGenPdf.Click += btnGenPdf_Click;
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(8, 58);
        label3.Name = "label3";
        label3.Size = new Size(62, 15);
        label3.TabIndex = 0;
        label3.Text = "Start Label";
        label3.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // label2
        // 
        label2.Location = new Point(5, 20);
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
        cmbTemplate.Location = new Point(77, 21);
        cmbTemplate.Name = "cmbTemplate";
        cmbTemplate.Size = new Size(132, 23);
        cmbTemplate.TabIndex = 0;
        cmbTemplate.SelectedValueChanged += cmbTemplate_SelectedValueChanged;
        // 
        // cmbStartLabelNum
        // 
        cmbStartLabelNum.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStartLabelNum.FormattingEnabled = true;
        cmbStartLabelNum.Location = new Point(77, 54);
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
        lblStatus.Location = new Point(0, 942);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(1127, 15);
        lblStatus.TabIndex = 6;
        lblStatus.Text = "Ready";
        lblStatus.TextAlign = ContentAlignment.MiddleRight;
        // 
        // gbxAnalyze
        // 
        gbxAnalyze.Controls.Add(cbDxOnly);
        gbxAnalyze.Controls.Add(btnAnalyze);
        gbxAnalyze.Location = new Point(341, 6);
        gbxAnalyze.Name = "gbxAnalyze";
        gbxAnalyze.Size = new Size(166, 95);
        gbxAnalyze.TabIndex = 2;
        gbxAnalyze.TabStop = false;
        gbxAnalyze.Text = "Analyze";
        // 
        // cbDxOnly
        // 
        cbDxOnly.AutoSize = true;
        cbDxOnly.Checked = true;
        cbDxOnly.CheckState = CheckState.Checked;
        cbDxOnly.Location = new Point(10, 23);
        cbDxOnly.Name = "cbDxOnly";
        cbDxOnly.Size = new Size(67, 19);
        cbDxOnly.TabIndex = 0;
        cbDxOnly.Text = "DX only";
        cbDxOnly.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        btnSave.Enabled = false;
        btnSave.Location = new Point(10, 54);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(74, 28);
        btnSave.TabIndex = 1;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        // 
        // btnMarkSent
        // 
        btnMarkSent.Enabled = false;
        btnMarkSent.Location = new Point(9, 19);
        btnMarkSent.Name = "btnMarkSent";
        btnMarkSent.Size = new Size(75, 28);
        btnMarkSent.TabIndex = 0;
        btnMarkSent.Text = "Mark sent";
        btnMarkSent.UseVisualStyleBackColor = true;
        btnMarkSent.Click += btnMarkSent_Click;
        // 
        // gbxUpdate
        // 
        gbxUpdate.Controls.Add(btnMarkSent);
        gbxUpdate.Controls.Add(btnSave);
        gbxUpdate.Location = new Point(853, 6);
        gbxUpdate.Name = "gbxUpdate";
        gbxUpdate.Size = new Size(95, 95);
        gbxUpdate.TabIndex = 4;
        gbxUpdate.TabStop = false;
        gbxUpdate.Text = "Update";
        // 
        // gbxView
        // 
        gbxView.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        gbxView.Controls.Add(cbShowWaiting);
        gbxView.Controls.Add(cbViewMyLocation);
        gbxView.Location = new Point(997, 6);
        gbxView.Name = "gbxView";
        gbxView.Size = new Size(110, 95);
        gbxView.TabIndex = 5;
        gbxView.TabStop = false;
        gbxView.Text = "View";
        // 
        // cbShowWaiting
        // 
        cbShowWaiting.AutoSize = true;
        cbShowWaiting.Checked = true;
        cbShowWaiting.CheckState = CheckState.Checked;
        cbShowWaiting.Enabled = false;
        cbShowWaiting.Location = new Point(11, 54);
        cbShowWaiting.Name = "cbShowWaiting";
        cbShowWaiting.Size = new Size(67, 19);
        cbShowWaiting.TabIndex = 1;
        cbShowWaiting.Text = "Waiting";
        cbShowWaiting.UseVisualStyleBackColor = true;
        cbShowWaiting.Click += cbShowWaiting_Click;
        // 
        // cbViewMyLocation
        // 
        cbViewMyLocation.AutoSize = true;
        cbViewMyLocation.Enabled = false;
        cbViewMyLocation.Location = new Point(11, 23);
        cbViewMyLocation.Name = "cbViewMyLocation";
        cbViewMyLocation.Size = new Size(89, 19);
        cbViewMyLocation.TabIndex = 0;
        cbViewMyLocation.Text = "My location";
        cbViewMyLocation.UseVisualStyleBackColor = true;
        cbViewMyLocation.Click += cbViewMyLocation_Click;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1127, 958);
        Controls.Add(gbxView);
        Controls.Add(gbxUpdate);
        Controls.Add(gbxAnalyze);
        Controls.Add(lblStatus);
        Controls.Add(gbxPdf);
        Controls.Add(gbxFilter);
        Controls.Add(gridLog);
        Name = "MainForm";
        Text = "QSL Label";
        WindowState = FormWindowState.Maximized;
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)gridLog).EndInit();
        gbxFilter.ResumeLayout(false);
        gbxFilter.PerformLayout();
        gbxPdf.ResumeLayout(false);
        gbxPdf.PerformLayout();
        gbxAnalyze.ResumeLayout(false);
        gbxAnalyze.PerformLayout();
        gbxUpdate.ResumeLayout(false);
        gbxView.ResumeLayout(false);
        gbxView.PerformLayout();
        ResumeLayout(false);
    }

    private System.Windows.Forms.Label label2;

    private System.Windows.Forms.CheckBox cbQueued;
    private System.Windows.Forms.GroupBox gbxPdf;
    private System.Windows.Forms.ComboBox cmbStartLabelNum;
    private System.Windows.Forms.ComboBox cmbTemplate;

    private System.Windows.Forms.DataGridView gridLog;
    private System.Windows.Forms.GroupBox gbxFilter;
    private System.Windows.Forms.Label lblCallsign;
    private System.Windows.Forms.TextBox tbCall;
    private System.Windows.Forms.Button btnSearch;

    #endregion

    private Button btnGenPdf;
    private Label label3;
    private SaveFileDialog saveDlg;
    private Button btnAnalyze;
    private Label lblStatus;
    private GroupBox gbxAnalyze;
    private CheckBox cbDxOnly;
    private Button btnSave;
    private Button btnSelectPrint;
    private Button btnMarkSent;
    private GroupBox gbxUpdate;
    private GroupBox gbxView;
    private CheckBox cbViewMyLocation;
    private DateTimePicker dtpEndDate;
    private Label lblPeriod;
    private DateTimePicker dtpStartDate;
    private CheckBox cbShowWaiting;
    private ToolTip mainFormTooltip;
}
