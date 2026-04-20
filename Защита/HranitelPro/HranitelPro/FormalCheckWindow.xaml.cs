using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace HranitelPro
{
    public partial class FormalCheckWindow : Window
    {
        private DatabaseHelper db = new DatabaseHelper();
        private int requestId;
        private bool isInBlacklist = false;
        private DataRow requestData;

        public FormalCheckWindow(int id)
        {
            InitializeComponent();
            requestId = id;
            LoadRequest();
            CheckBlacklist();
            LoadFiles();

            dpVisitDate.SelectedDate = DateTime.Now.AddDays(1);
        }

        private void LoadRequest()
        {
            // Получаем данные заявки
            requestData = db.GetRequestByIdAsDataRow(requestId);

            if (requestData != null)
            {
                // Информация о заявке
                string requestType = requestData["requesttype"]?.ToString() ?? "личная";
                string status = requestData["status"]?.ToString() ?? "проверка";
                string startDate = Convert.ToDateTime(requestData["startdate"]).ToShortDateString();
                string endDate = Convert.ToDateTime(requestData["enddate"]).ToShortDateString();
                string purpose = requestData["visitpurpose"]?.ToString() ?? "";
                string department = requestData["targetdepartment"]?.ToString() ?? "";
                string note = requestData["note"]?.ToString() ?? "";

                // Информация о посетителе
                string visitorLastName = requestData["visitor_lastname"]?.ToString() ?? "";
                string visitorFirstName = requestData["visitor_firstname"]?.ToString() ?? "";
                string visitorPatronymic = requestData["visitor_patronymic"]?.ToString() ?? "";
                string visitorPhone = requestData["visitor_phone"]?.ToString() ?? "";
                string visitorEmail = requestData["visitor_email"]?.ToString() ?? "";
                string visitorOrganization = requestData["visitor_organization"]?.ToString() ?? "";
                string visitorBirthDate = requestData["visitor_birthdate"] != DBNull.Value ?
                    Convert.ToDateTime(requestData["visitor_birthdate"]).ToShortDateString() : "не указана";
                string visitorPassport = requestData["visitor_passportdata"]?.ToString() ?? "";

                // Формируем текст для отображения
                txtInfo.Text = $"📋 Информация о заявке:\n" +
                              $"  • Номер заявки: {requestId}\n" +
                              $"  • Тип: {requestType}\n" +
                              $"  • Статус: {status}\n" +
                              $"  • Дата посещения: с {startDate} по {endDate}\n" +
                              $"  • Цель: {purpose}\n" +
                              $"  • Подразделение: {department}\n" +
                              $"  • Примечание: {note}\n\n" +
                              $"👤 Информация о посетителе:\n" +
                              $"  • ФИО: {visitorLastName} {visitorFirstName} {visitorPatronymic}\n" +
                              $"  • Телефон: {(string.IsNullOrEmpty(visitorPhone) ? "не указан" : visitorPhone)}\n" +
                              $"  • Email: {visitorEmail}\n" +
                              $"  • Организация: {(string.IsNullOrEmpty(visitorOrganization) ? "не указана" : visitorOrganization)}\n" +
                              $"  • Дата рождения: {visitorBirthDate}\n" +
                              $"  • Паспортные данные: {(string.IsNullOrEmpty(visitorPassport) ? "не указаны" : visitorPassport)}";
            }
            else
            {
                txtInfo.Text = "Ошибка: заявка не найдена";
            }
        }

        private void CheckBlacklist()
        {
            if (requestData != null)
            {
                string lastName = requestData["visitor_lastname"]?.ToString() ?? "";
                string firstName = requestData["visitor_firstname"]?.ToString() ?? "";
                string passport = requestData["visitor_passportdata"]?.ToString() ?? "";

                // Проверка в черном списке
                isInBlacklist = db.IsInBlacklist(lastName, firstName, passport);

                if (isInBlacklist)
                {
                    lblBlacklistMsg.Text = "⚠️ ПОСЕТИТЕЛЬ В ЧЕРНОМ СПИСКЕ! Заявка автоматически отклонена.";
                    lblBlacklistMsg.Foreground = System.Windows.Media.Brushes.Red;
                    cmbStatus.IsEnabled = false;
                    cmbStatus.SelectedIndex = 1; // отклонена
                    dpVisitDate.IsEnabled = false;
                    txtVisitTime.IsEnabled = false;

                    // Автоматически отклоняем заявку
                    db.UpdateRequestStatus(requestId, "отклонена");
                }
                else
                {
                    lblBlacklistMsg.Text = "✅ Черный список: записей не найдено";
                    lblBlacklistMsg.Foreground = System.Windows.Media.Brushes.Green;
                    cmbStatus.IsEnabled = true;
                }
            }
        }

        private void LoadFiles()
        {
            var files = db.GetAttachedFiles(requestId);
            lbFiles.Items.Clear();
            if (files.Count == 0)
            {
                lbFiles.Items.Add("Нет прикрепленных файлов");
            }
            else
            {
                foreach (var f in files)
                {
                    lbFiles.Items.Add($"📄 {f.FileName}");
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (isInBlacklist)
            {
                lblError.Text = "Невозможно изменить статус - посетитель в черном списке!";
                return;
            }

            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(status))
            {
                lblError.Text = "Выберите статус";
                return;
            }

            try
            {
                if (status == "одобрена")
                {
                    DateTime date = dpVisitDate.SelectedDate ?? DateTime.Now;
                    string time = txtVisitTime.Text;

                    // Обновляем статус
                    db.UpdateRequestStatus(requestId, "одобрена");

                    // Обновляем даты посещения
                    db.UpdateVisitDateTime(requestId, date, date);

                    MessageBox.Show($"Заявка одобрена!\nДата посещения: {date:dd.MM.yyyy}\nВремя: {time}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (status == "отклонена")
                {
                    db.UpdateRequestStatus(requestId, "отклонена");
                    MessageBox.Show("Заявка отклонена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}