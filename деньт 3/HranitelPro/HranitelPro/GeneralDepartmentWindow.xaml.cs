using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HranitelPro
{
    public partial class GeneralDepartmentWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private DataTable allRequests;

        public GeneralDepartmentWindow()
        {
            InitializeComponent();
            lblUser.Text = "👔 Сотрудник общего отдела";

            cmbType.Items.Add("Все"); cmbType.Items.Add("личная"); cmbType.Items.Add("групповая");
            cmbType.SelectedIndex = 0;

            cmbStatus.Items.Add("Все"); cmbStatus.Items.Add("проверка");
            cmbStatus.Items.Add("одобрена"); cmbStatus.Items.Add("отклонена");
            cmbStatus.SelectedIndex = 0;

            cmbDepartment.Items.Add("Все");
            var depts = db.GetDepartments();
            foreach (DataRow row in depts.Rows) cmbDepartment.Items.Add(row["department"].ToString());
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
            LoadRequests(); // Обновляем список после закрытия
        }

        private void DgvRequests_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvRequests.SelectedItem != null) BtnView_Click(sender, null);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}