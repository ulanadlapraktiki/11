using System;
using System.Windows;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class RequestWindow : Window
    {
        private AppDbContext db = new AppDbContext();
        private string currentUser = "";
        private Guid? editRequestId = null;

        public RequestWindow(string login, Guid? requestId = null)
        {
            InitializeComponent();
            currentUser = login;
            editRequestId = requestId;

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;

            LoadDepartments();
            LoadEmployees();

            cbDepartment.SelectionChanged += CbDepartment_SelectionChanged;

            if (requestId.HasValue)
            {
                Title = "✏️ Редактирование заявки";
                LoadRequest(requestId.Value);
            }
            else
            {
                Title = "📝 Личное посещение";
                dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
                dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
                dpBirthDate.SelectedDate = DateTime.Now.AddYears(-20);
            }
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
            cbEmployee.Items.Add("Кузнецова Елена Андреевна");
            cbEmployee.Items.Add("Смирнов Дмитрий Петрович");
            cbEmployee.SelectedIndex = 0;
        }

        private void CbDepartment_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadEmployees();
        }

        private void LoadRequest(Guid requestId)
        {
            var request = db.GetRequestById(requestId);
            if (request != null)
            {
                dpStartDate.SelectedDate = request.StartDate;
                dpEndDate.SelectedDate = request.EndDate;
                txtPurpose.Text = request.VisitPurpose;
                cbDepartment.Text = request.TargetDepartment;
                txtLastName.Text = request.LastName;
                txtFirstName.Text = request.FirstName;
                txtPatronymic.Text = request.MiddleName;
                txtPhone.Text = request.Phone;
                txtEmail.Text = request.Email;
                txtOrganization.Text = request.Organization;
                txtNote.Text = request.Note;
                dpBirthDate.SelectedDate = request.BirthDate;
                txtPassportSeries.Text = request.PassportSeries;
                txtPassportNumber.Text = request.PassportNumber;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!dpStartDate.SelectedDate.HasValue)
            {
                lblMessage.Text = "Выберите дату начала";
                return;
            }

            if (!dpEndDate.SelectedDate.HasValue)
            {
                lblMessage.Text = "Выберите дату окончания";
                return;
            }

            if (dpStartDate.SelectedDate.Value < DateTime.Now.Date.AddDays(1))
            {
                lblMessage.Text = "Дата начала должна быть не ранее завтрашнего дня";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPurpose.Text))
            {
                lblMessage.Text = "Введите цель посещения";
                return;
            }

            if (cbDepartment.SelectedItem == null)
            {
                lblMessage.Text = "Выберите подразделение";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                lblMessage.Text = "Введите ФИО посетителя";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@'))
            {
                lblMessage.Text = "Введите корректный email";
                return;
            }

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                lblMessage.Text = "Введите дату рождения";
                return;
            }

            int age = DateTime.Now.Year - dpBirthDate.SelectedDate.Value.Year;
            if (dpBirthDate.SelectedDate.Value.Date > DateTime.Now.AddYears(-age)) age--;
            if (age < 16)
            {
                lblMessage.Text = "Посетитель должен быть старше 16 лет";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassportSeries.Text) || txtPassportSeries.Text.Length < 4)
            {
                lblMessage.Text = "Введите серию паспорта (4 символа)";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassportNumber.Text) || txtPassportNumber.Text.Length < 6)
            {
                lblMessage.Text = "Введите номер паспорта (6 символов)";
                return;
            }

            Guid employeeId = db.GetEmployeeId(currentUser);

            if (editRequestId.HasValue)
            {
                UpdateRequest(editRequestId.Value);
            }
            else
            {
                CreateRequest(employeeId);
            }

            DialogResult = true;
            Close();
        }

        private void CreateRequest(Guid employeeId)
        {
            var guest = new Guest
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

            Guid guestId = db.AddGuest(guest);

            var request = new Request
            {
                GuestId = guestId,
                EmployeeId = employeeId,
                StartDate = dpStartDate.SelectedDate.Value,
                EndDate = dpEndDate.SelectedDate.Value,
                VisitPurpose = txtPurpose.Text,
                TargetDepartment = cbDepartment.SelectedItem.ToString(),
                Note = txtNote.Text,
                Status = "проверка"
            };

            db.CreateRequest(request);
        }

        private void UpdateRequest(Guid requestId)
        {
            var request = db.GetRequestById(requestId);
            if (request != null)
            {
                request.StartDate = dpStartDate.SelectedDate.Value;
                request.EndDate = dpEndDate.SelectedDate.Value;
                request.VisitPurpose = txtPurpose.Text;
                request.TargetDepartment = cbDepartment.SelectedItem.ToString();
                request.Note = txtNote.Text;
                db.UpdateRequest(request);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}