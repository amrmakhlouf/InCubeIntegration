namespace InCubeIntegration_UI
{
    partial class frmSchedulesManagement
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
            this.lblTaskName = new System.Windows.Forms.Label();
            this.dtpFromDate = new System.Windows.Forms.DateTimePicker();
            this.cmbPriority = new System.Windows.Forms.ComboBox();
            this.lblFromDate = new System.Windows.Forms.Label();
            this.lblToDate = new System.Windows.Forms.Label();
            this.dtpToDate = new System.Windows.Forms.DateTimePicker();
            this.lblPriority = new System.Windows.Forms.Label();
            this.btnAddTask = new System.Windows.Forms.Button();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.dgvTasks = new System.Windows.Forms.DataGridView();
            this.TaskID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.From = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.To = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.EndDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPriority = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Organization = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StatusID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PriorityID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OrganizationID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblOrganization = new System.Windows.Forms.Label();
            this.cmbOrganization = new System.Windows.Forms.ComboBox();
            this.dgvActions = new System.Windows.Forms.DataGridView();
            this.ActionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTaskID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_ActionType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FieldID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Sequence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Action = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnAddAction = new System.Windows.Forms.Button();
            this.gbActions = new System.Windows.Forms.GroupBox();
            this.picDown = new System.Windows.Forms.PictureBox();
            this.picUp = new System.Windows.Forms.PictureBox();
            this.gbSchedules = new System.Windows.Forms.GroupBox();
            this.dgvSchedules = new System.Windows.Forms.DataGridView();
            this.Schedule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStartTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEndTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPeriod = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDay = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colActionTaskID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColScheduleType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnAddSchedule = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnStartService = new System.Windows.Forms.Button();
            this.btnStopService = new System.Windows.Forms.Button();
            this.btnRestartService = new System.Windows.Forms.Button();
            this.gbServiceInfo = new System.Windows.Forms.GroupBox();
            this.btnInstall = new System.Windows.Forms.Button();
            this.lblServiceStatus = new System.Windows.Forms.Label();
            this.lblServiceMachine = new System.Windows.Forms.Label();
            this.lblServiceName = new System.Windows.Forms.Label();
            this.lblCaptionServiceStatus = new System.Windows.Forms.Label();
            this.lblCaptionServiceMachine = new System.Windows.Forms.Label();
            this.lblCaptionServiceName = new System.Windows.Forms.Label();
            this.gbTasks = new System.Windows.Forms.GroupBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTasks)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActions)).BeginInit();
            this.gbActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).BeginInit();
            this.gbSchedules.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchedules)).BeginInit();
            this.gbServiceInfo.SuspendLayout();
            this.gbTasks.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTaskName
            // 
            this.lblTaskName.AutoSize = true;
            this.lblTaskName.Location = new System.Drawing.Point(9, 249);
            this.lblTaskName.Name = "lblTaskName";
            this.lblTaskName.Size = new System.Drawing.Size(35, 13);
            this.lblTaskName.TabIndex = 0;
            this.lblTaskName.Text = "Name";
            // 
            // dtpFromDate
            // 
            this.dtpFromDate.Enabled = false;
            this.dtpFromDate.Location = new System.Drawing.Point(56, 273);
            this.dtpFromDate.Name = "dtpFromDate";
            this.dtpFromDate.Size = new System.Drawing.Size(200, 20);
            this.dtpFromDate.TabIndex = 2;
            this.dtpFromDate.ValueChanged += new System.EventHandler(this.dtpFromDate_ValueChanged);
            // 
            // cmbPriority
            // 
            this.cmbPriority.Enabled = false;
            this.cmbPriority.FormattingEnabled = true;
            this.cmbPriority.Location = new System.Drawing.Point(56, 304);
            this.cmbPriority.Name = "cmbPriority";
            this.cmbPriority.Size = new System.Drawing.Size(121, 21);
            this.cmbPriority.TabIndex = 3;
            // 
            // lblFromDate
            // 
            this.lblFromDate.AutoSize = true;
            this.lblFromDate.Location = new System.Drawing.Point(9, 279);
            this.lblFromDate.Name = "lblFromDate";
            this.lblFromDate.Size = new System.Drawing.Size(30, 13);
            this.lblFromDate.TabIndex = 5;
            this.lblFromDate.Text = "From";
            // 
            // lblToDate
            // 
            this.lblToDate.AutoSize = true;
            this.lblToDate.Location = new System.Drawing.Point(272, 279);
            this.lblToDate.Name = "lblToDate";
            this.lblToDate.Size = new System.Drawing.Size(20, 13);
            this.lblToDate.TabIndex = 7;
            this.lblToDate.Text = "To";
            // 
            // dtpToDate
            // 
            this.dtpToDate.Enabled = false;
            this.dtpToDate.Location = new System.Drawing.Point(312, 273);
            this.dtpToDate.Name = "dtpToDate";
            this.dtpToDate.Size = new System.Drawing.Size(200, 20);
            this.dtpToDate.TabIndex = 6;
            // 
            // lblPriority
            // 
            this.lblPriority.AutoSize = true;
            this.lblPriority.Location = new System.Drawing.Point(9, 307);
            this.lblPriority.Name = "lblPriority";
            this.lblPriority.Size = new System.Drawing.Size(38, 13);
            this.lblPriority.TabIndex = 8;
            this.lblPriority.Text = "Priority";
            this.toolTip1.SetToolTip(this.lblPriority, "If task time intersects with other tasks then the highest priority task will run " +
        "first");
            // 
            // btnAddTask
            // 
            this.btnAddTask.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddTask.ForeColor = System.Drawing.Color.ForestGreen;
            this.btnAddTask.Location = new System.Drawing.Point(540, 14);
            this.btnAddTask.Name = "btnAddTask";
            this.btnAddTask.Size = new System.Drawing.Size(123, 26);
            this.btnAddTask.TabIndex = 10;
            this.btnAddTask.Text = "Add New Task";
            this.btnAddTask.UseVisualStyleBackColor = true;
            this.btnAddTask.Click += new System.EventHandler(this.btnAddTask_Click);
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.BackColor = System.Drawing.Color.Gainsboro;
            this.btnSaveChanges.Enabled = false;
            this.btnSaveChanges.Location = new System.Drawing.Point(224, 537);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(94, 28);
            this.btnSaveChanges.TabIndex = 11;
            this.btnSaveChanges.Text = "Save Changes";
            this.btnSaveChanges.UseVisualStyleBackColor = false;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // dgvTasks
            // 
            this.dgvTasks.AllowUserToAddRows = false;
            this.dgvTasks.AllowUserToDeleteRows = false;
            this.dgvTasks.AllowUserToResizeRows = false;
            this.dgvTasks.BackgroundColor = System.Drawing.Color.White;
            this.dgvTasks.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTasks.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.TaskID,
            this.From,
            this.To,
            this.Description,
            this.StartDate,
            this.EndDate,
            this.Status,
            this.colPriority,
            this.Organization,
            this.StatusID,
            this.PriorityID,
            this.OrganizationID});
            this.dgvTasks.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvTasks.Location = new System.Drawing.Point(9, 46);
            this.dgvTasks.MultiSelect = false;
            this.dgvTasks.Name = "dgvTasks";
            this.dgvTasks.RowHeadersVisible = false;
            this.dgvTasks.Size = new System.Drawing.Size(654, 195);
            this.dgvTasks.TabIndex = 19;
            this.dgvTasks.SelectionChanged += new System.EventHandler(this.dgvTasks_SelectionChanged);
            this.dgvTasks.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dgvTasks_MouseClick);
            // 
            // TaskID
            // 
            this.TaskID.DataPropertyName = "TaskID";
            this.TaskID.HeaderText = "TaskID";
            this.TaskID.Name = "TaskID";
            this.TaskID.ReadOnly = true;
            this.TaskID.Visible = false;
            // 
            // From
            // 
            this.From.DataPropertyName = "From";
            this.From.HeaderText = "From";
            this.From.Name = "From";
            this.From.ReadOnly = true;
            this.From.Visible = false;
            // 
            // To
            // 
            this.To.DataPropertyName = "To";
            this.To.HeaderText = "To";
            this.To.Name = "To";
            this.To.ReadOnly = true;
            this.To.Visible = false;
            // 
            // Description
            // 
            this.Description.DataPropertyName = "Name";
            this.Description.HeaderText = "Description";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 280;
            // 
            // StartDate
            // 
            this.StartDate.DataPropertyName = "StartDate";
            this.StartDate.HeaderText = "Start Date";
            this.StartDate.Name = "StartDate";
            this.StartDate.ReadOnly = true;
            this.StartDate.Width = 80;
            // 
            // EndDate
            // 
            this.EndDate.DataPropertyName = "EndDate";
            this.EndDate.HeaderText = "End Date";
            this.EndDate.Name = "EndDate";
            this.EndDate.ReadOnly = true;
            this.EndDate.Width = 80;
            // 
            // Status
            // 
            this.Status.DataPropertyName = "Status";
            this.Status.HeaderText = "Status";
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            this.Status.Width = 50;
            // 
            // colPriority
            // 
            this.colPriority.DataPropertyName = "Priority";
            this.colPriority.HeaderText = "Priority";
            this.colPriority.Name = "colPriority";
            this.colPriority.ReadOnly = true;
            this.colPriority.Width = 50;
            // 
            // Organization
            // 
            this.Organization.DataPropertyName = "Organization";
            this.Organization.HeaderText = "Organization";
            this.Organization.Name = "Organization";
            this.Organization.ReadOnly = true;
            this.Organization.Width = 70;
            // 
            // StatusID
            // 
            this.StatusID.DataPropertyName = "StatusID";
            this.StatusID.HeaderText = "StatusID";
            this.StatusID.Name = "StatusID";
            this.StatusID.ReadOnly = true;
            this.StatusID.Visible = false;
            // 
            // PriorityID
            // 
            this.PriorityID.DataPropertyName = "PriorityID";
            this.PriorityID.HeaderText = "PriorityID";
            this.PriorityID.Name = "PriorityID";
            this.PriorityID.ReadOnly = true;
            this.PriorityID.Visible = false;
            // 
            // OrganizationID
            // 
            this.OrganizationID.DataPropertyName = "OrganizationID";
            this.OrganizationID.HeaderText = "OrganizationID";
            this.OrganizationID.Name = "OrganizationID";
            this.OrganizationID.ReadOnly = true;
            this.OrganizationID.Visible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.Gainsboro;
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(352, 537);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 28);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(56, 246);
            this.txtName.Name = "txtName";
            this.txtName.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(607, 20);
            this.txtName.TabIndex = 21;
            // 
            // lblOrganization
            // 
            this.lblOrganization.AutoSize = true;
            this.lblOrganization.Location = new System.Drawing.Point(208, 307);
            this.lblOrganization.Name = "lblOrganization";
            this.lblOrganization.Size = new System.Drawing.Size(66, 13);
            this.lblOrganization.TabIndex = 23;
            this.lblOrganization.Text = "Organization";
            // 
            // cmbOrganization
            // 
            this.cmbOrganization.Enabled = false;
            this.cmbOrganization.FormattingEnabled = true;
            this.cmbOrganization.Location = new System.Drawing.Point(282, 304);
            this.cmbOrganization.Name = "cmbOrganization";
            this.cmbOrganization.Size = new System.Drawing.Size(121, 21);
            this.cmbOrganization.TabIndex = 22;
            // 
            // dgvActions
            // 
            this.dgvActions.AllowUserToAddRows = false;
            this.dgvActions.AllowUserToDeleteRows = false;
            this.dgvActions.AllowUserToResizeRows = false;
            this.dgvActions.BackgroundColor = System.Drawing.Color.White;
            this.dgvActions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvActions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ActionID,
            this.colTaskID,
            this.col_ActionType,
            this.FieldID,
            this.Sequence,
            this.Action});
            this.dgvActions.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvActions.Location = new System.Drawing.Point(6, 50);
            this.dgvActions.MultiSelect = false;
            this.dgvActions.Name = "dgvActions";
            this.dgvActions.RowHeadersVisible = false;
            this.dgvActions.Size = new System.Drawing.Size(259, 134);
            this.dgvActions.TabIndex = 24;
            this.dgvActions.SelectionChanged += new System.EventHandler(this.dgvActions_SelectionChanged);
            this.dgvActions.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dgvActions_MouseClick);
            // 
            // ActionID
            // 
            this.ActionID.DataPropertyName = "ActionID";
            this.ActionID.HeaderText = "ActionID";
            this.ActionID.Name = "ActionID";
            this.ActionID.ReadOnly = true;
            this.ActionID.Visible = false;
            // 
            // colTaskID
            // 
            this.colTaskID.DataPropertyName = "TaskID";
            this.colTaskID.HeaderText = "TaskID";
            this.colTaskID.Name = "colTaskID";
            this.colTaskID.ReadOnly = true;
            this.colTaskID.Visible = false;
            // 
            // col_ActionType
            // 
            this.col_ActionType.DataPropertyName = "ActionType";
            this.col_ActionType.HeaderText = "ActionType";
            this.col_ActionType.Name = "col_ActionType";
            this.col_ActionType.ReadOnly = true;
            this.col_ActionType.Visible = false;
            // 
            // FieldID
            // 
            this.FieldID.DataPropertyName = "FieldID";
            this.FieldID.HeaderText = "FieldID";
            this.FieldID.Name = "FieldID";
            this.FieldID.ReadOnly = true;
            this.FieldID.Visible = false;
            // 
            // Sequence
            // 
            this.Sequence.DataPropertyName = "Sequence";
            this.Sequence.HeaderText = "Sq.";
            this.Sequence.Name = "Sequence";
            this.Sequence.ReadOnly = true;
            this.Sequence.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Sequence.Width = 30;
            // 
            // Action
            // 
            this.Action.DataPropertyName = "Action";
            this.Action.FillWeight = 250F;
            this.Action.HeaderText = "Action";
            this.Action.Name = "Action";
            this.Action.ReadOnly = true;
            this.Action.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Action.Width = 210;
            // 
            // btnAddAction
            // 
            this.btnAddAction.Enabled = false;
            this.btnAddAction.Location = new System.Drawing.Point(6, 16);
            this.btnAddAction.Name = "btnAddAction";
            this.btnAddAction.Size = new System.Drawing.Size(85, 28);
            this.btnAddAction.TabIndex = 25;
            this.btnAddAction.Text = "Add Action";
            this.btnAddAction.UseVisualStyleBackColor = true;
            this.btnAddAction.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnAddAction_MouseClick);
            // 
            // gbActions
            // 
            this.gbActions.Controls.Add(this.picDown);
            this.gbActions.Controls.Add(this.picUp);
            this.gbActions.Controls.Add(this.dgvActions);
            this.gbActions.Controls.Add(this.btnAddAction);
            this.gbActions.Location = new System.Drawing.Point(6, 340);
            this.gbActions.Name = "gbActions";
            this.gbActions.Size = new System.Drawing.Size(330, 186);
            this.gbActions.TabIndex = 29;
            this.gbActions.TabStop = false;
            this.gbActions.Text = "Actions";
            // 
            // picDown
            // 
            this.picDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picDown.Enabled = false;
            this.picDown.Image = global::InCubeIntegration_UI.Properties.Resources.down;
            this.picDown.Location = new System.Drawing.Point(272, 128);
            this.picDown.Name = "picDown";
            this.picDown.Size = new System.Drawing.Size(40, 43);
            this.picDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picDown.TabIndex = 32;
            this.picDown.TabStop = false;
            this.toolTip1.SetToolTip(this.picDown, "Sequence Down");
            this.picDown.Click += new System.EventHandler(this.picDown_Click);
            // 
            // picUp
            // 
            this.picUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picUp.Enabled = false;
            this.picUp.Image = global::InCubeIntegration_UI.Properties.Resources.up;
            this.picUp.Location = new System.Drawing.Point(272, 82);
            this.picUp.Name = "picUp";
            this.picUp.Size = new System.Drawing.Size(40, 43);
            this.picUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picUp.TabIndex = 31;
            this.picUp.TabStop = false;
            this.toolTip1.SetToolTip(this.picUp, "Sequence Up");
            this.picUp.Click += new System.EventHandler(this.picUp_Click);
            // 
            // gbSchedules
            // 
            this.gbSchedules.Controls.Add(this.dgvSchedules);
            this.gbSchedules.Controls.Add(this.btnAddSchedule);
            this.gbSchedules.Location = new System.Drawing.Point(339, 340);
            this.gbSchedules.Name = "gbSchedules";
            this.gbSchedules.Size = new System.Drawing.Size(330, 186);
            this.gbSchedules.TabIndex = 33;
            this.gbSchedules.TabStop = false;
            this.gbSchedules.Text = "Schedules";
            // 
            // dgvSchedules
            // 
            this.dgvSchedules.AllowUserToAddRows = false;
            this.dgvSchedules.AllowUserToDeleteRows = false;
            this.dgvSchedules.AllowUserToResizeRows = false;
            this.dgvSchedules.BackgroundColor = System.Drawing.Color.White;
            this.dgvSchedules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSchedules.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Schedule,
            this.colStartTime,
            this.colEndTime,
            this.colPeriod,
            this.colDay,
            this.colActionTaskID,
            this.ColScheduleType});
            this.dgvSchedules.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvSchedules.Location = new System.Drawing.Point(6, 50);
            this.dgvSchedules.MultiSelect = false;
            this.dgvSchedules.Name = "dgvSchedules";
            this.dgvSchedules.RowHeadersVisible = false;
            this.dgvSchedules.Size = new System.Drawing.Size(315, 134);
            this.dgvSchedules.TabIndex = 24;
            this.dgvSchedules.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dgvSchedules_MouseClick);
            // 
            // Schedule
            // 
            this.Schedule.DataPropertyName = "Schedule";
            this.Schedule.HeaderText = "Schedule";
            this.Schedule.Name = "Schedule";
            this.Schedule.ReadOnly = true;
            this.Schedule.Width = 300;
            // 
            // colStartTime
            // 
            this.colStartTime.DataPropertyName = "StartTime";
            this.colStartTime.HeaderText = "StartTime";
            this.colStartTime.Name = "colStartTime";
            this.colStartTime.Visible = false;
            // 
            // colEndTime
            // 
            this.colEndTime.DataPropertyName = "EndTime";
            this.colEndTime.HeaderText = "EndTime";
            this.colEndTime.Name = "colEndTime";
            this.colEndTime.Visible = false;
            // 
            // colPeriod
            // 
            this.colPeriod.DataPropertyName = "Period";
            this.colPeriod.HeaderText = "Period";
            this.colPeriod.Name = "colPeriod";
            this.colPeriod.Visible = false;
            // 
            // colDay
            // 
            this.colDay.DataPropertyName = "Day";
            this.colDay.HeaderText = "Day";
            this.colDay.Name = "colDay";
            this.colDay.Visible = false;
            // 
            // colActionTaskID
            // 
            this.colActionTaskID.DataPropertyName = "TaskID";
            this.colActionTaskID.HeaderText = "TaskID";
            this.colActionTaskID.Name = "colActionTaskID";
            this.colActionTaskID.Visible = false;
            // 
            // ColScheduleType
            // 
            this.ColScheduleType.DataPropertyName = "ScheduleType";
            this.ColScheduleType.HeaderText = "ScheduleType";
            this.ColScheduleType.Name = "ColScheduleType";
            this.ColScheduleType.Visible = false;
            // 
            // btnAddSchedule
            // 
            this.btnAddSchedule.Enabled = false;
            this.btnAddSchedule.Location = new System.Drawing.Point(6, 16);
            this.btnAddSchedule.Name = "btnAddSchedule";
            this.btnAddSchedule.Size = new System.Drawing.Size(85, 28);
            this.btnAddSchedule.TabIndex = 25;
            this.btnAddSchedule.Text = "Add Schedule";
            this.btnAddSchedule.UseVisualStyleBackColor = true;
            this.btnAddSchedule.Click += new System.EventHandler(this.btnAddSchedule_Click);
            // 
            // btnStartService
            // 
            this.btnStartService.Enabled = false;
            this.btnStartService.Location = new System.Drawing.Point(470, 44);
            this.btnStartService.Name = "btnStartService";
            this.btnStartService.Size = new System.Drawing.Size(58, 26);
            this.btnStartService.TabIndex = 36;
            this.btnStartService.Text = "Start";
            this.btnStartService.UseVisualStyleBackColor = true;
            this.btnStartService.Visible = false;
            this.btnStartService.Click += new System.EventHandler(this.btnStartService_Click);
            // 
            // btnStopService
            // 
            this.btnStopService.Enabled = false;
            this.btnStopService.Location = new System.Drawing.Point(534, 44);
            this.btnStopService.Name = "btnStopService";
            this.btnStopService.Size = new System.Drawing.Size(58, 26);
            this.btnStopService.TabIndex = 37;
            this.btnStopService.Text = "Stop";
            this.btnStopService.UseVisualStyleBackColor = true;
            this.btnStopService.Visible = false;
            // 
            // btnRestartService
            // 
            this.btnRestartService.Enabled = false;
            this.btnRestartService.Location = new System.Drawing.Point(598, 44);
            this.btnRestartService.Name = "btnRestartService";
            this.btnRestartService.Size = new System.Drawing.Size(58, 26);
            this.btnRestartService.TabIndex = 38;
            this.btnRestartService.Text = "Restart";
            this.btnRestartService.UseVisualStyleBackColor = true;
            this.btnRestartService.Visible = false;
            // 
            // gbServiceInfo
            // 
            this.gbServiceInfo.Controls.Add(this.btnInstall);
            this.gbServiceInfo.Controls.Add(this.lblServiceStatus);
            this.gbServiceInfo.Controls.Add(this.lblServiceMachine);
            this.gbServiceInfo.Controls.Add(this.lblServiceName);
            this.gbServiceInfo.Controls.Add(this.lblCaptionServiceStatus);
            this.gbServiceInfo.Controls.Add(this.lblCaptionServiceMachine);
            this.gbServiceInfo.Controls.Add(this.lblCaptionServiceName);
            this.gbServiceInfo.Controls.Add(this.btnStartService);
            this.gbServiceInfo.Controls.Add(this.btnRestartService);
            this.gbServiceInfo.Controls.Add(this.btnStopService);
            this.gbServiceInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbServiceInfo.Location = new System.Drawing.Point(12, 5);
            this.gbServiceInfo.Name = "gbServiceInfo";
            this.gbServiceInfo.Size = new System.Drawing.Size(672, 78);
            this.gbServiceInfo.TabIndex = 40;
            this.gbServiceInfo.TabStop = false;
            this.gbServiceInfo.Text = "Service Info";
            // 
            // btnInstall
            // 
            this.btnInstall.Enabled = false;
            this.btnInstall.Location = new System.Drawing.Point(388, 44);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(58, 26);
            this.btnInstall.TabIndex = 46;
            this.btnInstall.Text = "Install";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Visible = false;
            // 
            // lblServiceStatus
            // 
            this.lblServiceStatus.Location = new System.Drawing.Point(467, 21);
            this.lblServiceStatus.Name = "lblServiceStatus";
            this.lblServiceStatus.Size = new System.Drawing.Size(100, 23);
            this.lblServiceStatus.TabIndex = 45;
            this.lblServiceStatus.Text = ":  Satus";
            // 
            // lblServiceMachine
            // 
            this.lblServiceMachine.Location = new System.Drawing.Point(100, 51);
            this.lblServiceMachine.Name = "lblServiceMachine";
            this.lblServiceMachine.Size = new System.Drawing.Size(239, 25);
            this.lblServiceMachine.TabIndex = 44;
            this.lblServiceMachine.Text = ":  Machine";
            // 
            // lblServiceName
            // 
            this.lblServiceName.Location = new System.Drawing.Point(100, 21);
            this.lblServiceName.Name = "lblServiceName";
            this.lblServiceName.Size = new System.Drawing.Size(239, 25);
            this.lblServiceName.TabIndex = 43;
            this.lblServiceName.Text = ":  Name ";
            // 
            // lblCaptionServiceStatus
            // 
            this.lblCaptionServiceStatus.AutoSize = true;
            this.lblCaptionServiceStatus.Location = new System.Drawing.Point(385, 21);
            this.lblCaptionServiceStatus.Name = "lblCaptionServiceStatus";
            this.lblCaptionServiceStatus.Size = new System.Drawing.Size(76, 13);
            this.lblCaptionServiceStatus.TabIndex = 42;
            this.lblCaptionServiceStatus.Text = "Service Status";
            // 
            // lblCaptionServiceMachine
            // 
            this.lblCaptionServiceMachine.AutoSize = true;
            this.lblCaptionServiceMachine.Location = new System.Drawing.Point(6, 51);
            this.lblCaptionServiceMachine.Name = "lblCaptionServiceMachine";
            this.lblCaptionServiceMachine.Size = new System.Drawing.Size(87, 13);
            this.lblCaptionServiceMachine.TabIndex = 41;
            this.lblCaptionServiceMachine.Text = "Service Machine";
            // 
            // lblCaptionServiceName
            // 
            this.lblCaptionServiceName.AutoSize = true;
            this.lblCaptionServiceName.Location = new System.Drawing.Point(6, 21);
            this.lblCaptionServiceName.Name = "lblCaptionServiceName";
            this.lblCaptionServiceName.Size = new System.Drawing.Size(77, 13);
            this.lblCaptionServiceName.TabIndex = 40;
            this.lblCaptionServiceName.Text = "Service Name ";
            // 
            // gbTasks
            // 
            this.gbTasks.Controls.Add(this.dgvTasks);
            this.gbTasks.Controls.Add(this.lblTaskName);
            this.gbTasks.Controls.Add(this.gbSchedules);
            this.gbTasks.Controls.Add(this.dtpFromDate);
            this.gbTasks.Controls.Add(this.gbActions);
            this.gbTasks.Controls.Add(this.cmbPriority);
            this.gbTasks.Controls.Add(this.lblOrganization);
            this.gbTasks.Controls.Add(this.lblFromDate);
            this.gbTasks.Controls.Add(this.cmbOrganization);
            this.gbTasks.Controls.Add(this.dtpToDate);
            this.gbTasks.Controls.Add(this.txtName);
            this.gbTasks.Controls.Add(this.lblToDate);
            this.gbTasks.Controls.Add(this.btnCancel);
            this.gbTasks.Controls.Add(this.lblPriority);
            this.gbTasks.Controls.Add(this.btnAddTask);
            this.gbTasks.Controls.Add(this.btnSaveChanges);
            this.gbTasks.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbTasks.Location = new System.Drawing.Point(12, 89);
            this.gbTasks.Name = "gbTasks";
            this.gbTasks.Size = new System.Drawing.Size(674, 574);
            this.gbTasks.TabIndex = 41;
            this.gbTasks.TabStop = false;
            this.gbTasks.Text = "Tasks Management";
            // 
            // frmSchedulesManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(194)))), ((int)(((byte)(217)))), ((int)(((byte)(247)))));
            this.ClientSize = new System.Drawing.Size(694, 664);
            this.Controls.Add(this.gbTasks);
            this.Controls.Add(this.gbServiceInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmSchedulesManagement";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Schedules Management";
            this.Load += new System.EventHandler(this.frmSchedules_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTasks)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActions)).EndInit();
            this.gbActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picUp)).EndInit();
            this.gbSchedules.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSchedules)).EndInit();
            this.gbServiceInfo.ResumeLayout(false);
            this.gbServiceInfo.PerformLayout();
            this.gbTasks.ResumeLayout(false);
            this.gbTasks.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTaskName;
        private System.Windows.Forms.DateTimePicker dtpFromDate;
        private System.Windows.Forms.ComboBox cmbPriority;
        private System.Windows.Forms.Label lblFromDate;
        private System.Windows.Forms.Label lblToDate;
        private System.Windows.Forms.DateTimePicker dtpToDate;
        private System.Windows.Forms.Label lblPriority;
        private System.Windows.Forms.Button btnAddTask;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.DataGridView dgvTasks;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblOrganization;
        private System.Windows.Forms.ComboBox cmbOrganization;
        private System.Windows.Forms.DataGridView dgvActions;
        private System.Windows.Forms.Button btnAddAction;
        private System.Windows.Forms.GroupBox gbActions;
        private System.Windows.Forms.PictureBox picUp;
        private System.Windows.Forms.PictureBox picDown;
        private System.Windows.Forms.GroupBox gbSchedules;
        private System.Windows.Forms.DataGridView dgvSchedules;
        private System.Windows.Forms.Button btnAddSchedule;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DataGridViewTextBoxColumn TaskID;
        private System.Windows.Forms.DataGridViewTextBoxColumn From;
        private System.Windows.Forms.DataGridViewTextBoxColumn To;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn EndDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn Status;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPriority;
        private System.Windows.Forms.DataGridViewTextBoxColumn Organization;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusID;
        private System.Windows.Forms.DataGridViewTextBoxColumn PriorityID;
        private System.Windows.Forms.DataGridViewTextBoxColumn OrganizationID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ActionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTaskID;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_ActionType;
        private System.Windows.Forms.DataGridViewTextBoxColumn FieldID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Sequence;
        private System.Windows.Forms.DataGridViewTextBoxColumn Action;
        private System.Windows.Forms.DataGridViewTextBoxColumn Schedule;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStartTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEndTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPeriod;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDay;
        private System.Windows.Forms.DataGridViewTextBoxColumn colActionTaskID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColScheduleType;
        private System.Windows.Forms.Button btnStartService;
        private System.Windows.Forms.Button btnStopService;
        private System.Windows.Forms.Button btnRestartService;
        private System.Windows.Forms.GroupBox gbServiceInfo;
        private System.Windows.Forms.Label lblCaptionServiceStatus;
        private System.Windows.Forms.Label lblCaptionServiceMachine;
        private System.Windows.Forms.Label lblCaptionServiceName;
        private System.Windows.Forms.GroupBox gbTasks;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Label lblServiceName;
        private System.Windows.Forms.Label lblServiceMachine;
        private System.Windows.Forms.Label lblServiceStatus;
        private System.Windows.Forms.Button btnInstall;
    }
}