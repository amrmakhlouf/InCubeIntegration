using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace InCubeLibrary
{
    public partial class frmSelectSheet : Form
    {
        public string SelectedSheet = "";
        public frmSelectSheet(DataTable dtSheets)
        {
            InitializeComponent();
            foreach (DataRow dr in dtSheets.Rows)
                listBox1.Items.Add(dr["TABLE_NAME"].ToString());
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                int index = this.listBox1.IndexFromPoint(e.Location);
                if (index != System.Windows.Forms.ListBox.NoMatches)
                {
                    SelectedSheet = listBox1.Items[index].ToString();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
            }
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }
    }
}
