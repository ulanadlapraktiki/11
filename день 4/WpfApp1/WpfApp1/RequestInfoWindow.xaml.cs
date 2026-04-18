using System;
using System.Data;
using System.Windows;
using WpfApp1;

namespace WpfApp1
{
    public partial class RequestInfoWindow : Window
    {
        public RequestInfoWindow(Guid requestId)
        {
            InitializeComponent();

            DatabaseHelper db = new DatabaseHelper();
            var request = db.GetRequestById(requestId);

            if (request != null)
            {
                txtInfo.Text = $"👤 Посетитель: {request["last_name"]} {request["first_name"]} {request["middle_name"]}\n" +
                              $"📧 Email: {request["email"]}\n" +
                              $"📞 Телефон: {request["phone"]}\n" +
                              $"🆔 Паспорт: {request["passport_series"]} {request["passport_number"]}\n" +
                              $"📅 Дата рождения: {Convert.ToDateTime(request["birth_date"]):dd.MM.yyyy}\n" +
                              $"📅 Даты посещения: {Convert.ToDateTime(request["start_date"]):dd.MM.yyyy} - {Convert.ToDateTime(request["end_date"]):dd.MM.yyyy}\n" +
                              $"🏢 Подразделение: {request["target_department"]}\n" +
                              $"🎯 Цель: {request["visit_purpose"]}\n" +
                              $"📝 Примечание: {request["note"]}\n" +
                              $"🕐 Время входа: {(request["entry_time"] == DBNull.Value ? "—" : Convert.ToDateTime(request["entry_time"]).ToString("HH:mm:ss"))}\n" +
                              $"🕐 Время выхода: {(request["exit_time"] == DBNull.Value ? "—" : Convert.ToDateTime(request["exit_time"]).ToString("HH:mm:ss"))}";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}