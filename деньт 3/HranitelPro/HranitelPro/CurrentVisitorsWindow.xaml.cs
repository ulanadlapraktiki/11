using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;

namespace HranitelPro
{
    public partial class CurrentVisitorsWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();

        public CurrentVisitorsWindow()
        {
            InitializeComponent();
            LoadVisitors();
        }

        private void LoadVisitors()
        {
            var dt = db.GetCurrentVisitors();
            dgvVisitors.ItemsSource = dt.DefaultView;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadVisitors();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt";
            saveDialog.FileName = $"Текущие_посетители_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            if (saveDialog.ShowDialog() == true)
            {
                SaveToFile(saveDialog.FileName);
                MessageBox.Show("Список сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveToFile(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Список лиц на территории");
            sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine("");
            sb.AppendLine("Подразделение;Фамилия;Имя;Отчество;Время входа;Статус");

            var dt = db.GetCurrentVisitors();
            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine($"{row["department"]};{row["last_name"]};{row["first_name"]};" +
                             $"{row["middle_name"]};{row["entry_time"]};{row["status"]}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}