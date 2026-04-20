using System.Windows;

namespace HranitelPro
{
    public partial class AddMemberDialog : Window
    {
        public string LastName { get; private set; } = "";
        public string FirstName { get; private set; } = "";
        public string Phone { get; private set; } = "";
        public string Email { get; private set; } = "";

        public AddMemberDialog()
        {
            InitializeComponent();
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            LastName = txtLastName.Text.Trim();
            FirstName = txtFirstName.Text.Trim();
            Phone = txtPhone.Text.Trim();
            Email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(LastName)) { lblError.Text = "Введите фамилию"; return; }
            if (string.IsNullOrWhiteSpace(FirstName)) { lblError.Text = "Введите имя"; return; }
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            { lblError.Text = "Введите корректный email"; return; }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}