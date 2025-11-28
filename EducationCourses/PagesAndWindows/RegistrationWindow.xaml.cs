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
using EducationCourses.Connect;

namespace EducationCourses.PagesAndWindows
{
    /// <summary>
    /// Логика взаимодействия для RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    txtMessage.Text = "Введите ФИО!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLogin.Text))
                {
                    txtMessage.Text = "Введите логин!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (txtPassword.Password.Length < 3)
                {
                    txtMessage.Text = "Пароль должен содержать минимум 3 символа!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (txtPassword.Password != txtConfirmPassword.Password)
                {
                    txtMessage.Text = "Пароли не совпадают!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (!IsValidEmail(txtEmail.Text))
                {
                    txtMessage.Text = "Введите корректный email!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Проверка существующего логина
                var existingUser = Connect.Connection.entities.Users.FirstOrDefault(u => u.Login == txtLogin.Text);
                if (existingUser != null)
                {
                    txtMessage.Text = "Пользователь с таким логином уже существует!";
                    txtMessage.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Создание нового пользователя
                var newUser = new Users
                {
                    FullName = txtFullName.Text.Trim(),
                    Login = txtLogin.Text.Trim(),
                    Password = txtPassword.Password,
                    Email = txtEmail.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Role = (cmbRole.SelectedItem as ComboBoxItem).Tag.ToString(),
                    RegistrationDate = DateTime.Now
                };

                Connect.Connection.entities.Users.Add(newUser);
                Connect.Connection.entities.SaveChanges();

                txtMessage.Text = "Регистрация успешна! Теперь вы можете войти в систему.";
                txtMessage.Foreground = System.Windows.Media.Brushes.Green;

                // Автоматическое закрытие через 2 секунды
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => this.Close());
                });
            }
            catch (Exception ex)
            {
                txtMessage.Text = $"Ошибка регистрации: {ex.Message}";
                txtMessage.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
