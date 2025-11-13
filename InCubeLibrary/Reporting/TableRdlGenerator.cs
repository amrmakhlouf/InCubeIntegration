using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
namespace InCubeLibrary
{
    public class TableRdlGenerator
    {
        private List<string> m_fields;
        public object values;
        public List<string> Fields
        {
            get { return m_fields; }
            set { m_fields = value; }
        }
        public Rdl.TableType CreateTable()
        {

            Rdl.TableType table = new Rdl.TableType();
            table.Name = "Table1";
            table.Items = new object[]
                {
                    CreateTableColumns(),
                   CreateHeader(), 
                    CreateDetails(), 
                    "0cm", 
                    true,
                };
            table.ItemsElementName = new Rdl.ItemsChoiceType21[]
                {
                    Rdl.ItemsChoiceType21.TableColumns,
                   Rdl.ItemsChoiceType21.Header, //
                    Rdl.ItemsChoiceType21.Details,
                    Rdl.ItemsChoiceType21.Top,
                    Rdl.ItemsChoiceType21.FillPage,
                };
            return table;
        }
        private Rdl.HeaderType CreateHeader()
        {
            Rdl.HeaderType header = new Rdl.HeaderType();
            header.Items = new object[]
                {
                    CreateHeaderTableRows(),
                    true,
                };
            header.ItemsElementName = new Rdl.ItemsChoiceType20[]
                {
                    Rdl.ItemsChoiceType20.TableRows,
                    Rdl.ItemsChoiceType20.RepeatOnNewPage,
                };
            return header;
        }
        private Rdl.TableRowsType CreateHeaderTableRows()
        {
            Rdl.TableRowsType headerTableRows = new Rdl.TableRowsType();
            headerTableRows.TableRow = new Rdl.TableRowType[] { CreateHeaderTableRow() };
            return headerTableRows;
        }
        private Rdl.TableRowType CreateHeaderTableRow()
        {
            Rdl.TableRowType headerTableRow = new Rdl.TableRowType();
            headerTableRow.Items = new object[] { CreateHeaderTableCells(), "0.25in" };  // row hight
            return headerTableRow;
        }
        private Rdl.TableCellsType CreateHeaderTableCells()
        {
            Rdl.TableCellsType headerTableCells = new Rdl.TableCellsType();
            headerTableCells.TableCell = new Rdl.TableCellType[m_fields.Count];

            for (int i = 0; i < m_fields.Count; i++)
            {
                headerTableCells.TableCell[i] = CreateHeaderTableCell(m_fields[i]);
            }
            return headerTableCells;
        }
        private Rdl.TableCellType CreateHeaderTableCell(string fieldName)
        {
            Rdl.TableCellType headerTableCell = new Rdl.TableCellType();
            headerTableCell.Items = new object[] { CreateHeaderTableCellReportItems(fieldName) };
            return headerTableCell;
        }
        private Rdl.ReportItemsType CreateHeaderTableCellReportItems(string fieldName)
        {
            Rdl.ReportItemsType headerTableCellReportItems = new Rdl.ReportItemsType();
            headerTableCellReportItems.Items = new object[] { CreateHeaderTableCellTextbox(fieldName) };
            return headerTableCellReportItems;
        }
        private Rdl.TextboxType CreateHeaderTableCellTextbox(string fieldName)
        {
            Rdl.TextboxType headerTableCellTextbox = new Rdl.TextboxType();
            headerTableCellTextbox.Name = fieldName + "_Header";
            headerTableCellTextbox.Items = new object[] 
                {
                    fieldName,
                    CreateHeaderTableCellTextboxStyle(),
                    true,
                };
            headerTableCellTextbox.ItemsElementName = new Rdl.ItemsChoiceType14[] 
                {
                    Rdl.ItemsChoiceType14.Value,
                    Rdl.ItemsChoiceType14.Style,
                    Rdl.ItemsChoiceType14.CanGrow,
                };
            return headerTableCellTextbox;
        }
        private Rdl.StyleType CreateHeaderTableCellTextboxStyle()
        {
            Rdl.StyleType headerTableCellTextboxStyle = new Rdl.StyleType();
            headerTableCellTextboxStyle.Items = new object[]
                {
                    "11pt",  /// may this for table headers  
                    "darkblue",
                    "Center",
                };
            headerTableCellTextboxStyle.ItemsElementName = new Rdl.ItemsChoiceType5[]
                {
                    Rdl.ItemsChoiceType5.FontSize,
                    Rdl.ItemsChoiceType5.Color , 
                    Rdl.ItemsChoiceType5.TextAlign,
              
                };
            return headerTableCellTextboxStyle;
        }
        private Rdl.DetailsType CreateDetails()
        {
            Rdl.DetailsType details = new Rdl.DetailsType();
            details.Items = new object[] { CreateTableRows() };

            return details;
        }
        private Rdl.TableRowsType CreateTableRows()
        {
            Rdl.TableRowsType tableRows = new Rdl.TableRowsType();
            tableRows.TableRow = new Rdl.TableRowType[] { CreateTableRow() };

            return tableRows;
        }
        private Rdl.TableRowType CreateTableRow()
        {
            Rdl.TableRowType tableRow = new Rdl.TableRowType();
            tableRow.Items = new object[] { CreateTableCells(), "0.25in" };  // row hight

            return tableRow;
        }
        private Rdl.TableCellsType CreateTableCells()
        {
            Rdl.TableCellsType tableCells = new Rdl.TableCellsType();
            tableCells.TableCell = new Rdl.TableCellType[m_fields.Count];
            for (int i = 0; i < m_fields.Count; i++)
            {
                tableCells.TableCell[i] = CreateTableCell(m_fields[i]);
            }

            return tableCells;
        }
        private Rdl.TableCellType CreateTableCell(string fieldName)
        {
            Rdl.TableCellType tableCell = new Rdl.TableCellType();
            tableCell.Items = new object[] { CreateTableCellReportItems(fieldName) };
            return tableCell;
        }
        private Rdl.ReportItemsType CreateTableCellReportItems(string fieldName)
        {
            Rdl.ReportItemsType reportItems = new Rdl.ReportItemsType();
            reportItems.Items = new object[] { CreateTableCellTextbox(fieldName) };

            return reportItems;
        }
        public Rdl.TextboxType CreateTableCellTextbox(string fieldName)
        {
            Rdl.TextboxType textbox = new Rdl.TextboxType();
            textbox.Name = fieldName;
            textbox.Items = new object[] 
                {
                    "=Fields!" + fieldName + ".Value",
                    CreateTableCellTextboxStyle(),
                    true,
                };
            textbox.ItemsElementName = new Rdl.ItemsChoiceType14[] 
                {
                    Rdl.ItemsChoiceType14.Value,
                    Rdl.ItemsChoiceType14.Style,
                    Rdl.ItemsChoiceType14.CanGrow,
                };
            return textbox;


        }
        private Rdl.StyleType CreateTextboxStyle()
        {
            Rdl.StyleType TextboxStyle = new Rdl.StyleType();
            TextboxStyle.Items = new object[]
                {
                    "Left",
                    "11pt",  // may this for  table header
                    "0pt",
                    "0pt",
                    "0pt",
                    "0pt",
                   
                   
               };
            TextboxStyle.ItemsElementName = new Rdl.ItemsChoiceType5[]
                { 
                    Rdl.ItemsChoiceType5.TextAlign,
                    Rdl.ItemsChoiceType5.FontSize,
                    Rdl.ItemsChoiceType5.PaddingRight, 
                    Rdl.ItemsChoiceType5.PaddingLeft ,
                    Rdl.ItemsChoiceType5.PaddingBottom,
                    Rdl.ItemsChoiceType5 .PaddingTop ,
                   
                   
                };
            return TextboxStyle;
        }
        private Rdl.StyleType CreateTableCellTextboxStyle()
        {
            Rdl.StyleType style = new Rdl.StyleType();
            style.Items = new object[]
                {
                    "=iif(RowNumber(Nothing) mod 2, \"AliceBlue\", \"White\")",
                    "Center", 
                    "darkblue",
                     "10pt",  // may this the content of the table (table cell)
                     "2pt",
                     "0pt",
                     "2pt",
                     "2pt",
                };
            style.ItemsElementName = new Rdl.ItemsChoiceType5[]
                {
                    Rdl.ItemsChoiceType5.BackgroundColor,
                    Rdl.ItemsChoiceType5.TextAlign,
                    Rdl.ItemsChoiceType5.Color , 
                    Rdl.ItemsChoiceType5.FontSize,
                    Rdl.ItemsChoiceType5.PaddingRight, 
                    Rdl.ItemsChoiceType5.PaddingLeft ,
                    Rdl.ItemsChoiceType5.PaddingBottom,
                    Rdl.ItemsChoiceType5.PaddingTop ,
                };
            return style;
        }
        private Rdl.TableColumnsType CreateTableColumns()
        {
            Rdl.TableColumnsType tableColumns = new Rdl.TableColumnsType();
            tableColumns.TableColumn = new Rdl.TableColumnType[m_fields.Count];
            for (int i = 0; i < m_fields.Count; i++)
            {
                tableColumns.TableColumn[i] = CreateTableColumn();
            }
            return tableColumns;
        }
        private Rdl.TableColumnType CreateTableColumn()
        {
            Rdl.TableColumnType tableColumn = new Rdl.TableColumnType();
            tableColumn.Items = new object[] { 17.95 / m_fields.Count + "cm" };      // "1in" };  // coulmns width 
            return tableColumn;
        }
    }
}
