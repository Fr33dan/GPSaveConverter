namespace GPSaveConverter
{
    partial class SaveFileConverterForm
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
            this.components = new System.ComponentModel.Container();
            this.nonXboxProfileListBox = new System.Windows.Forms.ListBox();
            this.packagesLabel = new System.Windows.Forms.Label();
            this.nonXboxProfileLabel = new System.Windows.Forms.Label();
            this.nonXboxFilesLabel = new System.Windows.Forms.Label();
            this.xboxFileLabel = new System.Windows.Forms.Label();
            this.xboxFilesTable = new System.Windows.Forms.DataGridView();
            this.nonXboxFilesTable = new System.Windows.Forms.DataGridView();
            this.saveFilesBasePanel = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.promptNonXboxLocationButton = new System.Windows.Forms.Button();
            this.viewNonXboxFileButton = new System.Windows.Forms.Button();
            this.xboxFileBasePanel = new System.Windows.Forms.Panel();
            this.viewXboxFilesButton = new System.Windows.Forms.Button();
            this.moveControlPanel = new System.Windows.Forms.TableLayoutPanel();
            this.moveAllFromXboxButton = new System.Windows.Forms.Button();
            this.moveSelectionFromXboxButton = new System.Windows.Forms.Button();
            this.moveAllToXboxButton = new System.Windows.Forms.Button();
            this.moveSelectionToXboxButton = new System.Windows.Forms.Button();
            this.packagesBasePanel = new System.Windows.Forms.Panel();
            this.packagesScrollPanel = new System.Windows.Forms.Panel();
            this.packagesDataGridView = new System.Windows.Forms.DataGridView();
            this.GameIcon = new System.Windows.Forms.DataGridViewImageColumn();
            this.GameName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.foldersToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.infoStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.profilesBasePanel = new System.Windows.Forms.TableLayoutPanel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.xboxProfileListBox = new System.Windows.Forms.ListBox();
            this.xboxProfileLabel = new System.Windows.Forms.Label();
            this.nonXboxProfilePanel = new System.Windows.Forms.Panel();
            this.buttonsToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGameProfileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadGameProfileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showFileTranslationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileTranslationPanel = new System.Windows.Forms.Panel();
            this.fileTranslationPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.removeTranslationButton = new System.Windows.Forms.Button();
            this.addTranslationButton = new System.Windows.Forms.Button();
            this.fileTranslationListBox = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nonXboxFilesTable)).BeginInit();
            this.saveFilesBasePanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.xboxFileBasePanel.SuspendLayout();
            this.moveControlPanel.SuspendLayout();
            this.packagesBasePanel.SuspendLayout();
            this.packagesScrollPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagesDataGridView)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.profilesBasePanel.SuspendLayout();
            this.panel3.SuspendLayout();
            this.nonXboxProfilePanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.fileTranslationPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // nonXboxProfileListBox
            // 
            this.nonXboxProfileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nonXboxProfileListBox.Enabled = false;
            this.nonXboxProfileListBox.FormattingEnabled = true;
            this.nonXboxProfileListBox.Location = new System.Drawing.Point(0, 19);
            this.nonXboxProfileListBox.Name = "nonXboxProfileListBox";
            this.nonXboxProfileListBox.Size = new System.Drawing.Size(204, 95);
            this.nonXboxProfileListBox.TabIndex = 7;
            this.nonXboxProfileListBox.SelectedIndexChanged += new System.EventHandler(this.profileListBox_SelectedIndexChanged);
            // 
            // packagesLabel
            // 
            this.packagesLabel.AutoSize = true;
            this.packagesLabel.Location = new System.Drawing.Point(3, 3);
            this.packagesLabel.Name = "packagesLabel";
            this.packagesLabel.Size = new System.Drawing.Size(129, 13);
            this.packagesLabel.TabIndex = 8;
            this.packagesLabel.Text = "Potential Xbox Packages:";
            // 
            // nonXboxProfileLabel
            // 
            this.nonXboxProfileLabel.AutoSize = true;
            this.nonXboxProfileLabel.Location = new System.Drawing.Point(3, 3);
            this.nonXboxProfileLabel.Name = "nonXboxProfileLabel";
            this.nonXboxProfileLabel.Size = new System.Drawing.Size(154, 13);
            this.nonXboxProfileLabel.TabIndex = 9;
            this.nonXboxProfileLabel.Text = "Non-Xbox Profile: (if applicable)";
            // 
            // nonXboxFilesLabel
            // 
            this.nonXboxFilesLabel.AutoSize = true;
            this.nonXboxFilesLabel.Location = new System.Drawing.Point(3, 15);
            this.nonXboxFilesLabel.Name = "nonXboxFilesLabel";
            this.nonXboxFilesLabel.Size = new System.Drawing.Size(109, 13);
            this.nonXboxFilesLabel.TabIndex = 10;
            this.nonXboxFilesLabel.Text = "Non-Xbox Save Files:";
            // 
            // xboxFileLabel
            // 
            this.xboxFileLabel.AutoSize = true;
            this.xboxFileLabel.Location = new System.Drawing.Point(3, 15);
            this.xboxFileLabel.Name = "xboxFileLabel";
            this.xboxFileLabel.Size = new System.Drawing.Size(86, 13);
            this.xboxFileLabel.TabIndex = 12;
            this.xboxFileLabel.Text = "Xbox Save Files:";
            // 
            // xboxFilesTable
            // 
            this.xboxFilesTable.AllowUserToAddRows = false;
            this.xboxFilesTable.AllowUserToDeleteRows = false;
            this.xboxFilesTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.xboxFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.xboxFilesTable.Location = new System.Drawing.Point(3, 31);
            this.xboxFilesTable.Name = "xboxFilesTable";
            this.xboxFilesTable.ReadOnly = true;
            this.xboxFilesTable.RowHeadersVisible = false;
            this.xboxFilesTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.xboxFilesTable.Size = new System.Drawing.Size(497, 265);
            this.xboxFilesTable.TabIndex = 13;
            this.xboxFilesTable.SelectionChanged += new System.EventHandler(this.xboxFilesTable_SelectionChanged);
            // 
            // nonXboxFilesTable
            // 
            this.nonXboxFilesTable.AllowUserToAddRows = false;
            this.nonXboxFilesTable.AllowUserToDeleteRows = false;
            this.nonXboxFilesTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nonXboxFilesTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.nonXboxFilesTable.Location = new System.Drawing.Point(0, 31);
            this.nonXboxFilesTable.Margin = new System.Windows.Forms.Padding(0);
            this.nonXboxFilesTable.Name = "nonXboxFilesTable";
            this.nonXboxFilesTable.ReadOnly = true;
            this.nonXboxFilesTable.RowHeadersVisible = false;
            this.nonXboxFilesTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.nonXboxFilesTable.Size = new System.Drawing.Size(500, 265);
            this.nonXboxFilesTable.TabIndex = 14;
            this.nonXboxFilesTable.SelectionChanged += new System.EventHandler(this.nonXboxFilesTable_SelectionChanged);
            // 
            // saveFilesBasePanel
            // 
            this.saveFilesBasePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFilesBasePanel.ColumnCount = 1;
            this.saveFilesBasePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.saveFilesBasePanel.Controls.Add(this.panel1, 0, 2);
            this.saveFilesBasePanel.Controls.Add(this.xboxFileBasePanel, 0, 0);
            this.saveFilesBasePanel.Controls.Add(this.moveControlPanel, 0, 1);
            this.saveFilesBasePanel.Location = new System.Drawing.Point(422, 27);
            this.saveFilesBasePanel.Name = "saveFilesBasePanel";
            this.saveFilesBasePanel.RowCount = 3;
            this.saveFilesBasePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.saveFilesBasePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.saveFilesBasePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.saveFilesBasePanel.Size = new System.Drawing.Size(500, 642);
            this.saveFilesBasePanel.TabIndex = 15;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.promptNonXboxLocationButton);
            this.panel1.Controls.Add(this.viewNonXboxFileButton);
            this.panel1.Controls.Add(this.nonXboxFilesTable);
            this.panel1.Controls.Add(this.nonXboxFilesLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 346);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(500, 296);
            this.panel1.TabIndex = 0;
            // 
            // promptNonXboxLocationButton
            // 
            this.promptNonXboxLocationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.promptNonXboxLocationButton.Enabled = false;
            this.promptNonXboxLocationButton.Location = new System.Drawing.Point(211, 3);
            this.promptNonXboxLocationButton.Name = "promptNonXboxLocationButton";
            this.promptNonXboxLocationButton.Size = new System.Drawing.Size(156, 22);
            this.promptNonXboxLocationButton.TabIndex = 16;
            this.promptNonXboxLocationButton.Text = "Select non-Xbox Location";
            this.buttonsToolTip.SetToolTip(this.promptNonXboxLocationButton, "Select non-Xbox save file location (overrides library)");
            this.promptNonXboxLocationButton.UseVisualStyleBackColor = true;
            this.promptNonXboxLocationButton.Click += new System.EventHandler(this.promptNonXboxLocationButton_Click);
            // 
            // viewNonXboxFileButton
            // 
            this.viewNonXboxFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.viewNonXboxFileButton.Enabled = false;
            this.viewNonXboxFileButton.Location = new System.Drawing.Point(373, 3);
            this.viewNonXboxFileButton.Name = "viewNonXboxFileButton";
            this.viewNonXboxFileButton.Size = new System.Drawing.Size(124, 22);
            this.viewNonXboxFileButton.TabIndex = 15;
            this.viewNonXboxFileButton.Text = "Explore non-Xbox Files";
            this.buttonsToolTip.SetToolTip(this.viewNonXboxFileButton, "Open non-Xbox files in Windows Explorer");
            this.viewNonXboxFileButton.UseVisualStyleBackColor = true;
            this.viewNonXboxFileButton.Click += new System.EventHandler(this.viewNonXboxFileButton_Click);
            // 
            // xboxFileBasePanel
            // 
            this.xboxFileBasePanel.Controls.Add(this.viewXboxFilesButton);
            this.xboxFileBasePanel.Controls.Add(this.xboxFileLabel);
            this.xboxFileBasePanel.Controls.Add(this.xboxFilesTable);
            this.xboxFileBasePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xboxFileBasePanel.Location = new System.Drawing.Point(0, 0);
            this.xboxFileBasePanel.Margin = new System.Windows.Forms.Padding(0);
            this.xboxFileBasePanel.Name = "xboxFileBasePanel";
            this.xboxFileBasePanel.Size = new System.Drawing.Size(500, 296);
            this.xboxFileBasePanel.TabIndex = 16;
            // 
            // viewXboxFilesButton
            // 
            this.viewXboxFilesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.viewXboxFilesButton.Enabled = false;
            this.viewXboxFilesButton.Location = new System.Drawing.Point(373, 3);
            this.viewXboxFilesButton.Name = "viewXboxFilesButton";
            this.viewXboxFilesButton.Size = new System.Drawing.Size(124, 22);
            this.viewXboxFilesButton.TabIndex = 14;
            this.viewXboxFilesButton.Text = "Explore Xbox Files";
            this.buttonsToolTip.SetToolTip(this.viewXboxFilesButton, "Open Xbox files in Windows Explorer");
            this.viewXboxFilesButton.UseVisualStyleBackColor = true;
            this.viewXboxFilesButton.Click += new System.EventHandler(this.viewXboxFilesButton_Click);
            // 
            // moveControlPanel
            // 
            this.moveControlPanel.ColumnCount = 6;
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.moveControlPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveControlPanel.Controls.Add(this.moveAllFromXboxButton, 4, 0);
            this.moveControlPanel.Controls.Add(this.moveSelectionFromXboxButton, 3, 0);
            this.moveControlPanel.Controls.Add(this.moveAllToXboxButton, 2, 0);
            this.moveControlPanel.Controls.Add(this.moveSelectionToXboxButton, 1, 0);
            this.moveControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.moveControlPanel.Location = new System.Drawing.Point(3, 299);
            this.moveControlPanel.Name = "moveControlPanel";
            this.moveControlPanel.RowCount = 1;
            this.moveControlPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.moveControlPanel.Size = new System.Drawing.Size(494, 44);
            this.moveControlPanel.TabIndex = 17;
            // 
            // moveAllFromXboxButton
            // 
            this.moveAllFromXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveAllFromXboxButton.Location = new System.Drawing.Point(286, 4);
            this.moveAllFromXboxButton.Name = "moveAllFromXboxButton";
            this.moveAllFromXboxButton.Size = new System.Drawing.Size(27, 36);
            this.moveAllFromXboxButton.TabIndex = 3;
            this.moveAllFromXboxButton.Text = "˅\r\n˅";
            this.buttonsToolTip.SetToolTip(this.moveAllFromXboxButton, "Move all files to the non-Xbox saves");
            this.moveAllFromXboxButton.UseVisualStyleBackColor = true;
            this.moveAllFromXboxButton.Click += new System.EventHandler(this.moveAllFromXboxButton_Click);
            // 
            // moveSelectionFromXboxButton
            // 
            this.moveSelectionFromXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveSelectionFromXboxButton.Location = new System.Drawing.Point(251, 4);
            this.moveSelectionFromXboxButton.Name = "moveSelectionFromXboxButton";
            this.moveSelectionFromXboxButton.Size = new System.Drawing.Size(27, 36);
            this.moveSelectionFromXboxButton.TabIndex = 2;
            this.moveSelectionFromXboxButton.Text = "˅";
            this.buttonsToolTip.SetToolTip(this.moveSelectionFromXboxButton, "Move the selected files to the non-Xbox saves");
            this.moveSelectionFromXboxButton.UseVisualStyleBackColor = true;
            this.moveSelectionFromXboxButton.Click += new System.EventHandler(this.moveSelectionFromXboxButton_Click);
            // 
            // moveAllToXboxButton
            // 
            this.moveAllToXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveAllToXboxButton.Location = new System.Drawing.Point(216, 4);
            this.moveAllToXboxButton.Name = "moveAllToXboxButton";
            this.moveAllToXboxButton.Size = new System.Drawing.Size(27, 36);
            this.moveAllToXboxButton.TabIndex = 1;
            this.moveAllToXboxButton.Text = "˄\r\n˄";
            this.buttonsToolTip.SetToolTip(this.moveAllToXboxButton, "Move all files to the Xbox saves");
            this.moveAllToXboxButton.UseVisualStyleBackColor = true;
            this.moveAllToXboxButton.Click += new System.EventHandler(this.moveAllToXboxButton_Click);
            // 
            // moveSelectionToXboxButton
            // 
            this.moveSelectionToXboxButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.moveSelectionToXboxButton.Location = new System.Drawing.Point(181, 4);
            this.moveSelectionToXboxButton.Name = "moveSelectionToXboxButton";
            this.moveSelectionToXboxButton.Size = new System.Drawing.Size(27, 36);
            this.moveSelectionToXboxButton.TabIndex = 0;
            this.moveSelectionToXboxButton.Text = "˄";
            this.buttonsToolTip.SetToolTip(this.moveSelectionToXboxButton, "Move the selected files to the Xbox saves");
            this.moveSelectionToXboxButton.UseVisualStyleBackColor = true;
            this.moveSelectionToXboxButton.Click += new System.EventHandler(this.moveSelectionToXboxButton_Click);
            // 
            // packagesBasePanel
            // 
            this.packagesBasePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.packagesBasePanel.Controls.Add(this.packagesScrollPanel);
            this.packagesBasePanel.Controls.Add(this.packagesLabel);
            this.packagesBasePanel.Location = new System.Drawing.Point(9, 27);
            this.packagesBasePanel.Margin = new System.Windows.Forms.Padding(0);
            this.packagesBasePanel.Name = "packagesBasePanel";
            this.packagesBasePanel.Size = new System.Drawing.Size(407, 296);
            this.packagesBasePanel.TabIndex = 16;
            // 
            // packagesScrollPanel
            // 
            this.packagesScrollPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packagesScrollPanel.AutoScroll = true;
            this.packagesScrollPanel.Controls.Add(this.packagesDataGridView);
            this.packagesScrollPanel.Location = new System.Drawing.Point(0, 19);
            this.packagesScrollPanel.Name = "packagesScrollPanel";
            this.packagesScrollPanel.Size = new System.Drawing.Size(407, 274);
            this.packagesScrollPanel.TabIndex = 9;
            // 
            // packagesDataGridView
            // 
            this.packagesDataGridView.AllowUserToAddRows = false;
            this.packagesDataGridView.AllowUserToDeleteRows = false;
            this.packagesDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packagesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.packagesDataGridView.ColumnHeadersVisible = false;
            this.packagesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.GameIcon,
            this.GameName});
            this.packagesDataGridView.Location = new System.Drawing.Point(0, 0);
            this.packagesDataGridView.Margin = new System.Windows.Forms.Padding(0);
            this.packagesDataGridView.MultiSelect = false;
            this.packagesDataGridView.Name = "packagesDataGridView";
            this.packagesDataGridView.ReadOnly = true;
            this.packagesDataGridView.RowHeadersVisible = false;
            this.packagesDataGridView.RowTemplate.Height = 75;
            this.packagesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.packagesDataGridView.Size = new System.Drawing.Size(407, 274);
            this.packagesDataGridView.TabIndex = 9;
            this.packagesDataGridView.Click += new System.EventHandler(this.packagesDataGridView_Click);
            // 
            // GameIcon
            // 
            this.GameIcon.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.GameIcon.DataPropertyName = "GameIcon";
            this.GameIcon.Frozen = true;
            this.GameIcon.HeaderText = "Icon";
            this.GameIcon.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.GameIcon.Name = "GameIcon";
            this.GameIcon.ReadOnly = true;
            this.GameIcon.Width = 75;
            // 
            // GameName
            // 
            this.GameName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.GameName.DataPropertyName = "Name";
            this.GameName.HeaderText = "Name";
            this.GameName.Name = "GameName";
            this.GameName.ReadOnly = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.infoStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 683);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(934, 22);
            this.statusStrip1.TabIndex = 16;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // infoStatusLabel
            // 
            this.infoStatusLabel.Name = "infoStatusLabel";
            this.infoStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // profilesBasePanel
            // 
            this.profilesBasePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.profilesBasePanel.ColumnCount = 2;
            this.profilesBasePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.profilesBasePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.profilesBasePanel.Controls.Add(this.panel3, 0, 0);
            this.profilesBasePanel.Controls.Add(this.nonXboxProfilePanel, 1, 0);
            this.profilesBasePanel.Location = new System.Drawing.Point(9, 549);
            this.profilesBasePanel.Margin = new System.Windows.Forms.Padding(0);
            this.profilesBasePanel.Name = "profilesBasePanel";
            this.profilesBasePanel.RowCount = 1;
            this.profilesBasePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.profilesBasePanel.Size = new System.Drawing.Size(407, 120);
            this.profilesBasePanel.TabIndex = 10;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.xboxProfileListBox);
            this.panel3.Controls.Add(this.xboxProfileLabel);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(203, 120);
            this.panel3.TabIndex = 1;
            // 
            // xboxProfileListBox
            // 
            this.xboxProfileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.xboxProfileListBox.Enabled = false;
            this.xboxProfileListBox.FormattingEnabled = true;
            this.xboxProfileListBox.Location = new System.Drawing.Point(0, 19);
            this.xboxProfileListBox.Name = "xboxProfileListBox";
            this.xboxProfileListBox.Size = new System.Drawing.Size(200, 95);
            this.xboxProfileListBox.TabIndex = 7;
            this.xboxProfileListBox.SelectedIndexChanged += new System.EventHandler(this.xboxProfileListBox_SelectedIndexChanged);
            // 
            // xboxProfileLabel
            // 
            this.xboxProfileLabel.AutoSize = true;
            this.xboxProfileLabel.Location = new System.Drawing.Point(3, 3);
            this.xboxProfileLabel.Name = "xboxProfileLabel";
            this.xboxProfileLabel.Size = new System.Drawing.Size(66, 13);
            this.xboxProfileLabel.TabIndex = 9;
            this.xboxProfileLabel.Text = "Xbox Profile:";
            // 
            // nonXboxProfilePanel
            // 
            this.nonXboxProfilePanel.Controls.Add(this.nonXboxProfileListBox);
            this.nonXboxProfilePanel.Controls.Add(this.nonXboxProfileLabel);
            this.nonXboxProfilePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nonXboxProfilePanel.Location = new System.Drawing.Point(203, 0);
            this.nonXboxProfilePanel.Margin = new System.Windows.Forms.Padding(0);
            this.nonXboxProfilePanel.Name = "nonXboxProfilePanel";
            this.nonXboxProfilePanel.Size = new System.Drawing.Size(204, 120);
            this.nonXboxProfilePanel.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(934, 24);
            this.menuStrip1.TabIndex = 17;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveGameProfileToolStripMenuItem,
            this.loadGameProfileToolStripMenuItem,
            this.preferencesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveGameProfileToolStripMenuItem
            // 
            this.saveGameProfileToolStripMenuItem.Name = "saveGameProfileToolStripMenuItem";
            this.saveGameProfileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveGameProfileToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.saveGameProfileToolStripMenuItem.Text = "Save Game Profile";
            this.saveGameProfileToolStripMenuItem.Click += new System.EventHandler(this.saveGameProfileToolStripMenuItem_Click);
            // 
            // loadGameProfileToolStripMenuItem
            // 
            this.loadGameProfileToolStripMenuItem.Name = "loadGameProfileToolStripMenuItem";
            this.loadGameProfileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.loadGameProfileToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.loadGameProfileToolStripMenuItem.Text = "Load Game Profile";
            this.loadGameProfileToolStripMenuItem.Click += new System.EventHandler(this.loadGameProfileToolStripMenuItem_Click);
            // 
            // preferencesToolStripMenuItem
            // 
            this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.preferencesToolStripMenuItem.Text = "Preferences";
            this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.preferencesToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showFileTranslationsToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // showFileTranslationsToolStripMenuItem
            // 
            this.showFileTranslationsToolStripMenuItem.Checked = true;
            this.showFileTranslationsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showFileTranslationsToolStripMenuItem.Name = "showFileTranslationsToolStripMenuItem";
            this.showFileTranslationsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.showFileTranslationsToolStripMenuItem.Text = "Show File Translations";
            this.showFileTranslationsToolStripMenuItem.Click += new System.EventHandler(this.showFileTranslationsToolStripMenuItem_Click);
            // 
            // fileTranslationPanel
            // 
            this.fileTranslationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.fileTranslationPanel.Controls.Add(this.fileTranslationPropertyGrid);
            this.fileTranslationPanel.Controls.Add(this.removeTranslationButton);
            this.fileTranslationPanel.Controls.Add(this.addTranslationButton);
            this.fileTranslationPanel.Controls.Add(this.fileTranslationListBox);
            this.fileTranslationPanel.Location = new System.Drawing.Point(9, 326);
            this.fileTranslationPanel.Name = "fileTranslationPanel";
            this.fileTranslationPanel.Size = new System.Drawing.Size(407, 220);
            this.fileTranslationPanel.TabIndex = 18;
            // 
            // fileTranslationPropertyGrid
            // 
            this.fileTranslationPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileTranslationPropertyGrid.Location = new System.Drawing.Point(138, 3);
            this.fileTranslationPropertyGrid.Name = "fileTranslationPropertyGrid";
            this.fileTranslationPropertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.fileTranslationPropertyGrid.Size = new System.Drawing.Size(266, 214);
            this.fileTranslationPropertyGrid.TabIndex = 3;
            // 
            // removeTranslationButton
            // 
            this.removeTranslationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.removeTranslationButton.Location = new System.Drawing.Point(0, 194);
            this.removeTranslationButton.Name = "removeTranslationButton";
            this.removeTranslationButton.Size = new System.Drawing.Size(23, 23);
            this.removeTranslationButton.TabIndex = 2;
            this.removeTranslationButton.Text = "-";
            this.removeTranslationButton.UseVisualStyleBackColor = true;
            this.removeTranslationButton.Click += new System.EventHandler(this.removeTranslationButton_Click);
            // 
            // addTranslationButton
            // 
            this.addTranslationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addTranslationButton.Location = new System.Drawing.Point(109, 194);
            this.addTranslationButton.Name = "addTranslationButton";
            this.addTranslationButton.Size = new System.Drawing.Size(23, 23);
            this.addTranslationButton.TabIndex = 1;
            this.addTranslationButton.Text = "+";
            this.addTranslationButton.UseVisualStyleBackColor = true;
            this.addTranslationButton.Click += new System.EventHandler(this.addTranslationButton_Click);
            // 
            // fileTranslationListBox
            // 
            this.fileTranslationListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.fileTranslationListBox.FormattingEnabled = true;
            this.fileTranslationListBox.Location = new System.Drawing.Point(0, 3);
            this.fileTranslationListBox.Name = "fileTranslationListBox";
            this.fileTranslationListBox.Size = new System.Drawing.Size(132, 173);
            this.fileTranslationListBox.TabIndex = 0;
            this.fileTranslationListBox.SelectedIndexChanged += new System.EventHandler(this.fileTranslationListBox_SelectedIndexChanged);
            // 
            // SaveFileConverterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 705);
            this.Controls.Add(this.fileTranslationPanel);
            this.Controls.Add(this.packagesBasePanel);
            this.Controls.Add(this.profilesBasePanel);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.saveFilesBasePanel);
            this.Icon = global::GPSaveConverter.Properties.Resources.Icon;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SaveFileConverterForm";
            this.Text = "Xbox Save File Converter";
            this.Load += new System.EventHandler(this.SaveFileConverterForm_Load);
            this.Shown += new System.EventHandler(this.SaveFileConverterForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.xboxFilesTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nonXboxFilesTable)).EndInit();
            this.saveFilesBasePanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.xboxFileBasePanel.ResumeLayout(false);
            this.xboxFileBasePanel.PerformLayout();
            this.moveControlPanel.ResumeLayout(false);
            this.packagesBasePanel.ResumeLayout(false);
            this.packagesBasePanel.PerformLayout();
            this.packagesScrollPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.packagesDataGridView)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.profilesBasePanel.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.nonXboxProfilePanel.ResumeLayout(false);
            this.nonXboxProfilePanel.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.fileTranslationPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox nonXboxProfileListBox;
        private System.Windows.Forms.Label packagesLabel;
        private System.Windows.Forms.Label nonXboxProfileLabel;
        private System.Windows.Forms.Label nonXboxFilesLabel;
        private System.Windows.Forms.Label xboxFileLabel;
        private System.Windows.Forms.DataGridView xboxFilesTable;
        private System.Windows.Forms.DataGridView nonXboxFilesTable;
        private System.Windows.Forms.TableLayoutPanel saveFilesBasePanel;
        private System.Windows.Forms.Panel packagesBasePanel;
        private System.Windows.Forms.Panel xboxFileBasePanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button moveSelectionToXboxButton;
        private System.Windows.Forms.Button moveAllToXboxButton;
        private System.Windows.Forms.Button moveSelectionFromXboxButton;
        private System.Windows.Forms.Button moveAllFromXboxButton;
        private System.Windows.Forms.ToolTip foldersToolTip;
        private System.Windows.Forms.Button viewXboxFilesButton;
        private System.Windows.Forms.Button viewNonXboxFileButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel infoStatusLabel;
        private System.Windows.Forms.DataGridView packagesDataGridView;
        private System.Windows.Forms.DataGridViewImageColumn GameIcon;
        private System.Windows.Forms.DataGridViewTextBoxColumn GameName;
        private System.Windows.Forms.Panel packagesScrollPanel;
        private System.Windows.Forms.Button promptNonXboxLocationButton;
        private System.Windows.Forms.TableLayoutPanel profilesBasePanel;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ListBox xboxProfileListBox;
        private System.Windows.Forms.Label xboxProfileLabel;
        private System.Windows.Forms.Panel nonXboxProfilePanel;
        private System.Windows.Forms.TableLayoutPanel moveControlPanel;
        private System.Windows.Forms.ToolTip buttonsToolTip;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
        private System.Windows.Forms.Panel fileTranslationPanel;
        private System.Windows.Forms.PropertyGrid fileTranslationPropertyGrid;
        private System.Windows.Forms.Button removeTranslationButton;
        private System.Windows.Forms.Button addTranslationButton;
        private System.Windows.Forms.ListBox fileTranslationListBox;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showFileTranslationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGameProfileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadGameProfileToolStripMenuItem;
    }
}

