using System;
using System.Windows;
using WpfApp1;

namespace WpfApp1
{
    public partial class AccessWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Guid requestId;

        public AccessWindow(Guid id, string visitorName)
        {
            InitializeComponent();
            requestId = id;
            txtVisitorName.Text = visitorName;
        }

        private void BtnAllow_Click(object sender, RoutedEventArgs e)
        {
            db.PlaySystemSound();
            db.SendGateOpenSignal(requestId);
            db.LogEntryTime(requestId, DateTime.Now);

            if (!string.IsNullOrWhiteSpace(txtExitTime.Text))
            {
                if (TimeSpan.TryParse(txtExitTime.Text, out TimeSpan exitTime))
                {
                    db.LogExitTime(requestId, DateTime.Today.Add(exitTime));
                    lblMessage.Text = $"✅ Доступ разрешён! Выход в {exitTime:hh\\:mm}";
                }
                else
                {
                    lblMessage.Text = "✅ Доступ разрешён!";
                }
            }
            else
            {
                lblMessage.Text = "✅ Доступ разрешён!";
            }

            DialogResult = true;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Tick += (s, args) => { timer.Stop(); this.Close(); };
            timer.Start();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}