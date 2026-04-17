using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class GroupRequestWindow : Window
    {
        private AppDbContext db = new AppDbContext();
        private string currentUser = "";
        private string passportFilePath = "";
        private ObservableCollection<GroupMember> members = new ObservableCollection<GroupMember>();
        private int nextNumber = 1;

        public class GroupMember : INotifyPropertyChanged
        {
            public int Number { get; set; }
            public string LastName { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string MiddleName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhotoPath { get; set; } = "";

            public string FullName => $"{LastName} {FirstName?[0]}. {(string.IsNullOrEmpty(MiddleName) ? "" : MiddleName?[0] + ".")}";
            public string Contacts => $"тел. {Phone}, email: {Email}";

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public GroupRequestWindow(string login)
        {
            InitializeComponent();
            currentUser = login;

            btnSave.Click += BtnSave_Click;
            btnClear.Click += BtnClear_Click;
            btnDownloadTemplate.Click += BtnDownloadTemplate_Click;
            btnUploadList.Click += BtnUploadList_Click;
            btnAddMember.Click += BtnAddMember_Click;
            btnUploadPassport.Click += BtnUploadPassport_Click;

            LoadDepartments();
            LoadEmployees();

            cbDepartment.SelectionChanged += CbDepartment_SelectionChanged;

            dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
            dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
            dpBirthDate.SelectedDate = DateTime.Now.AddYears(-20);

            dgvMembers.ItemsSource = members;
            UpdateMemberCount();
        }

        private void LoadDepartments()
        {
            cbDepartment.Items.Clear();
            cbDepartment.Items.Add("IT отдел");
            cbDepartment.Items.Add("Бухгалтерия");
            cbDepartment.Items.Add("Отдел кадров");
            cbDepartment.Items.Add("Производственный отдел");
            cbDepartment.Items.Add("Лаборатория");
            cbDepartment.Items.Add("Склад");
            cbDepartment.Items.Add("Администрация");
            cbDepartment.SelectedIndex = 0;
        }

        private void LoadEmployees()
        {
            cbEmployee.Items.Clear();
            cbEmployee.Items.Add("Иванов Иван Иванович");
            cbEmployee.Items.Add("Петрова Мария Сергеевна");
            cbEmployee.Items.Add("Сидоров Алексей Владимирович");
            cbEmployee.SelectedIndex = 0;
        }

        private void CbDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadEmployees();
        }

        private void UpdateMemberCount()
        {
            lblMemberCount.Text = $"Всего: {members.Count} человек";
        }

        private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV файлы (*.csv)|*.csv";
            saveDialog.FileName = "Шаблон_списка_посетителей.csv";

            if (saveDialog.ShowDialog() == true)
            {
                string template = "№;Фамилия;Имя;Отчество;Телефон;Email\n" +
                                 "1;Иванов;Иван;Иванович;+7 (999) 123-45-67;ivanov@example.com\n" +
                                 "2;Петрова;Мария;Сергеевна;+7 (999) 765-43-21;petrova@example.com";

                File.WriteAllText(saveDialog.FileName, template, Encoding.UTF8);
                MessageBox.Show("Шаблон сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnUploadList_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt";
            openDialog.Title = "Выберите файл со списком посетителей";

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openDialog.FileName, Encoding.UTF8);
                    members.Clear();
                    nextNumber = 1;

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(';');
                        if (parts.Length >= 3)
                        {
                            var member = new GroupMember
                            {
                                Number = nextNumber++,
                                LastName = parts.Length > 1 ? parts[1].Trim() : "",
                                FirstName = parts.Length > 2 ? parts[2].Trim() : "",
                                MiddleName = parts.Length > 3 ? parts[3].Trim() : "",
                                Phone = parts.Length > 4 ? parts[4].Trim() : "",
                                Email = parts.Length > 5 ? parts[5].Trim() : ""
                            };

                            if (!string.IsNullOrEmpty(member.LastName) && !string.IsNullOrEmpty(member.FirstName))
                            {
                                members.Add(member);
                            }
                        }
                    }

                    dgvMembers.Items.Refresh();
                    UpdateMemberCount();
                    MessageBox.Show($"Загружено {members.Count} человек", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                var member = new GroupMember
                {
                    Number = nextNumber++,
                    LastName = dialog.LastName,
                    FirstName = dialog.FirstName,
                    MiddleName = dialog.MiddleName,
                    Phone = dialog.Phone,
                    Email = dialog.Email
                };
                members.Add(member);
                dgvMembers.Items.Refresh();
                UpdateMemberCount();
            }
        }

        // ИСПРАВЛЕНО: имя метода совпадает с XAML
        private void BtnUploadMemberPhoto_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var member = button?.Tag as GroupMember;

            if (member != null)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "JPG файлы (*.jpg)|*.jpg";
                dialog.Title = $"Выберите фото для {member.FullName}";

                if (dialog.ShowDialog() == true)
                {
                    member.PhotoPath = dialog.FileName;
                    MessageBox.Show($"Фото добавлено для {member.FullName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // ИСПРАВЛЕНО: имя метода совпадает с XAML
        private void BtnDeleteMember_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var member = button?.Tag as GroupMember;

            if (member != null && MessageBox.Show($"Удалить {member.FullName}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                members.Remove(member);
                for (int i = 0; i < members.Count; i++) members[i].Number = i + 1;
                nextNumber = members.Count + 1;
                dgvMembers.Items.Refresh();
                UpdateMemberCount();
            }
        }

        private void BtnUploadPassport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "PDF файлы (*.pdf)|*.pdf";

            if (dialog.ShowDialog() == true)
            {
                passportFilePath = dialog.FileName;
                lblPassportPath.Text = Path.GetFileName(passportFilePath);
                lblPassportPath.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить все поля?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
                dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
                txtPurpose.Text = "";
                cbDepartment.SelectedIndex = 0;
                cbEmployee.SelectedIndex = 0;
                txtLastName.Text = "";
                txtFirstName.Text = "";
                txtPatronymic.Text = "";
                txtPhone.Text = "";
                txtEmail.Text = "";
                txtOrganization.Text = "";
                txtNote.Text = "";
                dpBirthDate.SelectedDate = DateTime.Now.AddYears(-20);
                txtPassportSeries.Text = "";
                txtPassportNumber.Text = "";
                members.Clear();
                nextNumber = 1;
                passportFilePath = "";
                lblPassportPath.Text = "Файл не выбран";
                lblPassportPath.Foreground = System.Windows.Media.Brushes.Gray;
                lblMessage.Text = "";
                UpdateMemberCount();
                dgvMembers.Items.Refresh();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверки
            if (!dpStartDate.SelectedDate.HasValue) { lblMessage.Text = "Выберите дату начала"; return; }
            if (!dpEndDate.SelectedDate.HasValue) { lblMessage.Text = "Выберите дату окончания"; return; }
            if (dpStartDate.SelectedDate.Value < DateTime.Now.Date.AddDays(1)) { lblMessage.Text = "Дата начала не ранее завтрашнего дня"; return; }
            if (string.IsNullOrWhiteSpace(txtPurpose.Text)) { lblMessage.Text = "Введите цель посещения"; return; }
            if (cbDepartment.SelectedItem == null) { lblMessage.Text = "Выберите подразделение"; return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text)) { lblMessage.Text = "Введите ФИО организатора"; return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@')) { lblMessage.Text = "Введите корректный email"; return; }
            if (!dpBirthDate.SelectedDate.HasValue) { lblMessage.Text = "Введите дату рождения"; return; }

            int age = DateTime.Now.Year - dpBirthDate.SelectedDate.Value.Year;
            if (dpBirthDate.SelectedDate.Value.Date > DateTime.Now.AddYears(-age)) age--;
            if (age < 16) { lblMessage.Text = "Организатор должен быть старше 16 лет"; return; }

            if (members.Count < 1) { lblMessage.Text = "Добавьте хотя бы одного посетителя"; return; }
            if (string.IsNullOrEmpty(passportFilePath)) { lblMessage.Text = "Прикрепите скан паспорта организатора"; return; }

            try
            {
                Guid employeeId = db.GetEmployeeId(currentUser);

                var organizer = new Guest
                {
                    LastName = txtLastName.Text,
                    FirstName = txtFirstName.Text,
                    MiddleName = string.IsNullOrEmpty(txtPatronymic.Text) ? null : txtPatronymic.Text,
                    PassportSeries = txtPassportSeries.Text,
                    PassportNumber = txtPassportNumber.Text,
                    BirthDate = dpBirthDate.SelectedDate.Value,
                    Phone = string.IsNullOrEmpty(txtPhone.Text) ? null : txtPhone.Text,
                    Email = txtEmail.Text,
                    Organization = string.IsNullOrEmpty(txtOrganization.Text) ? null : txtOrganization.Text
                };

                Guid organizerId = db.AddGuest(organizer);

                var request = new Request
                {
                    GuestId = organizerId,
                    EmployeeId = employeeId,
                    StartDate = dpStartDate.SelectedDate.Value,
                    EndDate = dpEndDate.SelectedDate.Value,
                    VisitPurpose = txtPurpose.Text,
                    TargetDepartment = cbDepartment.SelectedItem.ToString(),
                    Note = txtNote.Text,
                    Status = "проверка"
                };

                Guid requestId = db.CreateRequest(request);

                SaveFile(requestId, "passport_scan", passportFilePath);

                foreach (var member in members)
                {
                    var guest = new Guest
                    {
                        LastName = member.LastName,
                        FirstName = member.FirstName,
                        MiddleName = string.IsNullOrEmpty(member.MiddleName) ? null : member.MiddleName,
                        Phone = string.IsNullOrEmpty(member.Phone) ? null : member.Phone,
                        Email = member.Email,
                        BirthDate = DateTime.Now.AddYears(-20)
                    };

                    Guid guestId = db.AddGuest(guest);
                    db.AddGroupMember(requestId, guestId);

                    if (!string.IsNullOrEmpty(member.PhotoPath))
                    {
                        SaveFile(requestId, $"member_photo_{guestId}", member.PhotoPath);
                    }
                }

                MessageBox.Show($"Заявка успешно создана!\nГруппа: {members.Count} человек", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblMessage.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void SaveFile(Guid requestId, string fileType, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string uploadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", requestId.ToString());
            string destPath = Path.Combine(uploadDir, fileName);

            Directory.CreateDirectory(uploadDir);
            File.Copy(filePath, destPath, true);
            db.AddAttachedFile(requestId, fileType, destPath, fileName);
        }
    }
}