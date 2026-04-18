using System;
using System.Windows;
using WpfApp1;

namespace WpfApp1
{
    public partial class AddRequestWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Employee currentEmployee;

        public AddRequestWindow(Employee emp)
        {
            InitializeComponent();
            currentEmployee = emp;

            dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
            dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!dpStartDate.SelectedDate.HasValue) { lblMessage.Text = "Выберите дату начала"; return; }
            if (!dpEndDate.SelectedDate.HasValue) { lblMessage.Text = "Выберите дату окончания"; return; }
            if (string.IsNullOrWhiteSpace(txtPurpose.Text)) { lblMessage.Text = "Введите цель посещения"; return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text)) { lblMessage.Text = "Введите ФИО"; return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@")) { lblMessage.Text = "Введите корректный email"; return; }

            try
            {
                Guid guestId = db.AddGuest(
                    txtLastName.Text.Trim(),
                    txtFirstName.Text.Trim(),
                    txtMiddleName.Text.Trim(),
                    txtPassportSeries.Text.Trim(),
                    txtPassportNumber.Text.Trim(),
                    DateTime.Now.AddYears(-20),
                    txtPhone.Text.Trim(),
                    txtEmail.Text.Trim(),
                    "");

                Guid employeeId = db.GetGeneralDepartmentEmployeeId();

                db.CreateRequest(
                    guestId,
                    employeeId,
                    dpStartDate.SelectedDate.Value,
                    dpEndDate.SelectedDate.Value,
                    txtPurpose.Text.Trim(),
                    cbDepartment.Text,
                    "");

                MessageBox.Show("Заявка успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}