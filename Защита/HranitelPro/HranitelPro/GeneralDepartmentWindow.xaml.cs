using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace HranitelPro
{
    public partial class GeneralDepartmentWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private DataTable allRequests;
        private System.Windows.Threading.DispatcherTimer autoReportTimer;

        public GeneralDepartmentWindow()
        {
            InitializeComponent();
            lblUser.Text = "👔 Сотрудник общего отдела";

            // Создание папки для отчетов
            CreateReportsFolder();

            // Запуск автоматического формирования отчетов
            StartAuto3HoursReportTimer();

            // Заполнение фильтров
            cmbType.Items.Add("Все");
            cmbType.Items.Add("личная");
            cmbType.Items.Add("групповая");
            cmbType.SelectedIndex = 0;

            cmbStatus.Items.Add("Все");
            cmbStatus.Items.Add("проверка");
            cmbStatus.Items.Add("одобрена");
            cmbStatus.Items.Add("отклонена");
            cmbStatus.SelectedIndex = 0;

            cmbDepartment.Items.Add("Все");
            try
            {
                var depts = db.GetDepartments();
                foreach (DataRow row in depts.Rows)
                {
                    cmbDepartment.Items.Add(row["department"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            cmbDepartment.SelectedIndex = 0;

            LoadRequests();
        }

        // ==================== ИНИЦИАЛИЗАЦИЯ ====================

        private void CreateReportsFolder()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folderPath = Path.Combine(documentsPath, "Отчеты ТБ");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    lblStatus.Text = $"📁 Папка создана: {folderPath}";
                }
                else
                {
                    lblStatus.Text = $"📁 Папка существует: {folderPath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания папки: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartAuto3HoursReportTimer()
        {
            autoReportTimer = new System.Windows.Threading.DispatcherTimer();
            autoReportTimer.Interval = TimeSpan.FromHours(3);
            autoReportTimer.Tick += (s, e) =>
            {
                try
                {
                    db.SaveLast3HoursReport();
                    lblStatus.Text = $"📁 Отчет за 3 часа сохранен в {DateTime.Now:HH:mm:ss}";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ Ошибка автоотчета: {ex.Message}";
                }
            };
            autoReportTimer.Start();
        }

        // ==================== ЗАГРУЗКА И ФИЛЬТРАЦИЯ ====================

        private void LoadRequests()
        {
            try
            {
                allRequests = db.GetAllRequests();
                dgvRequests.ItemsSource = allRequests.DefaultView;
                ApplyFilter(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (allRequests == null) return;

            string type = cmbType.SelectedItem?.ToString();
            string status = cmbStatus.SelectedItem?.ToString();
            string dept = cmbDepartment.SelectedItem?.ToString();

            var filtered = allRequests.AsEnumerable().Where(row =>
                (type == "Все" || row.Field<string>("requesttype") == type) &&
                (status == "Все" || row.Field<string>("status") == status) &&
                (dept == "Все" || row.Field<string>("targetdepartment") == dept)
            );

            dgvRequests.ItemsSource = filtered.Any() ? filtered.CopyToDataTable().DefaultView : null;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            int count = dgvRequests.ItemsSource != null ?
                (dgvRequests.ItemsSource as DataView)?.Count ?? 0 : 0;
            lblStatus.Text = $"📋 Всего заявок: {count}";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        // ==================== ФОРМАЛЬНАЯ ПРОВЕРКА ====================

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgvRequests.SelectedItem;
            int requestId = Convert.ToInt32(row["requestid"]);

            FormalCheckWindow win = new FormalCheckWindow(requestId);
            win.ShowDialog();
            LoadRequests();
        }

        private void DgvRequests_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvRequests.SelectedItem != null) BtnView_Click(sender, null);
        }

        // ==================== ОТЧЕТЫ ====================

        private void BtnReportDay_Click(object sender, RoutedEventArgs e)
        {
            DateTime selectedDate = DateTime.Now;
            var result = MessageBox.Show($"Сформировать отчет за {selectedDate:dd.MM.yyyy}?", "Отчет",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShowReportWindow(selectedDate, "день");
            }
        }

        private void BtnReportMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTime selectedDate = DateTime.Now;
            var result = MessageBox.Show($"Сформировать отчет за {selectedDate:MMMM yyyy}?", "Отчет",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShowReportWindow(selectedDate, "месяц");
            }
        }

        private void BtnReportYear_Click(object sender, RoutedEventArgs e)
        {
            DateTime selectedDate = DateTime.Now;
            var result = MessageBox.Show($"Сформировать отчет за {selectedDate:yyyy} год?", "Отчет",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ShowReportWindow(selectedDate, "год");
            }
        }

        private void BtnLast3Hours_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = db.GetLast3HoursReport();

                if (data.Rows.Count == 0)
                {
                    MessageBox.Show("За последние 3 часа нет посещений", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowReportWindow(DateTime.Now, "3часа");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowReportWindow(DateTime date, string period)
        {
            try
            {
                ReportWindow reportWindow = new ReportWindow(date, period);
                reportWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCurrentVisitors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CurrentVisitorsWindow visitorsWindow = new CurrentVisitorsWindow();
                visitorsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ВЫХОД ====================

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из системы?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                autoReportTimer?.Stop();
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}