using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EducationCourses.Connect;
using System.Windows.Shapes;

namespace EducationCourses.PagesAndWindows
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Тестовые данные для быстрой проверки
            txtLogin.Text = "admin";
            txtPassword.Password = "123";
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                txtMessage.Text = "Введите логин и пароль!";
                return;
            }

            try
            {
                // Проверяем подключение к базе данных
                if (Connection.entities == null)
                {
                    txtMessage.Text = "Ошибка подключения к базе данных!";
                    return;
                }

                // Проверяем, доступна ли база данных
                bool databaseExists;
                try
                {
                    databaseExists = Connection.entities.Database.Exists();
                }
                catch
                {
                    databaseExists = false;
                }

                if (!databaseExists)
                {
                    txtMessage.Text = "База данных не доступна! Проверьте подключение.";
                    return;
                }

                var user = Connection.entities.Users.FirstOrDefault(u =>
                           u.Login == login && u.Password == password);

                if (user != null)
                {
                    // Создаем копию пользователя, чтобы избежать проблем с контекстом
                    var currentUser = new Users
                    {
                        UserId = user.UserId,
                        Login = user.Login,
                        Password = user.Password,
                        Role = user.Role,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        RegistrationDate = user.RegistrationDate
                    };

                    MainWindow mainWindow = new MainWindow(currentUser);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    txtMessage.Text = "Неверный логин или пароль!";
                }
            }
            catch (System.Exception ex)
            {
                txtMessage.Text = $"Ошибка подключения к базе данных: {ex.Message}";
                // Для отладки - показываем полную информацию об ошибке
                MessageBox.Show($"Полная ошибка: {ex.ToString()}", "Ошибка подключения",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow regWindow = new RegistrationWindow();
            regWindow.Owner = this;
            regWindow.ShowDialog();
        }
    }
}
