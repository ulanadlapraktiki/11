using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using WpfApp1;

namespace WpfApp1
{
    public partial class FormalCheckWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private Guid requestId;

        public FormalCheckWindow(Guid id)
        {
            InitializeComponent();
            requestId = id;
            LoadRequest();
            CheckBlacklist();

            dpVisitDate.SelectedDate = DateTime.Now.AddDays(1);
        }

        private void LoadRequest()
        {
            var request = db.GetRequestById(requestId);
            if (request != null)
            {
                txtInfo.Text = $"👤 Посетитель: {request["last_name"]} {request["first_name"]} {request["middle_name"]}\n" +
                              $"📧 Email: {request["email"]}\n" +
                              $"📞 Телефон: {request["phone"]}\n" +
                              $"🆔 Паспорт: {request["passport_series"]} {request["passport_number"]}\n" +
                              $"📅 Дата рождения: {Convert.ToDateTime(request["birth_date"]):dd.MM.yyyy}\n" +
                              $"🏢 Подразделение: {request["target_department"]}\n" +
                              $"🎯 Цель: {request["visit_purpose"]}\n" +
                              $"📝 Примечание: {request["note"]}";
            }
        }

        private void CheckBlacklist()
        {
            var request = db.GetRequestById(requestId);
            if (request != null)
            {
                bool inBlacklist = db.IsInBlacklist(
                    request["last_name"]?.ToString() ?? "",
                    request["first_name"]?.ToString() ?? "",
                    request["passport_number"]?.ToString() ?? "");

                if (inBlacklist)
                {
                    lblBlacklistMsg.Text = "⚠️ ПОСЕТИТЕЛЬ В ЧЕРНОМ СПИСКЕ! Заявка автоматически отклонена.";
                    lblBlacklistMsg.Foreground = System.Windows.Media.Brushes.Red;
                    cmbStatus.IsEnabled = false;
                    cmbStatus.SelectedIndex = 1;
                }
                else
                {
                    lblBlacklistMsg.Text = "✅ Черный список: записей не найдено";
                    lblBlacklistMsg.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
            string message = "";

            if (status == "одобрена")
            {
                DateTime date = dpVisitDate.SelectedDate ?? DateTime.Now;
                string time = txtVisitTime.Text;
                message = $"Заявка одобрена. Дата: {date:dd.MM.yyyy}, Время: {time}";
            }
            else
            {
                message = "Заявка отклонена";
            }

            db.UpdateRequestStatus(requestId, status, message);
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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