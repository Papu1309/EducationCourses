using EducationCourses.Connect;
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
using System.Windows.Shapes;

namespace EducationCourses.PagesAndWindows
{
    /// <summary>
    /// Логика взаимодействия для AddEditCourseWindow.xaml
    /// </summary>
    public partial class AddEditCourseWindow : Window
    {
        private Courses _course;
        private bool _isEditMode;

        public AddEditCourseWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            Title = "Добавление курса";
            LoadTeachers();
        }

        public AddEditCourseWindow(Courses course) : this()
        {
            _course = course;
            _isEditMode = true;
            Title = "Редактирование курса";
            LoadCourseData();
        }

        private void LoadTeachers()
        {
            try
            {
                // Создаем список преподавателей с дополнительным элементом "Не назначен"
                var teachersList = new List<Users>();

                // Добавляем элемент "Не назначен"
                teachersList.Add(new Users { UserId = 0, FullName = "Не назначен" });

                // Добавляем реальных преподавателей
                var realTeachers = Connection.entities.Users
                    .Where(u => u.Role == "Teacher")
                    .ToList();

                teachersList.AddRange(realTeachers);

                // Устанавливаем источник данных
                cmbTeacher.ItemsSource = teachersList;
                cmbTeacher.DisplayMemberPath = "FullName";
                cmbTeacher.SelectedValuePath = "UserId";

                // Выбираем "Не назначен" по умолчанию
                cmbTeacher.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки преподавателей: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCourseData()
        {
            if (_course != null)
            {
                txtName.Text = _course.Name;
                txtDescription.Text = _course.Description;
                txtDuration.Text = _course.Duration.ToString();
                txtPrice.Text = _course.Price.ToString("F2");
                txtMaxStudents.Text = _course.MaxStudents.ToString();
                chkIsActive.IsChecked = _course.IsActive;

                // Устанавливаем уровень
                foreach (ComboBoxItem item in cmbLevel.Items)
                {
                    if (item.Content.ToString() == _course.Level)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                // Устанавливаем преподавателя
                if (_course.TeacherId.HasValue)
                {
                    cmbTeacher.SelectedValue = _course.TeacherId.Value;
                }
                else
                {
                    cmbTeacher.SelectedValue = 0; // "Не назначен"
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Введите название курса!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    ShowError("Введите описание курса!");
                    return;
                }

                if (!int.TryParse(txtDuration.Text, out int duration) || duration <= 0)
                {
                    ShowError("Введите корректную длительность курса!");
                    return;
                }

                if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
                {
                    ShowError("Введите корректную цену курса!");
                    return;
                }

                if (!int.TryParse(txtMaxStudents.Text, out int maxStudents) || maxStudents <= 0)
                {
                    ShowError("Введите корректное максимальное количество студентов!");
                    return;
                }

                // Получаем выбранного преподавателя
                int? teacherId = null;
                if (cmbTeacher.SelectedValue != null && cmbTeacher.SelectedValue is int selectedId && selectedId != 0)
                {
                    teacherId = selectedId;
                }

                if (_isEditMode)
                {
                    // Редактирование существующего курса
                    _course.Name = txtName.Text.Trim();
                    _course.Description = txtDescription.Text.Trim();
                    _course.Duration = duration;
                    _course.Price = price;
                    _course.MaxStudents = maxStudents;
                    _course.Level = (cmbLevel.SelectedItem as ComboBoxItem).Content.ToString();
                    _course.IsActive = chkIsActive.IsChecked ?? true;
                    _course.TeacherId = teacherId;
                }
                else
                {
                    // Добавление нового курса
                    var newCourse = new Courses
                    {
                        Name = txtName.Text.Trim(),
                        Description = txtDescription.Text.Trim(),
                        Duration = duration,
                        Price = price,
                        MaxStudents = maxStudents,
                        CurrentStudents = 0,
                        Level = (cmbLevel.SelectedItem as ComboBoxItem).Content.ToString(),
                        IsActive = chkIsActive.IsChecked ?? true,
                        TeacherId = teacherId
                    };

                    Connection.entities.Courses.Add(newCourse);
                }

                Connection.entities.SaveChanges();

                txtMessage.Text = _isEditMode ? "Курс успешно обновлен!" : "Курс успешно добавлен!";
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
