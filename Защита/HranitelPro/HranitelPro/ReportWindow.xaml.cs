using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;

namespace HranitelPro
{
    public partial class ReportWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private DataTable reportData;
        private DateTime reportDate;
        private string period;

        public ReportWindow(DateTime date, string periodType)
        {
            InitializeComponent();
            reportDate = date;
            period = periodType;

            btnSaveReport.Click += BtnSaveReport_Click;
            btnSaveAuto.Click += BtnSaveAuto_Click;
            btnClose.Click += (s, e) => this.Close();

            LoadReport();
        }

        private void LoadReport()
        {
            string title = "";

            if (period == "день")
            {
                title = $"Отчет за {reportDate:dd.MM.yyyy}";
                reportData = db.GetReportByDay(reportDate);
            }
            else if (period == "месяц")
            {
                title = $"Отчет за {reportDate:MMMM yyyy}";
                reportData = db.GetReportByMonth(reportDate);
            }
            else if (period == "год")
            {
                title = $"Отчет за {reportDate:yyyy} год";
                reportData = db.GetReportByYear(reportDate);
            }
            else if (period == "3часа")
            {
                title = $"Отчет за последние 3 часа ({DateTime.Now.AddHours(-3):HH:mm} - {DateTime.Now:HH:mm})";
                reportData = db.GetLast3HoursReport();
            }

            lblTitle.Text = $"📊 {title}";
            dgvReport.ItemsSource = reportData.DefaultView;

            int total = 0;
            foreach (DataRow row in reportData.Rows)
            {
                total += Convert.ToInt32(row["count"]);
            }
            lblTotal.Text = $"📋 Всего посещений: {total}";
        }

        private void BtnSaveReport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt";
            saveDialog.FileName = $"Отчет_{reportDate:yyyyMMdd}_{period}.csv";

            if (saveDialog.ShowDialog() == true)
            {
                SaveReportToFile(saveDialog.FileName);
                MessageBox.Show("Отчет сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSaveAuto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folderPath = Path.Combine(documentsPath, "Отчеты ТБ");
                string todayFolder = Path.Combine(folderPath, DateTime.Now.ToString("dd_MM_yyyy"));

                // Создаем папку, если её нет
                if (!Directory.Exists(todayFolder))
                {
                    Directory.CreateDirectory(todayFolder);
                }

                string fileName = Path.Combine(todayFolder, $"Отчет_за_{reportDate:yyyyMMdd}_{period}.csv");
                SaveReportToFile(fileName);
                MessageBox.Show($"Отчет сохранен в:\n{fileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveReportToFile(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Отчет за период: {reportDate:dd.MM.yyyy} ({period})");
            sb.AppendLine($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("");
            sb.AppendLine("Подразделение;Количество посещений");

            foreach (DataRow row in reportData.Rows)
            {
                sb.AppendLine($"{row["department"]};{row["count"]}");
            }

            int total = 0;
            foreach (DataRow row in reportData.Rows)
            {
                total += Convert.ToInt32(row["count"]);
            }
            sb.AppendLine($"\nИТОГО: {total}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}