using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmAddSchedule : Form
    {
        bool _isLoading = false;
        Dictionary<int, string> WeekDays = new Dictionary<int, string>();
        Dictionary<int, string> MonthDays = new Dictionary<int, string>();
        public delegate string AddScheduleDel(int ScheduleType, string StartTime, string EndTime, int Period, int Day);
        public AddScheduleDel AddScheduleHandler;
        int preValue;

        public frmAddSchedule()
        {
            InitializeComponent();
        }

        private void frmAddSchedule_Load(object sender, EventArgs e)
        {
            try
            {
                _isLoading = true;
                this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;

                Dictionary<int, string> _scheduleTypes = new Dictionary<int, string>();
                _scheduleTypes.Add(ScheduleType.DailyAt.GetHashCode(), "Daily at specific time");
                _scheduleTypes.Add(ScheduleType.DailyEvery.GetHashCode(), "Daily every specific period");
                _scheduleTypes.Add(ScheduleType.Weekly.GetHashCode(), "Weekly at specific day and time");
                _scheduleTypes.Add(ScheduleType.Monthly.GetHashCode(), "Monthly at specific day and time");

                WeekDays.Add(DayOfWeek.Sunday.GetHashCode(), DayOfWeek.Sunday.ToString());
                WeekDays.Add(DayOfWeek.Monday.GetHashCode(), DayOfWeek.Monday.ToString());
                WeekDays.Add(DayOfWeek.Tuesday.GetHashCode(), DayOfWeek.Tuesday.ToString());
                WeekDays.Add(DayOfWeek.Wednesday.GetHashCode(), DayOfWeek.Wednesday.ToString());
                WeekDays.Add(DayOfWeek.Thursday.GetHashCode(), DayOfWeek.Thursday.ToString());
                WeekDays.Add(DayOfWeek.Friday.GetHashCode(), DayOfWeek.Friday.ToString());
                WeekDays.Add(DayOfWeek.Saturday.GetHashCode(), DayOfWeek.Saturday.ToString());

                MonthDays.Add(1, "1");
                MonthDays.Add(2, "2");
                MonthDays.Add(3, "3");
                MonthDays.Add(4, "4");
                MonthDays.Add(5, "5");
                MonthDays.Add(6, "6");
                MonthDays.Add(7, "7");
                MonthDays.Add(8, "8");
                MonthDays.Add(9, "9");
                MonthDays.Add(10, "10");
                MonthDays.Add(11, "11");
                MonthDays.Add(12, "12");
                MonthDays.Add(13, "13");
                MonthDays.Add(14, "14");
                MonthDays.Add(15, "15");
                MonthDays.Add(16, "16");
                MonthDays.Add(17, "17");
                MonthDays.Add(18, "18");
                MonthDays.Add(19, "19");
                MonthDays.Add(20, "20");
                MonthDays.Add(21, "21");
                MonthDays.Add(22, "22");
                MonthDays.Add(23, "23");
                MonthDays.Add(24, "24");
                MonthDays.Add(25, "25");
                MonthDays.Add(26, "26");
                MonthDays.Add(27, "27");
                MonthDays.Add(28, "28");
                MonthDays.Add(31, "Last Day");

                cmbType.DataSource = new BindingSource(_scheduleTypes, null);
                cmbType.ValueMember = "Key";
                cmbType.DisplayMember = "Value";

                _isLoading = false;
                cmbType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_isLoading && cmbType.SelectedValue != null)
                {
                    ScheduleType type = (ScheduleType)(Convert.ToInt16(cmbType.SelectedValue));
                    switch (type)
                    {
                        case ScheduleType.DailyAt:
                            //Day
                            cmbDay.DataSource = null;
                            cmbDay.Enabled = false;
                            //Start
                            txtStartH.Text = "00";
                            txtStartM.Text = "00";
                            txtStartH.SelectAll();
                            txtStartH.Focus();
                            //End
                            txtEndH.ReadOnly = true;
                            txtEndH.TabStop = false;
                            txtEndH.Text = "";
                            txtEndM.ReadOnly = true;
                            txtEndM.TabStop = false;
                            txtEndM.Text = "";
                            //Period
                            txtPeriod.ReadOnly = true;
                            txtPeriod.TabStop = false;
                            txtPeriod.Text = "";
                            break;

                        case ScheduleType.DailyEvery:
                            //Day
                            cmbDay.DataSource = null;
                            cmbDay.Enabled = false;
                            //Start
                            txtStartH.Text = "00";
                            txtStartM.Text = "00";
                            txtStartH.SelectAll();
                            txtStartH.Focus();
                            //End
                            txtEndH.ReadOnly = false;
                            txtEndH.TabStop = true;
                            txtEndH.Text = "23";
                            txtEndM.ReadOnly = false;
                            txtEndM.TabStop = true;
                            txtEndM.Text = "59";
                            //Period
                            txtPeriod.ReadOnly = false;
                            txtPeriod.TabStop = true;
                            txtPeriod.Text = "60";
                            break;

                        case ScheduleType.Weekly:
                        case ScheduleType.Monthly:
                            //Day
                            cmbDay.DataSource = new BindingSource(type == ScheduleType.Weekly ? WeekDays : MonthDays, "");
                            cmbDay.DisplayMember = "Value";
                            cmbDay.ValueMember = "Key";
                            cmbDay.Enabled = true;
                            cmbDay.Focus();
                            //Start
                            txtStartH.Text = "00";
                            txtStartM.Text = "00";
                            txtStartH.SelectAll();
                            txtStartH.Focus();
                            //End
                            txtEndH.ReadOnly = true;
                            txtEndH.TabStop = false;
                            txtEndH.Text = "";
                            txtEndM.ReadOnly = true;
                            txtEndM.TabStop = false;
                            txtEndM.Text = "";
                            //Period
                            txtPeriod.ReadOnly = true;
                            txtPeriod.TabStop = false;
                            txtPeriod.Text = "";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (AddScheduleHandler != null)
                {
                    int period = 0;
                    int startH = 0;
                    int startM = 0;
                    int endH = 0;
                    int endM = 0;
                    int day = 0;
                    if (cmbDay.DataSource != null && cmbDay.SelectedValue != null)
                        int.TryParse(cmbDay.SelectedValue.ToString(), out day);
                    int.TryParse(txtPeriod.Text, out period);
                    int.TryParse(txtStartH.Text, out startH);
                    int.TryParse(txtStartM.Text, out startM);
                    int.TryParse(txtEndH.Text, out endH);
                    int.TryParse(txtEndM.Text, out endM);
                    System.Drawing.Color FontColor = System.Drawing.Color.Black;
                    lblResult.Text = AddScheduleHandler(Convert.ToInt16(cmbType.SelectedValue), startH.ToString().PadLeft(2, '0') + startM.ToString().PadLeft(2, '0'), endH.ToString().PadLeft(2, '0') + endM.ToString().PadLeft(2, '0'), period * 60, day);
                    if (lblResult.Text == "Added ..")
                        lblResult.ForeColor = System.Drawing.Color.Green;
                    else
                        lblResult.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtStartH_KeyPress(object sender, KeyPressEventArgs e)
        {
            ValidateNumericTextBox(sender, e, 2);
        }


        private void txtStartM_KeyPress(object sender, KeyPressEventArgs e)
        {
            ValidateNumericTextBox(sender, e, 2);
        }

        private void txtEndH_KeyPress(object sender, KeyPressEventArgs e)
        {
            ValidateNumericTextBox(sender, e, 2);
        }

        private void txtEndM_KeyPress(object sender, KeyPressEventArgs e)
        {
            ValidateNumericTextBox(sender, e, 2);
        }

        private void txtPeriod_KeyPress(object sender, KeyPressEventArgs e)
        {
            ValidateNumericTextBox(sender, e, 3);
        }

        private void txtStartH_TextChanged(object sender, EventArgs e)
        {
            ValidateMaxValue(sender, 23);
        }

        private void txtStartM_TextChanged(object sender, EventArgs e)
        {
            ValidateMaxValue(sender, 59);
        }

        private void txtEndH_TextChanged(object sender, EventArgs e)
        {
            ValidateMaxValue(sender, 23);
        }

        private void txtEndM_TextChanged(object sender, EventArgs e)
        {
            ValidateMaxValue(sender, 59);
        }

        private void txtPeriod_TextChanged(object sender, EventArgs e)
        {
            ValidateMaxValue(sender, 719);
        }

        private void ValidateNumericTextBox(object sender, KeyPressEventArgs e, int Length)
        {
            try
            {
                char c = e.KeyChar;
                Keys key = (Keys)c;
                string preStrValue = ((TextBox)sender).Text;
                preValue = 0;
                int.TryParse(preStrValue, out preValue);
                if (char.IsDigit(c) || key == Keys.Back)// && preStrValue.Length < Length)
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
        private void ValidateMaxValue(object sender, int max)
        {
            try
            {
                int currentValue = -1;
                int.TryParse(((TextBox)sender).Text, out currentValue);
                if (currentValue == -1 || currentValue > max)
                {
                    ((TextBox)sender).Text = preValue.ToString();
                    ((TextBox)sender).SelectAll();
                    ((TextBox)sender).Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }
    }
}