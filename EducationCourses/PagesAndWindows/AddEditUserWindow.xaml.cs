using EducationCourses.Connect;
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

namespace EducationCourses.PagesAndWindows
{
    /// <summary>
    /// Логика взаимодействия для AddEditUserWindow.xaml
    /// </summary>
    public partial class AddEditUserWindow : Window
    {
        private Users _user;
        private bool _isEditMode;

        public AddEditUserWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            Title = "Добавление пользователя";
        }

        public AddEditUserWindow(Users user) : this()
        {
            _user = user;
            _isEditMode = true;
            Title = "Редактирование пользователя";
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (_user != null)
            {
                txtFullName.Text = _user.FullName;
                txtLogin.Text = _user.Login;
                txtEmail.Text = _user.Email;
                txtPhone.Text = _user.Phone;

                // Устанавливаем выбранную роль
                foreach (ComboBoxItem item in cmbRole.Items)
                {
                    if (item.Tag.ToString() == _user.Role)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    ShowError("Введите ФИО!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLogin.Text))
                {
                    ShowError("Введите логин!");
                    return;
                }

                if (!_isEditMode && string.IsNullOrEmpty(txtPassword.Password))
                {
                    ShowError("Введите пароль!");
                    return;
                }

                if (!IsValidEmail(txtEmail.Text))
                {
                    ShowError("Введите корректный email!");
                    return;
                }

                // ИСПРАВЛЕНИЕ: Заменяем проблемную строку
                int currentUserId = 0;
                if (_user != null)
                {
                    currentUserId = _user.UserId;
                }

                // Проверка уникальности логина
                var existingUser = Connection.entities.Users
                    .FirstOrDefault(u => u.Login == txtLogin.Text && u.UserId != currentUserId);

                if (existingUser != null)
                {
                    ShowError("Пользователь с таким логином уже существует!");
                    return;
                }

                if (_isEditMode)
                {
                    // Редактирование существующего пользователя
                    _user.FullName = txtFullName.Text.Trim();
                    _user.Login = txtLogin.Text.Trim();
                    _user.Email = txtEmail.Text.Trim();
                    _user.Phone = txtPhone.Text.Trim();
                    _user.Role = (cmbRole.SelectedItem as ComboBoxItem).Tag.ToString();

                    if (!string.IsNullOrEmpty(txtPassword.Password))
                    {
                        _user.Password = txtPassword.Password;
                    }
                }
                else
                {
                    // Добавление нового пользователя
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

                    Connection.entities.Users.Add(newUser);
                }

                Connection.entities.SaveChanges();

                txtMessage.Text = _isEditMode ? "Пользователь успешно обновлен!" : "Пользователь успешно добавлен!";
                txtMessage.Foreground = System.Windows.Media.Brushes.Green;

                // Закрываем окно через 1 секунду
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
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
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            txtMessage.Text = message;
            txtMessage.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
