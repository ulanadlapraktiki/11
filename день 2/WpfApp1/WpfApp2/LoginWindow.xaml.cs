using System;
using System.Windows;

namespace WpfApp2
{
    public partial class LoginWindow : Window
    {
        private AppDbContext db = new AppDbContext();

        public LoginWindow()
        {
            InitializeComponent();
            btnLogin.Click += BtnLogin_Click;
            btnRegister.Click += BtnRegister_Click;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Заполните все поля";
                return;
            }

            if (db.Login(login, password))
            {
                string role = db.GetEmployeeRole(login);
                MainWindow main = new MainWindow(login, role);
                main.Show();
                this.Close();
            }
            else
            {
                lblError.Text = "Неверный логин или пароль";
                txtPassword.Clear();
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow regWin = new RegisterWindow();
            regWin.ShowDialog();
        }
    }
}