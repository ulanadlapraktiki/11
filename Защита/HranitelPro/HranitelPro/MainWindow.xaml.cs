using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace HranitelPro
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private ObservableCollection<RequestItem> requests = new ObservableCollection<RequestItem>();
        private int currentUserId;

        public MainWindow(int userId, string userName)
        {
            InitializeComponent();
            currentUserId = userId;
            lblUser.Text = $"👤 {userName}";
            dgvRequests.ItemsSource = requests;

            btnAdd.Click += BtnAdd_Click;
            btnGroupAdd.Click += BtnGroupAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnLogout.Click += BtnLogout_Click;

            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                requests.Clear();
                var list = db.GetUserRequests(currentUserId);
                foreach (var item in list)
                {
                    requests.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            RequestWindow requestWindow = new RequestWindow(currentUserId);
            if (requestWindow.ShowDialog() == true) LoadRequests();
        }

        private void BtnGroupAdd_Click(object sender, RoutedEventArgs e)
        {
            GroupRequestWindow groupWindow = new GroupRequestWindow(currentUserId);
            if (groupWindow.ShowDialog() == true) LoadRequests();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var selected = (RequestItem)dgvRequests.SelectedItem;
            RequestWindow requestWindow = new RequestWindow(currentUserId, selected.Id);
            if (requestWindow.ShowDialog() == true) LoadRequests();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show("Вы уверены, что хотите удалить заявку?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var selected = (RequestItem)dgvRequests.SelectedItem;
                db.DeleteRequest(selected.Id);
                LoadRequests();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadRequests();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из системы?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}