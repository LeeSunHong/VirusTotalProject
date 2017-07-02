using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using VirusTotalNET;
using VirusTotalNET.ResponseCodes;
using VirusTotalNET.Results;
using Excel = Microsoft.Office.Interop.Excel;
namespace VirusTotalProject
{
    public partial class Form1 : Form
    {
        int taskCount = 0;
        int endTask = 0;
        VirusTotal virusTotal;
        const String API_KEY = "5e3ab76885cb5c6a5038a7972b8815581bd898dce47eb57feb4cddc13070481f";
        System.IO.FileInfo EXCEL_PATH;
        Thread checkThread = null;
        Excel.Application excelApp;
        Excel._Workbook workBook;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                EXCEL_PATH = new System.IO.FileInfo(Application.StartupPath + "\\VirusReport.xlsx");
                checkThread = new Thread(delegate () { checkFiles(folderBrowserDialog1.SelectedPath); });
                checkThread.Start();
            }
            else
            {
                System.Windows.Forms.Application.Exit();
            }
        }
        private void checkFiles(String folderPath)
        {
            this.Visible = false;
            if (System.IO.Directory.Exists(folderPath))
            {
                excelApp = new Excel.Application();
                if (EXCEL_PATH.Exists)
                {
                    workBook = excelApp.Workbooks.Open(EXCEL_PATH.FullName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                }
                else
                {
                    workBook = excelApp.Workbooks.Add("");
                }
               System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(folderPath);
                taskCount = di.GetFiles().Length;
                Console.Write("Started scanning {0}files. 0/{0}", taskCount);
                foreach (var file in di.GetFiles())
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file.FullName);
                    getFileReport(fi); 
                }
            }
        }

        private async Task getFileReport(System.IO.FileInfo fileInfo)
        {
            virusTotal = new VirusTotal(API_KEY);
            //Use HTTPS instead of HTTP
            virusTotal.UseTLS = true;
            //Check if the file has been scanned before.
            FileReport fileReport = await virusTotal.GetFileReport(fileInfo);
            bool hasFileBeenScannedBefore = fileReport.ResponseCode == ReportResponseCode.Present;
            //already there is file
            if (hasFileBeenScannedBefore)
            {
                addReport(fileReport,fileInfo.FullName);
                countTask(fileInfo.Name);
            }
            // upload the file
            else 
            {
                ScanResult fileResults = await virusTotal.ScanFile(fileInfo);
                if (fileResults.SHA256.Length != 0)
                    scanFileReport(fileInfo);
            }
        }
        
        private void scanFileReport(System.IO.FileInfo fileInfo)
        {
            Thread.Sleep(30000);
            getFileReport(fileInfo);
        }

        private void countTask(String fileName)
        {
            endTask += 1;
            Console.Clear();
            Console.Write("Started scanning {0}files. {1}/{0}", taskCount,endTask);
            if (endTask == taskCount)
            {
                if (EXCEL_PATH.Exists)
                {
                    workBook.Save();
                }
                else
                {
                    workBook.SaveAs(EXCEL_PATH.FullName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing,
                                false, false, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                                Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                }
                workBook.Close();
                excelApp.Quit();
                System.Diagnostics.Process.Start("explorer.exe", Application.StartupPath);
                System.Windows.Forms.Application.Exit();
            }
        }

        private void addReport(FileReport fileReport, String fileName)
        {
            Excel._Worksheet workSheet = (Microsoft.Office.Interop.Excel._Worksheet)excelApp.Worksheets.Add();
            workSheet.Cells[1, 1] = fileName;
            workSheet.Range["A1:A1"].Font.Bold = true;
            workSheet.Range["A1:A1"].Interior.Color = Excel.XlRgbColor.rgbLightGray;
            workSheet.Range["A1", "K1"].MergeCells = true;
            workSheet.Range["A1:A1"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            workSheet.Cells[2, 1] = "SHA256";
            workSheet.Range["A2", "C2"].MergeCells = true;
            workSheet.Range["A2:C2"].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

            workSheet.Cells[2, 4] = fileReport.SHA256.ToString();
            workSheet.Range["D2", "K2"].MergeCells = true;
            workSheet.Range["D2:K2"].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

            workSheet.Range["A3:K3"].Interior.Color = Excel.XlRgbColor.rgbLightGray;
            workSheet.Range["A3:K3"].Font.Bold = true;
            workSheet.Range["A3:K3"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            workSheet.Cells[3, 1] = "Vaccine";
            workSheet.Range["A3", "C3"].MergeCells = true;

            workSheet.Cells[3, 4] = "Version";
            workSheet.Range["D3", "E3"].MergeCells = true;

            workSheet.Cells[3, 6] = "Update";
            workSheet.Range["F3", "G3"].MergeCells = true;

            workSheet.Cells[3, 11] = "Detect";
            workSheet.Range["H3", "K3"].MergeCells = true;

            for (int i = 0; i < fileReport.Scans.Count; i++)
            {
                workSheet.Cells[i + 4, 1] = fileReport.Scans.Skip(i).First().Key.ToString();
                workSheet.Range[workSheet.Cells[i + 4, 1], workSheet.Cells[i + 4, 3]].MergeCells = true;

                workSheet.Cells[i + 4, 4] = fileReport.Scans.Skip(i).First().Value.Version.ToString();
                workSheet.Range[workSheet.Cells[i + 4, 4], workSheet.Cells[i + 4, 5]].MergeCells = true;
                workSheet.Range[workSheet.Cells[i + 4, 4], workSheet.Cells[i + 4, 5]].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                workSheet.Cells[i + 4, 6] = fileReport.Scans.Skip(i).First().Value.Update.ToString("yyyyMMdd");
                workSheet.Range[workSheet.Cells[i + 4, 6], workSheet.Cells[i + 4, 7]].MergeCells = true;
                workSheet.Range[workSheet.Cells[i + 4, 6], workSheet.Cells[i + 4, 7]].HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                if (fileReport.Scans.Skip(i).First().Value.Detected) {
                    workSheet.Cells[i + 4, 8] = fileReport.Scans.Skip(i).First().Value.Result.ToString();
                    workSheet.Range[workSheet.Cells[i + 4, 8], workSheet.Cells[i + 4, 11]].Interior.Color = Excel.XlRgbColor.rgbPaleVioletRed;
                }
                else {
                    workSheet.Cells[i + 4, 8] = "None";
                    workSheet.Range[workSheet.Cells[i + 4, 8], workSheet.Cells[i + 4, 11]].Interior.Color = Excel.XlRgbColor.rgbPaleGreen;
                }
                workSheet.Range[workSheet.Cells[i + 4, 8], workSheet.Cells[i + 4, 11]].MergeCells = true;
            }
        }

    }
}
