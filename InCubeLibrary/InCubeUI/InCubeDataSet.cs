using System;
using System.Data;
using System.Drawing;
//using System.Windows.Forms;
//using DevComponents.DotNetBar;
//using DevComponents.DotNetBar.Controls;
using System.ComponentModel;
using System.Text.RegularExpressions;
//using Microsoft.Reporting.WinForms;
using System.Collections.Generic;
namespace InCubeLibrary
{
    class InCubeDataSet
    {

        private string column1;
        private string column2;
        private string column3;
        private string column4;
        private string column5;
        private string column6;
        private string column7;
        private string column8;
        private string column9;
        private string column10;
        private string column11;
        private string column12;
        private string column13;
        private string column14;
        private string column15;
        public string Column1
        {
            get
            {
                return column1;
            }
            set
            {
                column1 = value;
            }
        }
        public string Column2
        {
            get
            {
                return column2;
            }
            set
            {
                column2 = value;
            }
        }
        public string Column3
        {
            get
            {
                return column3;
            }
            set
            {
                column3 = value;
            }
        }
        public string Column4
        {
            get
            {
                return column4;
            }
            set
            {
                column4 = value;
            }
        }
        public string Column5
        {
            get
            {
                return column5;
            }
            set
            {
                column5 = value;
            }
        }
        public string Column6
        {
            get
            {
                return column6;
            }
            set
            {
                column6 = value;
            }
        }

        public string Column7
        {
            get
            {
                return column7;
            }
            set
            {
                column7 = value;
            }
        }
        public string Column8
        {
            get
            {
                return column8;
            }
            set
            {
                column8 = value;
            }
        }
        public string Column9
        {
            get
            {
                return column9;
            }
            set
            {
                column9 = value;
            }
        }
        public string Column10
        {
            get
            {
                return column10;
            }
            set
            {
                column10 = value;
            }
        }
        public string Column11
        {
            get
            {
                return column11;
            }
            set
            {
                column11 = value;
            }
        }
        public string Column12
        {
            get
            {
                return column12;
            }
            set
            {
                column12 = value;
            }
        }
        public string Column13
        {
            get
            {
                return column13;
            }
            set
            {
                column13 = value;
            }
        }
        public string Column14
        {
            get
            {
                return column14;
            }
            set
            {
                column14 = value;
            }
        }
        public string Column15
        {
            get
            {
                return column15;
            }
            set
            {
                column15 = value;
            }
        }
        public static List<InCubeDataSet> GetTable2Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable3Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable4Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable4VColumns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = Convert.ToDateTime(table.Rows[i][3].ToString()).ToShortDateString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable5Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable6Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                c.Column6 = table.Rows[i][5].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable7Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                c.Column6 = table.Rows[i][5].ToString();
                c.Column7 = table.Rows[i][6].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable8Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                c.Column6 = table.Rows[i][5].ToString();
                c.Column7 = table.Rows[i][6].ToString();
                c.Column8 = table.Rows[i][7].ToString();
                list.Add(c);
            }
            return list;
        }
        public static List<InCubeDataSet> GetTable9Columns(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                InCubeDataSet c = new InCubeDataSet();
                c.Column1 = table.Rows[i][0].ToString();
                c.Column2 = table.Rows[i][1].ToString();
                c.Column3 = table.Rows[i][2].ToString();
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                c.Column6 = table.Rows[i][5].ToString();
                c.Column7 = table.Rows[i][6].ToString();
                c.Column8 = table.Rows[i][7].ToString();
                c.Column9 = table.Rows[i][8].ToString();
                list.Add(c);
            }
            return list;
        }

        public static List<InCubeDataSet> GetTable9Columns3NotRepeated(DataTable table)
        {
            List<InCubeDataSet> list = new List<InCubeDataSet>();

            InCubeDataSet cOld = new InCubeDataSet();
            for (int i = 0; i < table.Rows.Count; i++)
            {

                InCubeDataSet c = new InCubeDataSet();
                if (i > 0)
                {
                    if (table.Rows[i][0].ToString() == cOld.Column1)
                        c.Column1 = "";
                    else
                        c.Column1 = table.Rows[i][0].ToString();

                    if (table.Rows[i][1].ToString() == cOld.Column2)
                        c.Column2 = "";

                    else
                        c.Column2 = table.Rows[i][1].ToString();
                    if (table.Rows[i][2].ToString() == cOld.Column3)
                        c.Column3 = "";

                    else
                        c.Column3 = table.Rows[i][2].ToString();
                }
                else
                {
                    c.Column1 = table.Rows[i][0].ToString();
                    c.Column2 = table.Rows[i][1].ToString();
                    c.Column3 = table.Rows[i][2].ToString();
                    cOld.Column1 = table.Rows[i][0].ToString();
                    cOld.Column2 = table.Rows[i][1].ToString();
                    cOld.Column3 = table.Rows[i][2].ToString();
                }
                c.Column4 = table.Rows[i][3].ToString();
                c.Column5 = table.Rows[i][4].ToString();
                c.Column6 = table.Rows[i][5].ToString();
                c.Column7 = table.Rows[i][6].ToString();
                c.Column8 = table.Rows[i][7].ToString();
                c.Column9 = table.Rows[i][8].ToString();
                cOld.Column4 = table.Rows[i][3].ToString();
                cOld.Column5 = table.Rows[i][4].ToString();
                cOld.Column6 = table.Rows[i][5].ToString();
                cOld.Column7 = table.Rows[i][6].ToString();
                cOld.Column8 = table.Rows[i][7].ToString();
                cOld.Column9 = table.Rows[i][8].ToString();
                list.Add(c);

            }
            return list;
        }
        public static string Total(DataTable table, int index)
        {
            double sum = 0;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                sum += double.Parse(table.Rows[i][index].ToString());
            }
            return sum.ToString();

        }
        public static string Average(DataTable table, int index)
        {
            double sum = 0;
            double Avg = 0;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                sum += double.Parse(table.Rows[i][index].ToString());
            }
            if (table.Rows.Count != 0)
                Avg = (double)sum / (table.Rows.Count);
            return Avg.ToString();

        }



    }
}
