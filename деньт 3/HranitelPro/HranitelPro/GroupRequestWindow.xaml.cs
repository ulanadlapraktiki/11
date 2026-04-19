using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace HranitelPro
{
    public partial class GroupRequestWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private int currentUserId;
        private ObservableCollection<GroupMember> members = new ObservableCollection<GroupMember>();
        private int nextNumber = 1;

        public class GroupMember : INotifyPropertyChanged
        {
            public int Number { get; set; }
            public string LastName { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public GroupRequestWindow(int userId)
        {
            InitializeComponent();
            currentUserId = userId;

            btnSubmit.Click += BtnSubmit_Click;
            btnCancel.Click += BtnCancel_Click;
            btnAddMember.Click += BtnAddMember_Click;

            LoadDepartments();
            cmbDepartment.SelectionChanged += CmbDepartment_SelectionChanged;

            dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
            dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
            dpBirthDate.SelectedDate = DateTime.Now.AddYears(-20);

            dgvMembers.ItemsSource = members;
            UpdateMemberCount();
        }

        private void LoadDepartments()
        {
            var dt = db.GetDepartments();
            cmbDepartment.Items.Clear();
            foreach (DataRow row in dt.Rows) cmbDepartment.Items.Add(row["department"].ToString());
            if (cmbDepartment.Items.Count > 0) cmbDepartment.SelectedIndex = 0;
        }

        private void CmbDepartment_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbDepartment.SelectedItem == null) return;
            string dept = cmbDepartment.SelectedItem.ToString();
            var dt = db.GetEmployees(dept);
            cmbEmployee.Items.Clear();
            foreach (DataRow row in dt.Rows) cmbEmployee.Items.Add(row["fullname"].ToString());
        }

        private void UpdateMemberCount()
        {
            lblMemberCount.Text = $"Всего: {members.Count} человек";
        }

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            AddMemberDialog dialog = new AddMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                members.Add(new GroupMember
                {
                    Number = nextNumber++,
                    LastName = dialog.LastName,
                    FirstName = dialog.FirstName,
                    Phone = dialog.Phone,
                    Email = dialog.Email
                });
                dgvMembers.Items.Refresh();
                UpdateMemberCount();
            }
        }

        private void RemoveMember_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var member = button?.Tag as GroupMember;
            if (member != null)
            {
                members.Remove(member);
                for (int i = 0; i < members.Count; i++) members[i].Number = i + 1;
                nextNumber = members.Count + 1;
                dgvMembers.Items.Refresh();
                UpdateMemberCount();
            }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Проверки
            if (!dpStartDate.SelectedDate.HasValue) { lblStatus.Text = "Выберите дату начала"; return; }
            if (!dpEndDate.SelectedDate.HasValue) { lblStatus.Text = "Выберите дату окончания"; return; }
            if (string.IsNullOrWhiteSpace(txtPurpose.Text)) { lblStatus.Text = "Введите цель посещения"; return; }
            if (cmbDepartment.SelectedItem == null) { lblStatus.Text = "Выберите подразделение"; return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text))
            { lblStatus.Text = "Введите ФИО организатора"; return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@"))
            { lblStatus.Text = "Введите корректный email организатора"; return; }
            if (!dpBirthDate.SelectedDate.HasValue) { lblStatus.Text = "Введите дату рождения организатора"; return; }

            int age = DateTime.Now.Year - dpBirthDate.SelectedDate.Value.Year;
            if (dpBirthDate.SelectedDate.Value.Date > DateTime.Now.AddYears(-age)) age--;
            if (age < 16) { lblStatus.Text = "Организатор должен быть старше 16 лет"; return; }

            if (txtPassportSeries.Text.Length != 4) { lblStatus.Text = "Серия паспорта: 4 цифры"; return; }
            if (txtPassportNumber.Text.Length != 6) { lblStatus.Text = "Номер паспорта: 6 цифр"; return; }

            if (members.Count < 1) { lblStatus.Text = "Добавьте хотя бы одного посетителя"; return; }

            string passportData = txtPassportSeries.Text + txtPassportNumber.Text;

            try
            {
                // Создаем заявку
                int requestId = db.CreateRequest(currentUserId,
                    dpStartDate.SelectedDate.Value, dpEndDate.SelectedDate.Value,
                    txtPurpose.Text, cmbDepartment.SelectedItem.ToString(), 0, txtNote.Text,
                    txtLastName.Text, txtFirstName.Text, txtPatronymic.Text,
                    txtPhone.Text, txtEmail.Text, txtOrganization.Text,
                    dpBirthDate.SelectedDate.Value, passportData);

                MessageBox.Show($"Групповая заявка создана!\nКоличество посетителей: {members.Count}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}