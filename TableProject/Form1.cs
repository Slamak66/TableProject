using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace TableProject
{
    public partial class Form1 : Form
    {
        DataTable ActiveTable;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "XML Files|*.xml|All Files|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Title = "Select a File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                XDocument xDoc = XDocument.Load(filePath);

                ActiveTable = new DataTable();

                var veh1 = xDoc.Descendants("vehicle").FirstOrDefault();
                if(veh1 != null)
                {
                    foreach (var element in veh1.Elements())
                    {
                        string ColumnName = element.Name.LocalName;
                        string DataType = element.Attribute("type")?.Value;
                        DataColumn activeColumn = new DataColumn();
                        activeColumn.ColumnName = ColumnName;
                        string ActiveType = "System." + DataType;
                        activeColumn.DataType = System.Type.GetType(ActiveType);
                        activeColumn.AllowDBNull = false;
                        activeColumn.Caption = ColumnName;
                        ActiveTable.Columns.Add(activeColumn);
                    }
                }

                foreach (var vehicle in xDoc.Descendants("vehicle"))
                {
                    DataRow activeRow = ActiveTable.NewRow();

                    foreach(var element in vehicle.Elements())
                    {
                        string ColumnName = element.Name.LocalName;

                        activeRow[ColumnName] = element.Value;
                    }
                    ActiveTable.Rows.Add(activeRow);
                }

                dgvMain.DataSource = ActiveTable;

                foreach (DataGridViewColumn column in dgvMain.Columns)
                {
                    if (column.Name == "Cena")
                    {
                        column.DefaultCellStyle.Format = "C";
                    }
                }

                List<string> modelNames = ActiveTable.AsEnumerable()
                    .Select(row => row.Field<string>("NázevModelu"))
                    .Distinct()
                    .ToList();
                cbbModel.Items.Clear();
                cbbModel.Items.AddRange(modelNames.ToArray());
                cbbModel.Items.Add("All");
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if(cbbModel.SelectedItem == null)
            {
                return;
            }
            string selectedModel = cbbModel.SelectedItem.ToString();
            DateTime fromDate = dtpFrom.Value.Date;
            DateTime toDate = dtpTo.Value.Date;

            List<string> modelNamesList = new List<string>();
            List<double> totalPricesList = new List<double>();
            List<double> totalPricesWithVATList = new List<double>();

            List<string> formattedResultsList = new List<string>();
            List<string> formattedVATResultsList = new List<string>();

            foreach (DataGridViewRow row in dgvMain.Rows)
            {
                if (row.Cells["NázevModelu"].Value == null || row.Cells["DatumProdeje"].Value == null || row.Cells["Cena"].Value == null || row.Cells["DPH"].Value == null)
                {
                    continue;
                }
                string modelName = row.Cells["NázevModelu"].Value.ToString();
                DateTime dateOfSale = Convert.ToDateTime(row.Cells["DatumProdeje"].Value);
                double priceNoVAT = Convert.ToDouble(row.Cells["Cena"].Value);
                double VAT = Convert.ToDouble(row.Cells["DPH"].Value) / 100;
                double priceWithVAT = priceNoVAT + (priceNoVAT * VAT);

                if ((selectedModel == "All" || modelName == selectedModel) && (dateOfSale >= fromDate && dateOfSale <= toDate))
                {
                    int modelIndex = modelNamesList.IndexOf(modelName);
                    if (modelIndex == -1)
                    {
                        modelNamesList.Add(modelName);
                        totalPricesList.Add(priceNoVAT);
                        totalPricesWithVATList.Add(priceWithVAT);
                    }
                    else
                    {
                        totalPricesList[modelIndex] += priceNoVAT;
                        totalPricesWithVATList[modelIndex] += priceWithVAT;
                    }
                }
            }

            foreach (var modelName in modelNamesList)
            {
                int modelIndex = modelNamesList.IndexOf(modelName);
                double priceNoVAT = totalPricesList[modelIndex];
                double priceWithVAT = totalPricesWithVATList[modelIndex];

                string formattedString = $"{modelName}\n{priceNoVAT:C}";
                string formattedVATString = $"\n{priceWithVAT:C}";

                formattedResultsList.Add(formattedString);
                formattedVATResultsList.Add(formattedVATString);
            }


            dgvOutput.Rows.Clear();

            for(int i = 0; i < modelNamesList.Count; i++)
            {
                int rowIndex = dgvOutput.Rows.Add();

                dgvOutput.Rows[rowIndex].Cells[0].Value = formattedResultsList[i];
                dgvOutput.Rows[rowIndex].Cells[1].Value = formattedVATResultsList[i];
            }

            dgvOutput.Refresh();
        }
    }
}
