using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;
using EducationCourses.Connect;

namespace EducationCourses.PagesAndWindows
{
    /// <summary>
    /// Логика взаимодействия для PaymentWindow.xaml
    /// </summary>
    public partial class PaymentWindow : Window
    {
        private Courses _course;
        private Users _student;

        public PaymentWindow(Courses course, Users student)
        {
            InitializeComponent();
            _course = course;
            _student = student;
            LoadCourseInfo();

            // Инициализируем видимость панели карты после загрузки компонентов
            this.Loaded += PaymentWindow_Loaded;
        }

        private void PaymentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальную видимость панели карты
            UpdateCardPanelVisibility();
        }

        private void LoadCourseInfo()
        {
            if (_course != null)
            {
                txtCourseName.Text = _course.Name;
                txtCourseDescription.Text = _course.Description;
                txtDuration.Text = $"{_course.Duration} часов";
                txtLevel.Text = _course.Level;
                txtPrice.Text = $"{_course.Price:C}";
            }
        }

        private void cmbPaymentMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCardPanelVisibility();
        }

        private void UpdateCardPanelVisibility()
        {
            // Добавляем проверку на null
            if (pnlCardInfo == null || cmbPaymentMethod == null) return;

            if (cmbPaymentMethod.SelectedItem != null)
            {
                var method = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (!string.IsNullOrEmpty(method))
                {
                    pnlCardInfo.Visibility = method == "Card" ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                // Если ничего не выбрано, скрываем панель карты
                pnlCardInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void txtCardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCardNumber == null) return;

            // Форматирование номера карты
            string text = txtCardNumber.Text.Replace(" ", "");
            if (text.Length > 0 && text.Length % 4 == 0 && text.Length < 16)
            {
                string formatted = "";
                for (int i = 0; i < text.Length; i += 4)
                {
                    if (i + 4 <= text.Length)
                    {
                        if (!string.IsNullOrEmpty(formatted)) formatted += " ";
                        formatted += text.Substring(i, 4);
                    }
                }
                txtCardNumber.Text = formatted + " ";
                txtCardNumber.CaretIndex = txtCardNumber.Text.Length;
            }
        }

        private bool ValidatePayment()
        {
            if (_student == null)
            {
                txtMessage.Text = "Ошибка: студент не определен!";
                txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }

            var method = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            if (string.IsNullOrEmpty(method))
            {
                txtMessage.Text = "Выберите способ оплаты!";
                txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }

            if (method == "Card")
            {
                if (string.IsNullOrWhiteSpace(txtCardNumber.Text) ||
                    txtCardNumber.Text.Replace(" ", "").Length != 16)
                {
                    txtMessage.Text = "Введите корректный номер карты (16 цифр)!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtExpiryDate.Text) ||
                    !Regex.IsMatch(txtExpiryDate.Text, @"^\d{2}/\d{2}$"))
                {
                    txtMessage.Text = "Введите срок действия в формате ММ/ГГ!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtCVV.Text) ||
                    !Regex.IsMatch(txtCVV.Text, @"^\d{3}$"))
                {
                    txtMessage.Text = "Введите корректный CVV код (3 цифры)!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }
            }

            return true;
        }

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePayment())
                return;

            try
            {
                // Проверяем, не записан ли уже студент на курс
                var existingRegistration = Connection.entities.Registrations
                    .FirstOrDefault(r => r.StudentId == _student.UserId &&
                                       r.CourseId == _course.CourseId &&
                                       r.Status == "Active");

                if (existingRegistration != null)
                {
                    txtMessage.Text = "Вы уже записаны на этот курс!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Проверяем наличие свободных мест
                if (_course.CurrentStudents >= _course.MaxStudents)
                {
                    txtMessage.Text = "На этот курс нет свободных мест!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Создаем регистрацию
                var registration = new Registrations
                {
                    StudentId = _student.UserId,
                    CourseId = _course.CourseId,
                    RegistrationDate = DateTime.Now,
                    Status = "Active"
                };

                Connection.entities.Registrations.Add(registration);
                Connection.entities.SaveChanges();

                // Создаем платеж
                var paymentMethod = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var payment = new Payments
                {
                    RegistrationId = registration.RegistrationId,
                    Amount = _course.Price,
                    PaymentMethod = paymentMethod,
                    PaymentDate = DateTime.Now
                };

                if (paymentMethod == "Card")
                {
                    payment.CardNumber = "****" + txtCardNumber.Text.Replace(" ", "").Substring(12);
                    payment.ExpiryDate = txtExpiryDate.Text;
                    payment.CVV = txtCVV.Text;
                }

                Connection.entities.Payments.Add(payment);

                // Обновляем количество студентов на курсе
                _course.CurrentStudents++;

                Connection.entities.SaveChanges();

                txtMessage.Text = "Оплата прошла успешно! Вы записаны на курс.";
                txtMessage.Foreground = System.Windows.Media.Brushes.Green;

                // Автоматическое закрытие через 3 секунды
                System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.DialogResult = true;
                        this.Close();
                    });
                });
            }
            catch (Exception ex)
            {
                txtMessage.Text = $"Ошибка оплаты: {ex.Message}";
                txtMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}
