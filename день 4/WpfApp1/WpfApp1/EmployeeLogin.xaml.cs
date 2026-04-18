using System;
using System.Windows;
using WpfApp1;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace WpfApp1
{
    public partial class EmployeeLogin : Window
    {
        private DatabaseHelper db = new DatabaseHelper();

        public EmployeeLogin()
        {
            InitializeComponent();
            btnLogin.Click += BtnLogin_Click;
            btnTestDB.Click += BtnTestDB_Click;
        }

        private void BtnTestDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (db.TestConnection())
                {
                    lblError.Text = "✅ Подключение к БД успешно!";
                    lblError.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    lblError.Text = "❌ Не удалось подключиться к БД! Проверьте настройки.";
                    lblError.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"❌ Ошибка: {ex.Message}";
                lblError.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string code = txtCode.Password;

            if (string.IsNullOrWhiteSpace(code))
            {
                lblError.Text = "Введите код сотрудника";
                return;
            }

            if (!db.EmployeeCodeExists(code))
            {
                lblError.Text = "Код не найден в системе";
                return;
            }

            Employee emp = db.GetEmployeeByCode(code);

            if (emp == null)
            {
                lblError.Text = "Ошибка при получении данных";
                return;
            }

            lblError.Text = "";

            switch (emp.Role)
            {
                case "general":
                    GeneralWindow generalWin = new GeneralWindow(emp);
                    generalWin.Show();
                    this.Close();
                    break;

                case "security":
                    SecurityWindow securityWin = new SecurityWindow(emp);
                    securityWin.Show();
                    this.Close();
                    break;

                case "department":
                    DepartmentWindow deptWin = new DepartmentWindow(emp);
                    deptWin.Show();
                    this.Close();
                    break;

                default:
                    MessageBox.Show($"Роль '{emp.Role}' не распознана", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }
        }
    }
}