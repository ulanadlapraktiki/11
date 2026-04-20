using System;
using System.Windows;

namespace HranitelPro
{
    public partial class RegisterWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();

        public RegisterWindow()
        {
            InitializeComponent();
            btnRegister.Click += BtnRegister_Click;
            btnBack.Click += BtnBack_Click;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            errorBorder.Visibility = Visibility.Visible;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем предыдущую ошибку
            errorBorder.Visibility = Visibility.Collapsed;

            // Получение данных
            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string patronymic = txtPatronymic.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string passportSeries = txtPassportSeries.Text.Trim();
            string passportNumber = txtPassportNumber.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // ========== ПРОВЕРКА ОБЯЗАТЕЛЬНЫХ ПОЛЕЙ ==========

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ShowError("Введите фамилию");
                txtLastName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                ShowError("Введите имя");
                txtFirstName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Введите email");
                txtEmail.Focus();
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                ShowError("Введите корректный email (пример: user@mail.ru)");
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                txtLogin.Focus();
                return;
            }

            if (login.Length < 3)
            {
                ShowError("Логин должен содержать минимум 3 символа");
                txtLogin.Focus();
                return;
            }

            // ========== ПРОВЕРКА ПАРОЛЯ ==========

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                txtPassword.Focus();
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Пароль должен содержать минимум 6 символов");
                txtPassword.Focus();
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                txtConfirmPassword.Focus();
                return;
            }

            // ========== ПРОВЕРКА ДАТЫ РОЖДЕНИЯ ==========

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату рождения");
                dpBirthDate.Focus();
                return;
            }

            DateTime birthDate = dpBirthDate.SelectedDate.Value;
            int age = DateTime.Now.Year - birthDate.Year;
            if (birthDate.Date > DateTime.Now.AddYears(-age)) age--;

            if (age < 16)
            {
                ShowError("Возраст должен быть не младше 16 лет");
                dpBirthDate.Focus();
                return;
            }

            if (age > 120)
            {
                ShowError("Некорректная дата рождения");
                dpBirthDate.Focus();
                return;
            }

            // ========== ПРОВЕРКА ПАСПОРТА ==========

            if (string.IsNullOrWhiteSpace(passportSeries))
            {
                ShowError("Введите серию паспорта");
                txtPassportSeries.Focus();
                return;
            }

            if (passportSeries.Length != 4)
            {
                ShowError("Серия паспорта должна содержать 4 цифры");
                txtPassportSeries.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(passportNumber))
            {
                ShowError("Введите номер паспорта");
                txtPassportNumber.Focus();
                return;
            }

            if (passportNumber.Length != 6)
            {
                ShowError("Номер паспорта должен содержать 6 цифр");
                txtPassportNumber.Focus();
                return;
            }

            // ========== ПРОВЕРКА ТЕЛЕФОНА (опционально, но с маской) ==========

            if (!string.IsNullOrWhiteSpace(phone))
            {
                string cleanPhone = phone.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                if (cleanPhone.Length < 10)
                {
                    ShowError("Введите корректный номер телефона");
                    txtPhone.Focus();
                    return;
                }
            }

            // ========== ПРОВЕРКА СУЩЕСТВОВАНИЯ ЛОГИНА ==========

            if (db.LoginExists(login))
            {
                ShowError("Пользователь с таким логином уже существует");
                txtLogin.Focus();
                return;
            }

            // ========== РЕГИСТРАЦИЯ ==========

            string passportData = passportSeries + passportNumber;
            string hash = db.HashMD5(password);

            bool success = db.RegisterSQL(
                lastName,
                firstName,
                string.IsNullOrEmpty(patronymic) ? null : patronymic,
                string.IsNullOrEmpty(phone) ? null : phone,
                email,
                birthDate,
                passportData,
                login,
                hash);

            if (success)
            {
                MessageBox.Show("Регистрация успешно завершена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("Ошибка при регистрации. Возможно, такой email уже используется.");
            }
        }
    }
}