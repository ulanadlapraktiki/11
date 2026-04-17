using System.Windows;

namespace WpfApp2
{
    public partial class ChoiceWindow : Window
    {
        public RequestType SelectedType { get; private set; }

        public enum RequestType
        {
            None,
            Personal,
            Group
        }

        public ChoiceWindow()
        {
            InitializeComponent();
            SelectedType = RequestType.None;
        }

        private void BtnPersonal_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = RequestType.Personal;
            DialogResult = true;
            Close();
        }

        private void BtnGroup_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = RequestType.Group;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = RequestType.None;
            DialogResult = false;
            Close();
        }
    }
}