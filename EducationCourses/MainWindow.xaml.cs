using EducationCourses.PagesAndWindows;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using EducationCourses.Connect;
using System.Data.Entity;

namespace EducationCourses
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Users _currentUser;

        public MainWindow(Users user)
        {
            if (user == null)
            {
                MessageBox.Show("Ошибка: пользователь не определен. Возврат к окну входа.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }

            InitializeComponent();
            _currentUser = user;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            try
            {
                if (_currentUser == null) return;

                txtCurrentUser.Text = $"{_currentUser.FullName} ({_currentUser.Role})";

                if (_currentUser.Role != "Admin" && tabAdmin != null)
                {
                    tabAdmin.Visibility = Visibility.Collapsed;
                }

                if (_currentUser.Role != "Student" && tabMyCourses != null)
                {
                    tabMyCourses.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации интерфейса: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                if (Connection.entities == null)
                {
                    if (txtStatus != null)
                        txtStatus.Text = "Ошибка подключения к базе данных";
                    return;
                }

                // Загрузка курсов для основной вкладки
                var courses = Connection.entities.Courses
                    .Include(c => c.Users)
                    .Where(c => c.IsActive == true)
                    .ToList();

                List<int> userRegistrations = new List<int>();
                if (_currentUser != null && _currentUser.Role == "Student")
                {
                    userRegistrations = Connection.entities.Registrations
                        .Where(r => r.StudentId == _currentUser.UserId && r.Status == "Active")
                        .Select(r => r.CourseId)
                        .ToList();
                }

                foreach (var course in courses)
                {
                    bool canEnroll = course.CurrentStudents < course.MaxStudents &&
                                   _currentUser != null &&
                                   _currentUser.Role == "Student" &&
                                   !userRegistrations.Contains(course.CourseId);

                    course.CanEnroll = canEnroll;
                }

                if (dgCourses != null)
                {
                    dgCourses.ItemsSource = courses;
                    UpdateCoursesCount();
                }

                // Загрузка моих курсов (для студентов)
                if (_currentUser != null && _currentUser.Role == "Student" && dgMyCourses != null)
                {
                    var myCourses = Connection.entities.Registrations
                        .Include(r => r.Courses)
                        .Include(r => r.Payments)
                        .Where(r => r.StudentId == _currentUser.UserId)
                        .ToList();
                    dgMyCourses.ItemsSource = myCourses;
                }

                // Загрузка расписания
                if (dgSchedule != null)
                {
                    var schedule = Connection.entities.Schedule
                        .Include(s => s.Courses)
                        .Include(s => s.Courses.Users)
                        .ToList();
                    dgSchedule.ItemsSource = schedule;
                }

                // Загрузка данных для администратора
                if (_currentUser != null && _currentUser.Role == "Admin")
                {
                    // Пользователи
                    if (dgUsers != null)
                    {
                        var users = Connection.entities.Users.ToList();
                        dgUsers.ItemsSource = users;
                    }

                    // Все курсы для админки
                    if (dgAdminCourses != null)
                    {
                        var allCourses = Connection.entities.Courses
                            .Include(c => c.Users)
                            .ToList();
                        dgAdminCourses.ItemsSource = allCourses;
                    }

                    // Все регистрации
                    if (dgAllRegistrations != null)
                    {
                        var allRegistrations = Connection.entities.Registrations
                            .Include(r => r.Users)
                            .Include(r => r.Courses)
                            .Include(r => r.Payments)
                            .ToList();
                        dgAllRegistrations.ItemsSource = allRegistrations;
                    }
                }

                if (txtStatus != null)
                    txtStatus.Text = "Данные загружены успешно";
            }
            catch (Exception ex)
            {
                if (txtStatus != null)
                    txtStatus.Text = $"Ошибка загрузки данных: {ex.Message}";

                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ========== ОБРАБОТЧИКИ ДЛЯ АДМИНИСТРИРОВАНИЯ ==========

        // === Управление пользователями ===
        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            AddEditUserWindow userWindow = new AddEditUserWindow();
            userWindow.Owner = this;
            if (userWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void btnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as Users;
            AddEditUserWindow userWindow = new AddEditUserWindow(selectedUser);
            userWindow.Owner = this;
            if (userWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedUser = dgUsers.SelectedItem as Users;

            // Нельзя удалить самого себя
            if (selectedUser.UserId == _currentUser.UserId)
            {
                MessageBox.Show("Нельзя удалить собственный аккаунт!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Вы действительно хотите удалить пользователя {selectedUser.FullName}?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные записи
                    var userRegistrations = Connection.entities.Registrations
                        .Where(r => r.StudentId == selectedUser.UserId).ToList();
                    var taughtCourses = Connection.entities.Courses
                        .Where(c => c.TeacherId == selectedUser.UserId).ToList();

                    if (userRegistrations.Any() || taughtCourses.Any())
                    {
                        MessageBox.Show("Нельзя удалить пользователя, у которого есть регистрации на курсы или который преподает курсы!",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Connection.entities.Users.Remove(selectedUser);
                    Connection.entities.SaveChanges();
                    LoadData();

                    MessageBox.Show("Пользователь успешно удален!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // === Управление курсами ===
        private void btnAddCourse_Click(object sender, RoutedEventArgs e)
        {
            AddEditCourseWindow courseWindow = new AddEditCourseWindow();
            courseWindow.Owner = this;
            if (courseWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void btnEditCourse_Click(object sender, RoutedEventArgs e)
        {
            if (dgAdminCourses.SelectedItem == null)
            {
                MessageBox.Show("Выберите курс для редактирования!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCourse = dgAdminCourses.SelectedItem as Courses;
            AddEditCourseWindow courseWindow = new AddEditCourseWindow(selectedCourse);
            courseWindow.Owner = this;
            if (courseWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void btnDeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (dgAdminCourses.SelectedItem == null)
            {
                MessageBox.Show("Выберите курс для удаления!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCourse = dgAdminCourses.SelectedItem as Courses;

            var result = MessageBox.Show($"Вы действительно хотите удалить курс \"{selectedCourse.Name}\"?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли регистрации на этот курс
                    var courseRegistrations = Connection.entities.Registrations
                        .Where(r => r.CourseId == selectedCourse.CourseId).ToList();

                    if (courseRegistrations.Any())
                    {
                        MessageBox.Show("Нельзя удалить курс, на который есть регистрации!",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Connection.entities.Courses.Remove(selectedCourse);
                    Connection.entities.SaveChanges();
                    LoadData();

                    MessageBox.Show("Курс успешно удален!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении курса: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // === Управление регистрациями ===
        private void btnDeleteRegistration_Click(object sender, RoutedEventArgs e)
        {
            if (dgAllRegistrations.SelectedItem == null)
            {
                MessageBox.Show("Выберите регистрацию для удаления!", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedRegistration = dgAllRegistrations.SelectedItem as Registrations;

            var result = MessageBox.Show($"Вы действительно хотите удалить регистрацию студента {selectedRegistration.Users.FullName} на курс {selectedRegistration.Courses.Name}?",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем связанный платеж
                    var payment = Connection.entities.Payments
                        .FirstOrDefault(p => p.RegistrationId == selectedRegistration.RegistrationId);
                    if (payment != null)
                    {
                        Connection.entities.Payments.Remove(payment);
                    }

                    // Уменьшаем количество студентов на курсе
                    var course = Connection.entities.Courses
                        .FirstOrDefault(c => c.CourseId == selectedRegistration.CourseId);
                    if (course != null && course.CurrentStudents > 0)
                    {
                        course.CurrentStudents--;
                    }

                    Connection.entities.Registrations.Remove(selectedRegistration);
                    Connection.entities.SaveChanges();
                    LoadData();

                    MessageBox.Show("Регистрация успешно удалена!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении регистрации: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnRefreshRegistrations_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // === Фильтрация ===
        private void cmbCourseFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentUser != null && _currentUser.Role == "Admin" && dgAdminCourses != null)
            {
                var selectedFilter = (cmbCourseFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                var allCourses = Connection.entities.Courses.Include(c => c.Users).ToList();

                if (selectedFilter == "Активные")
                {
                    allCourses = allCourses.Where(c => c.IsActive == true).ToList();
                }
                else if (selectedFilter == "Неактивные")
                {
                    allCourses = allCourses.Where(c => c.IsActive == false).ToList();
                }

                dgAdminCourses.ItemsSource = allCourses;
            }
        }

        // Остальные методы остаются без изменений...
        private void UpdateCoursesCount()
        {
            if (dgCourses == null || txtCoursesCount == null) return;

            var courses = dgCourses.ItemsSource as System.Collections.IList;
            if (courses != null)
            {
                txtCoursesCount.Text = $"Найдено курсов: {courses.Count}";
            }
        }

        private void cmbLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterCourses();
        }

        private void FilterCourses()
        {
            try
            {
                if (Connection.entities == null) return;

                var allCourses = Connection.entities.Courses
                    .Include(c => c.Users)
                    .Where(c => c.IsActive == true)
                    .ToList();

                var selectedLevel = (cmbLevelFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (selectedLevel != "Все уровни")
                {
                    allCourses = allCourses.Where(c => c.Level == selectedLevel).ToList();
                }

                List<int> userRegistrations = new List<int>();
                if (_currentUser != null && _currentUser.Role == "Student")
                {
                    userRegistrations = Connection.entities.Registrations
                        .Where(r => r.StudentId == _currentUser.UserId && r.Status == "Active")
                        .Select(r => r.CourseId)
                        .ToList();
                }

                if (dgCourses != null)
                {
                    dgCourses.ItemsSource = allCourses;
                    UpdateCoursesCount();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации курсов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEnroll_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Ошибка: пользователь не авторизован", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var button = sender as Button;
            var course = button?.DataContext as Courses;

            if (course != null)
            {
                PaymentWindow paymentWindow = new PaymentWindow(course, _currentUser);
                if (paymentWindow.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Вы успешно записаны на курс!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void cmbUserRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentUser != null && _currentUser.Role == "Admin" && dgUsers != null)
            {
                var selectedRole = (cmbUserRoleFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                var users = Connection.entities.Users.ToList();

                if (selectedRole != "Все роли")
                {
                    users = users.Where(u => u.Role == selectedRole).ToList();
                }

                dgUsers.ItemsSource = users;
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?", "Подтверждение",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Закрыть приложение?", "Подтверждение",
                              MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }
    }
}
