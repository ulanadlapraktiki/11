using System;
using System.Windows;

namespace HranitelPro
{
    public partial class LoginWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();

        public LoginWindow()
        {
            InitializeComponent();
            btnLogin.Click += BtnLogin_Click;
            btnEmployeeLogin.Click += BtnEmployeeLogin_Click;
            btnRegister.Click += BtnRegister_Click;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                lblError.Text = "Введите логин";
                txtLogin.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Введите пароль";
                txtPassword.Focus();
                return;
            }

            string hash = db.HashMD5(password);
            User? user = db.LoginORM(login, hash);

            if (user != null)
            {
                lblError.Text = "";
                MainWindow mainWindow = new MainWindow(user.UserID, $"{user.LastName} {user.FirstName}");
                mainWindow.Show();
                this.Close();
            }
            else
            {
                lblError.Text = "Неверный логин или пароль";
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void BtnEmployeeLogin_Click(object sender, RoutedEventArgs e)
        {
            string code = txtEmployeeCode.Password;

            if (string.IsNullOrWhiteSpace(code))
            {
                lblError.Text = "Введите код сотрудника";
                txtEmployeeCode.Focus();
                return;
            }

            if (!db.EmployeeCodeExists(code))
            {
                lblError.Text = "Код сотрудника не найден!";
                txtEmployeeCode.Clear();
                txtEmployeeCode.Focus();
                return;
            }

            Employee? employee = db.GetEmployeeByCode(code);

            if (employee == null)
            {
                lblError.Text = "Ошибка получения данных сотрудника!";
                return;
            }

            lblError.Text = "";

            switch (employee.Role)
            {
                case "general":
                    GeneralDepartmentWindow generalWindow = new GeneralDepartmentWindow();
                    generalWindow.Show();
                    this.Close();
                    break;

                case "security":
                    MessageBox.Show($"Добро пожаловать, {employee.Login}!\nРоль: Охрана",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "department":
                    MessageBox.Show($"Добро пожаловать, {employee.Login}!\nРоль: Подразделение\nВаше подразделение: {employee.Department}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                default:
                    MessageBox.Show($"Роль '{employee.Role}' не распознана",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
    }
}