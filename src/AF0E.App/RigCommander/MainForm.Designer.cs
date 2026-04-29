#nullable disable
namespace RigCommander;
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
        if (disposing)
        {
            _activationIdValidationService?.Dispose();
            _trayShell?.Dispose();
            components?.Dispose();
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
        _titleLabel = new System.Windows.Forms.Label();
        _serverLabel = new System.Windows.Forms.Label();
        _splitContainer = new System.Windows.Forms.SplitContainer();
        _scriptActivityLabel = new System.Windows.Forms.Label();
        _clearScriptLogButton = new System.Windows.Forms.Button();
        _scriptLogBox = new System.Windows.Forms.RichTextBox();
        _errorsLabel = new System.Windows.Forms.Label();
        _clearLogButton = new System.Windows.Forms.Button();
        _logBox = new System.Windows.Forms.RichTextBox();
        _runAtStartupCheckBox = new System.Windows.Forms.CheckBox();
        _hideButton = new System.Windows.Forms.Button();
        _exitButton = new System.Windows.Forms.Button();
        _activationIdLabel = new System.Windows.Forms.Label();
        _activationIdTextBox = new System.Windows.Forms.TextBox();
        _setActivationIdButton = new System.Windows.Forms.Button();
        _clearActivationIdButton = new System.Windows.Forms.Button();
        _activationInfoLabel = new System.Windows.Forms.Label();
        ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
        _splitContainer.Panel1.SuspendLayout();
        _splitContainer.Panel2.SuspendLayout();
        _splitContainer.SuspendLayout();
        SuspendLayout();
        _titleLabel.AutoSize = true;
        _titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)0));
        _titleLabel.Location = new System.Drawing.Point(20, 20);
        _titleLabel.Name = "_titleLabel";
        _titleLabel.Size = new System.Drawing.Size(133, 21);
        _titleLabel.TabIndex = 0;
        _titleLabel.Text = "Rig Commander";
        _serverLabel.AutoSize = true;
        _serverLabel.Location = new System.Drawing.Point(20, 51);
        _serverLabel.Name = "_serverLabel";
        _serverLabel.Size = new System.Drawing.Size(128, 30);
        _serverLabel.TabIndex = 1;
        _serverLabel.Text = "Server: http://localhost\r\nRadio: profile";
        _activationIdLabel.AutoSize = true;
        _activationIdLabel.Location = new System.Drawing.Point(20, 104);
        _activationIdLabel.Name = "_activationIdLabel";
        _activationIdLabel.Size = new System.Drawing.Size(74, 15);
        _activationIdLabel.TabIndex = 2;
        _activationIdLabel.Text = "ActivationId";
        _activationIdTextBox.Location = new System.Drawing.Point(100, 100);
        _activationIdTextBox.Name = "_activationIdTextBox";
        _activationIdTextBox.Size = new System.Drawing.Size(110, 23);
        _activationIdTextBox.TabIndex = 3;
        _setActivationIdButton.Location = new System.Drawing.Point(216, 100);
        _setActivationIdButton.Name = "_setActivationIdButton";
        _setActivationIdButton.Size = new System.Drawing.Size(50, 23);
        _setActivationIdButton.TabIndex = 4;
        _setActivationIdButton.Text = "Set";
        _setActivationIdButton.UseVisualStyleBackColor = true;
        _clearActivationIdButton.Location = new System.Drawing.Point(272, 100);
        _clearActivationIdButton.Name = "_clearActivationIdButton";
        _clearActivationIdButton.Size = new System.Drawing.Size(50, 23);
        _clearActivationIdButton.TabIndex = 5;
        _clearActivationIdButton.Text = "Clear";
        _clearActivationIdButton.UseVisualStyleBackColor = true;
        _activationInfoLabel.AutoEllipsis = true;
        _activationInfoLabel.Location = new System.Drawing.Point(100, 126);
        _activationInfoLabel.Name = "_activationInfoLabel";
        _activationInfoLabel.Size = new System.Drawing.Size(222, 24);
        _activationInfoLabel.TabIndex = 6;
        _activationInfoLabel.Text = " ";
        _splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        _splitContainer.Location = new System.Drawing.Point(20, 154);
        _splitContainer.Name = "_splitContainer";
        _splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
        _splitContainer.Panel1.Controls.Add(_scriptActivityLabel);
        _splitContainer.Panel1.Controls.Add(_clearScriptLogButton);
        _splitContainer.Panel1.Controls.Add(_scriptLogBox);
        _splitContainer.Panel2.Controls.Add(_errorsLabel);
        _splitContainer.Panel2.Controls.Add(_clearLogButton);
        _splitContainer.Panel2.Controls.Add(_logBox);
        _splitContainer.Size = new System.Drawing.Size(504, 226);
        _splitContainer.SplitterDistance = 130;
        _splitContainer.TabIndex = 5;
        _scriptActivityLabel.AutoSize = true;
        _scriptActivityLabel.Location = new System.Drawing.Point(0, 5);
        _scriptActivityLabel.Name = "_scriptActivityLabel";
        _scriptActivityLabel.Size = new System.Drawing.Size(50, 15);
        _scriptActivityLabel.TabIndex = 0;
        _scriptActivityLabel.Text = "Activity:";
        _clearScriptLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        _clearScriptLogButton.Location = new System.Drawing.Point(454, 0);
        _clearScriptLogButton.Margin = new System.Windows.Forms.Padding(0);
        _clearScriptLogButton.Name = "_clearScriptLogButton";
        _clearScriptLogButton.Size = new System.Drawing.Size(50, 22);
        _clearScriptLogButton.TabIndex = 2;
        _clearScriptLogButton.TabStop = false;
        _clearScriptLogButton.Text = "Clear";
        _clearScriptLogButton.UseVisualStyleBackColor = true;
        _clearScriptLogButton.Click += ClearScriptLogButton_Click;
        _scriptLogBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        _scriptLogBox.BackColor = System.Drawing.Color.FromArgb(((int)((byte)30)), ((int)((byte)30)), ((int)((byte)30)));
        _scriptLogBox.Font = new System.Drawing.Font("Consolas", 8.5F);
        _scriptLogBox.ForeColor = System.Drawing.Color.LimeGreen;
        _scriptLogBox.Location = new System.Drawing.Point(0, 25);
        _scriptLogBox.Name = "_scriptLogBox";
        _scriptLogBox.ReadOnly = true;
        _scriptLogBox.Size = new System.Drawing.Size(504, 105);
        _scriptLogBox.TabIndex = 1;
        _scriptLogBox.TabStop = false;
        _scriptLogBox.Text = "";
        _scriptLogBox.WordWrap = false;
        _errorsLabel.AutoSize = true;
        _errorsLabel.Location = new System.Drawing.Point(0, 8);
        _errorsLabel.Name = "_errorsLabel";
        _errorsLabel.Size = new System.Drawing.Size(101, 15);
        _errorsLabel.TabIndex = 0;
        _errorsLabel.Text = "Warnings / Errors:";
        _clearLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right));
        _clearLogButton.Location = new System.Drawing.Point(454, 2);
        _clearLogButton.Margin = new System.Windows.Forms.Padding(0);
        _clearLogButton.Name = "_clearLogButton";
        _clearLogButton.Size = new System.Drawing.Size(50, 22);
        _clearLogButton.TabIndex = 2;
        _clearLogButton.TabStop = false;
        _clearLogButton.Text = "Clear";
        _clearLogButton.UseVisualStyleBackColor = true;
        _clearLogButton.Click += ClearLogButton_Click;
        _logBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
        _logBox.BackColor = System.Drawing.Color.FromArgb(((int)((byte)30)), ((int)((byte)30)), ((int)((byte)30)));
        _logBox.Font = new System.Drawing.Font("Consolas", 8.5F);
        _logBox.ForeColor = System.Drawing.SystemColors.ControlText;
        _logBox.Location = new System.Drawing.Point(0, 26);
        _logBox.Name = "_logBox";
        _logBox.ReadOnly = true;
        _logBox.Size = new System.Drawing.Size(504, 104);
        _logBox.TabIndex = 1;
        _logBox.TabStop = false;
        _logBox.Text = "";
        _logBox.WordWrap = false;
        _runAtStartupCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
        _runAtStartupCheckBox.AutoSize = true;
        _runAtStartupCheckBox.Location = new System.Drawing.Point(20, 405);
        _runAtStartupCheckBox.Name = "_runAtStartupCheckBox";
        _runAtStartupCheckBox.Size = new System.Drawing.Size(152, 19);
        _runAtStartupCheckBox.TabIndex = 5;
        _runAtStartupCheckBox.Text = "Run at Windows startup";
        _runAtStartupCheckBox.UseVisualStyleBackColor = true;
        _runAtStartupCheckBox.CheckedChanged += RunAtStartupCheckBox_CheckedChanged;
        _hideButton.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));
        _hideButton.Location = new System.Drawing.Point(284, 393);
        _hideButton.Name = "_hideButton";
        _hideButton.Size = new System.Drawing.Size(120, 32);
        _hideButton.TabIndex = 6;
        _hideButton.Text = "Hide to Tray";
        _hideButton.UseVisualStyleBackColor = true;
        _hideButton.Click += HideButton_Click;
        _exitButton.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right));
        _exitButton.Location = new System.Drawing.Point(412, 393);
        _exitButton.Name = "_exitButton";
        _exitButton.Size = new System.Drawing.Size(112, 32);
        _exitButton.TabIndex = 7;
        _exitButton.Text = "Exit";
        _exitButton.UseVisualStyleBackColor = true;
        _exitButton.Click += ExitButton_Click;
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(544, 435);
        Controls.Add(_splitContainer);
        Controls.Add(_activationIdLabel);
        Controls.Add(_activationIdTextBox);
        Controls.Add(_setActivationIdButton);
        Controls.Add(_clearActivationIdButton);
        Controls.Add(_activationInfoLabel);
        Controls.Add(_runAtStartupCheckBox);
        Controls.Add(_hideButton);
        Controls.Add(_exitButton);
        Controls.Add(_serverLabel);
        Controls.Add(_titleLabel);
        MinimumSize = new System.Drawing.Size(560, 460);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "Rig Commander";
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;
        Resize += MainForm_Resize;
        _splitContainer.Panel1.ResumeLayout(false);
        _splitContainer.Panel1.PerformLayout();
        _splitContainer.Panel2.ResumeLayout(false);
        _splitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
        _splitContainer.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
    #endregion
    private Label _titleLabel;
    private System.Windows.Forms.Label _serverLabel;
    private SplitContainer _splitContainer;
    private System.Windows.Forms.Label _scriptActivityLabel;
    private System.Windows.Forms.Button _clearScriptLogButton;
    private System.Windows.Forms.RichTextBox _scriptLogBox;
    private System.Windows.Forms.Label _errorsLabel;
    private System.Windows.Forms.Button _clearLogButton;
    private System.Windows.Forms.RichTextBox _logBox;
    private CheckBox _runAtStartupCheckBox;
    private Button _hideButton;
    private Button _exitButton;
    private Label _activationIdLabel;
    private TextBox _activationIdTextBox;
    private Button _setActivationIdButton;
    private Button _clearActivationIdButton;
    private Label _activationInfoLabel;
}
