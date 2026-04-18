using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1;

namespace WpfApp1
{
    public partial class DepartmentWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Employee currentEmployee;
        private DataTable requests;

        public DepartmentWindow(Employee emp)
        {
            InitializeComponent();
            currentEmployee = emp;
            lblUser.Text = $"{emp.Login} ({emp.Department})";

            dpDate.SelectedDate = DateTime.Now;
            LoadRequests(null, null);
        }

        private void LoadRequests(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentEmployee.Department)) return;

            DateTime? date = dpDate.SelectedDate;

            if (date.HasValue)
                requests = db.GetRequestsByDepartmentAndDate(currentEmployee.Department, date.Value);
            else
                requests = db.GetRequestsByDepartment(currentEmployee.Department);

            dgvRequests.ItemsSource = requests.DefaultView;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests(null, null);
        }

        private void BtnArrival_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgvRequests.SelectedItem;
            Guid requestId = (Guid)row["id"];
            string visitorName = $"{row["last_name"]} {row["first_name"]}";

            if (db.HasArrivalTime(requestId))
            {
                MessageBox.Show($"Прибытие уже зафиксировано!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DateTime? entryTime = db.GetEntryTimeForRequest(requestId);
            if (!entryTime.HasValue)
            {
                MessageBox.Show("Посетитель ещё не прошёл через охрану!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime arrivalTimeNow = DateTime.Now;
            db.SetArrivalTime(requestId, arrivalTimeNow);

            int travelTimeMinutes = db.GetTravelTimeMinutes();
            TimeSpan actualTime = arrivalTimeNow - entryTime.Value;

            if (actualTime.TotalMinutes > travelTimeMinutes)
            {
                string message = $"⚠️ Нарушение! Превышено время перемещения!\nНорма: {travelTimeMinutes} мин, Фактически: {(int)actualTime.TotalMinutes} мин";
                MessageBox.Show(message, "Нарушение", MessageBoxButton.OK, MessageBoxImage.Warning);
                db.SendViolationNotification(requestId, message);
            }
            else
            {
                MessageBox.Show($"✅ Прибытие зафиксировано!\nПосетитель: {visitorName}\nВремя: {arrivalTimeNow:HH:mm:ss}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadRequests(null, null);
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgvRequests.SelectedItem;
            Guid requestId = (Guid)row["id"];

            RequestInfoWindow win = new RequestInfoWindow(requestId);
            win.ShowDialog();
        }

        private void DgvRequests_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var row = (DataRowView)dgvRequests.SelectedItem;
            if (row == null) return;

            Guid requestId = (Guid)row["id"];
            var visitors = db.GetVisitorsByRequest(requestId);

            if (visitors.Rows.Count == 0) return;

            ContextMenu menu = new ContextMenu();

            foreach (DataRow visitor in visitors.Rows)
            {
                string fullName = $"{visitor["last_name"]} {visitor["first_name"]}";
                if (visitor["middle_name"] != DBNull.Value)
                    fullName += $" {visitor["middle_name"]}";

                MenuItem item = new MenuItem();
                item.Header = $"🚫 Добавить в ЧС: {fullName}";
                item.Tag = visitor["id"];
                item.Click += AddToBlacklist_Click;
                menu.Items.Add(item);
            }

            dgvRequests.ContextMenu = menu;
        }

        private void AddToBlacklist_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            Guid guestId = (Guid)item.Tag;

            string reason = Interaction.InputBox(
                "Введите причину добавления в черный список:",
                "Причина добавления",
                "Нарушение правил посещения",
                -1, -1);

            if (!string.IsNullOrEmpty(reason))
            {
                db.AddToBlacklist(guestId, reason);
                MessageBox.Show("Посетитель добавлен в черный список!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                EmployeeLogin login = new EmployeeLogin();
                login.Show();
                this.Close();
            }
        }
    }
}