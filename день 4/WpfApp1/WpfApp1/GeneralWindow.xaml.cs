using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp1;

namespace WpfApp1
{
    public partial class GeneralWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Employee currentEmployee;
        private DataTable allRequests;

        public GeneralWindow(Employee emp)
        {
            InitializeComponent();
            currentEmployee = emp;
            lblUser.Text = $"{emp.Login} ({emp.Code})";

            cmbType.Items.Add("Все");
            foreach (var type in db.GetRequestTypes()) cmbType.Items.Add(type);
            cmbType.SelectedIndex = 0;

            cmbStatus.Items.Add("Все");
            foreach (var status in db.GetStatuses()) cmbStatus.Items.Add(status);
            cmbStatus.SelectedIndex = 0;

            cmbDepartment.Items.Add("Все");
            foreach (var dept in db.GetDepartments()) cmbDepartment.Items.Add(dept);
            cmbDepartment.SelectedIndex = 0;

            LoadRequests();
        }

        private void LoadRequests()
        {
            allRequests = db.GetAllRequests();
            dgvRequests.ItemsSource = allRequests.DefaultView;
            ApplyFilter(null, null);
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
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            AddRequestWindow addWin = new AddRequestWindow(currentEmployee);
            if (addWin.ShowDialog() == true)
            {
                LoadRequests();
                ApplyFilter(null, null);
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку (кликните на строку)", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var row = (DataRowView)dgvRequests.SelectedItem;
                Guid requestId = (Guid)row["id"];

                FormalCheckWindow win = new FormalCheckWindow(requestId);
                win.ShowDialog();

                LoadRequests();
                ApplyFilter(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgvRequests_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvRequests.SelectedItem != null)
                BtnView_Click(sender, null);
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