using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1;

namespace WpfApp1
{
    public partial class SecurityWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Employee currentEmployee;
        private DataTable allRequests;
        private DataTable filteredRequests;

        public SecurityWindow(Employee emp)
        {
            InitializeComponent();
            currentEmployee = emp;
            lblUser.Text = $"{emp.Login} ({emp.Code})";

            dpDate.SelectedDate = DateTime.Now;

            cmbType.Items.Add("Все");
            foreach (var type in db.GetRequestTypes()) cmbType.Items.Add(type);
            cmbType.SelectedIndex = 0;

            cmbDepartment.Items.Add("Все");
            foreach (var dept in db.GetApprovedDepartments()) cmbDepartment.Items.Add(dept);
            cmbDepartment.SelectedIndex = 0;

            LoadRequests();
        }

        private void LoadRequests()
        {
            allRequests = db.GetApprovedRequests();
            filteredRequests = allRequests;
            dgvRequests.ItemsSource = filteredRequests.DefaultView;
        }

        private void ApplyFilter(object sender, RoutedEventArgs e)
        {
            if (allRequests == null) return;

            DateTime? date = dpDate.SelectedDate;
            string type = cmbType.SelectedItem?.ToString();
            string dept = cmbDepartment.SelectedItem?.ToString();

            var filtered = allRequests.AsEnumerable();

            if (date.HasValue)
            {
                filtered = filtered.Where(row =>
                {
                    DateTime startDate = row.Field<DateTime>("start_date");
                    DateTime endDate = row.Field<DateTime>("end_date");
                    return startDate.Date <= date.Value.Date && endDate.Date >= date.Value.Date;
                });
            }

            if (type != "Все") filtered = filtered.Where(row => row.Field<string>("requesttype") == type);
            if (dept != "Все") filtered = filtered.Where(row => row.Field<string>("targetdepartment") == dept);

            filteredRequests = filtered.Any() ? filtered.CopyToDataTable() : allRequests.Clone();
            dgvRequests.ItemsSource = filteredRequests.DefaultView;
        }

        private void ApplySearch(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                dgvRequests.ItemsSource = filteredRequests.DefaultView;
                return;
            }

            string searchText = txtSearch.Text.ToLower();
            var searched = filteredRequests.AsEnumerable().Where(row =>
                row.Field<string>("last_name").ToLower().Contains(searchText) ||
                row.Field<string>("first_name").ToLower().Contains(searchText) ||
                (row.Field<string>("middle_name") != null && row.Field<string>("middle_name").ToLower().Contains(searchText)) ||
                (row.Field<string>("passport_number") != null && row.Field<string>("passport_number").Contains(searchText))
            );

            dgvRequests.ItemsSource = searched.Any() ? searched.CopyToDataTable().DefaultView : null;
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            dgvRequests.ItemsSource = filteredRequests.DefaultView;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
            ApplyFilter(null, null);
            txtSearch.Text = "";
        }

        private void BtnAccess_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgvRequests.SelectedItem;
            Guid requestId = (Guid)row["id"];
            string visitorName = $"{row["last_name"]} {row["first_name"]}";

            if (db.HasEntryTime(requestId))
            {
                MessageBox.Show($"Посетитель уже прошёл!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AccessWindow win = new AccessWindow(requestId, visitorName);
            win.ShowDialog();
            LoadRequests();
            ApplyFilter(null, null);
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