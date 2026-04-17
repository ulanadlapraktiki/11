using System;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private string currentUser = "";
        private string currentRole = "";
        private AppDbContext db = new AppDbContext();
        private ObservableCollection<RequestInfo> requests = new ObservableCollection<RequestInfo>();

        public MainWindow()
        {
            InitializeComponent();
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnLogout.Click += BtnLogout_Click;
        }

        public MainWindow(string login, string role) : this()
        {
            currentUser = login;
            currentRole = role;
            lblUser.Text = $"Пользователь: {login} ({role})";

            LoadRequests();
            dgvRequests.ItemsSource = requests;
        }

        private void LoadRequests()
        {
            requests.Clear();

            if (string.IsNullOrEmpty(currentUser)) return;

            try
            {
                Guid employeeId = db.GetEmployeeId(currentUser);
                var list = db.GetRequestsByEmployee(employeeId);

                foreach (var item in list)
                {
                    requests.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentUser))
            {
                MessageBox.Show("Пользователь не авторизован");
                return;
            }

            ChoiceWindow choiceWindow = new ChoiceWindow();
            if (choiceWindow.ShowDialog() == true)
            {
                if (choiceWindow.SelectedType == ChoiceWindow.RequestType.Personal)
                {
                    RequestWindow requestWindow = new RequestWindow(currentUser);
                    if (requestWindow.ShowDialog() == true)
                    {
                        LoadRequests();
                    }
                }
                else if (choiceWindow.SelectedType == ChoiceWindow.RequestType.Group)
                {
                    GroupRequestWindow groupWindow = new GroupRequestWindow(currentUser);
                    if (groupWindow.ShowDialog() == true)
                    {
                        LoadRequests();
                    }
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = (RequestInfo)dgvRequests.SelectedItem;
            RequestWindow requestWindow = new RequestWindow(currentUser, selected.Id);
            if (requestWindow.ShowDialog() == true)
            {
                LoadRequests();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgvRequests.SelectedItem == null)
            {
                MessageBox.Show("Выберите заявку", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Удалить заявку?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var selected = (RequestInfo)dgvRequests.SelectedItem;
                if (db.DeleteRequest(selected.Id))
                {
                    LoadRequests();
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

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