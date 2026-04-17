using System;
using System.Windows;

namespace WpfApp2
{
    public partial class RegisterWindow : Window
    {
        private AppDbContext db = new AppDbContext();

        public RegisterWindow()
        {
            InitializeComponent();
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string confirm = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || login.Length < 3)
            {
                txtMessage.Text = "Логин минимум 3 символа";
                return;
            }

            if (db.LoginExists(login))
            {
                txtMessage.Text = "Логин уже существует";
                return;
            }

            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            {
                txtMessage.Text = "Неверный email";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                txtMessage.Text = "Введите пароль";
                return;
            }

            if (password.Length < 6)
            {
                txtMessage.Text = "Пароль минимум 6 символов";
                return;
            }

            if (password != confirm)
            {
                txtMessage.Text = "Пароли не совпадают";
                return;
            }

            if (db.Register(login, password, "user"))
            {
                txtMessage.Text = "Регистрация успешна!";
                txtMessage.Foreground = System.Windows.Media.Brushes.Green;

                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1.5);
                timer.Tick += (s, args) => { timer.Stop(); this.Close(); };
                timer.Start();
            }
            else
            {
                txtMessage.Text = "Ошибка регистрации";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}