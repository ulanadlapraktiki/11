using System;
using System.Data;
using System.Windows;

namespace HranitelPro
{
    public partial class RequestWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private int currentUserId;
        private int? editRequestId;
        
        public RequestWindow(int userId, int? requestId = null)
        {
            InitializeComponent();
            currentUserId = userId;
            editRequestId = requestId;
            
            btnSubmit.Click += BtnSubmit_Click;
            btnClear.Click += BtnClear_Click;
            btnCancel.Click += BtnCancel_Click;
            
            LoadDepartments();
            cmbDepartment.SelectionChanged += CmbDepartment_SelectionChanged;
            
            dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
            dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
            dpBirthDate.SelectedDate = DateTime.Now.AddYears(-20);
            
            if (requestId.HasValue)
            {
                Title = "✏️ Редактирование заявки";
                LoadRequestData(requestId.Value);
            }
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
        
        private void LoadRequestData(int requestId)
        {
            var req = db.GetRequestById(requestId);
            if (req != null)
            {
                dpStartDate.SelectedDate = req.StartDate;
                dpEndDate.SelectedDate = req.EndDate;
                txtPurpose.Text = req.VisitPurpose;
                txtLastName.Text = req.VisitorLastName;
                txtFirstName.Text = req.VisitorFirstName;
                txtPatronymic.Text = req.VisitorPatronymic;
                txtPhone.Text = req.VisitorPhone;
                txtEmail.Text = req.VisitorEmail;
                txtOrganization.Text = req.VisitorOrganization;
                txtNote.Text = req.Note;
                dpBirthDate.SelectedDate = req.VisitorBirthDate;
                if (req.VisitorPassportData?.Length >= 10)
                {
                    txtPassportSeries.Text = req.VisitorPassportData.Substring(0, 4);
                    txtPassportNumber.Text = req.VisitorPassportData.Substring(4, 6);
                }
            }
        }
        
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {

            if (!dpStartDate.SelectedDate.HasValue) { lblStatus.Text = "Выберите дату начала"; return; }
            if (!dpEndDate.SelectedDate.HasValue) { lblStatus.Text = "Выберите дату окончания"; return; }
            if (string.IsNullOrWhiteSpace(txtPurpose.Text)) { lblStatus.Text = "Введите цель посещения"; return; }
            if (cmbDepartment.SelectedItem == null) { lblStatus.Text = "Выберите подразделение"; return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtFirstName.Text)) 
            { lblStatus.Text = "Введите ФИО"; return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@")) 
            { lblStatus.Text = "Введите корректный email"; return; }
            if (!dpBirthDate.SelectedDate.HasValue) { lblStatus.Text = "Введите дату рождения"; return; }
            
            int age = DateTime.Now.Year - dpBirthDate.SelectedDate.Value.Year;
            if (dpBirthDate.SelectedDate.Value.Date > DateTime.Now.AddYears(-age)) age--;
            if (age < 16) { lblStatus.Text = "Посетитель должен быть старше 16 лет"; return; }
            
            if (txtPassportSeries.Text.Length != 4) { lblStatus.Text = "Серия паспорта: 4 цифры"; return; }
            if (txtPassportNumber.Text.Length != 6) { lblStatus.Text = "Номер паспорта: 6 цифр"; return; }
            

            string passportData = txtPassportSeries.Text + txtPassportNumber.Text;


            try
            {
                if (editRequestId.HasValue)
                {
                    db.UpdateRequest(editRequestId.Value,
                        dpStartDate.SelectedDate.Value, dpEndDate.SelectedDate.Value,
                        txtPurpose.Text, cmbDepartment.SelectedItem.ToString(), 0, txtNote.Text,
                        txtLastName.Text, txtFirstName.Text, txtPatronymic.Text,
                        txtPhone.Text, txtEmail.Text, txtOrganization.Text,
                        dpBirthDate.SelectedDate.Value, passportData);
                }
                else
                {
                    db.CreateRequest(currentUserId,
                        dpStartDate.SelectedDate.Value, dpEndDate.SelectedDate.Value,
                        txtPurpose.Text, cmbDepartment.SelectedItem.ToString(), 0, txtNote.Text,
                        txtLastName.Text, txtFirstName.Text, txtPatronymic.Text,
                        txtPhone.Text, txtEmail.Text, txtOrganization.Text,
                        dpBirthDate.SelectedDate.Value, passportData);
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
            }
        }


        

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            dpStartDate.SelectedDate = DateTime.Now.AddDays(1);
            dpEndDate.SelectedDate = DateTime.Now.AddDays(2);
            txtPurpose.Text = "";
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
            lblStatus.Text = "";
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}