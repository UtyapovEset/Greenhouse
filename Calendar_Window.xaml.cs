using System;
using System.Collections.Generic;
using System.Data.Entity;
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

namespace Greenhose
{
    /// <summary>
    /// Логика взаимодействия для Calendar_Window.xaml
    /// </summary>
    public partial class Calendar_Window : Window
    {
        private Greenhouse_AtenaEntities context;
        private string _currentUserRole;

        public Calendar_Window(string userRole)
        {
            InitializeComponent();
            context = new Greenhouse_AtenaEntities();
            MainCalendar.SelectedDate = DateTime.Today;
            LoadTasksForDate(DateTime.Today);
            _currentUserRole = userRole;
            CheckUserPermissions();
        }

        private void CheckUserPermissions()
        {
            if (_currentUserRole == "Admin" || _currentUserRole == "Agronomist")
            {
                AddTaskButton.Visibility = Visibility.Visible;
                AddPlanButton.Visibility = Visibility.Visible;
                EditTaskButton.Visibility = Visibility.Visible;
                DeleteTaskButton.Visibility = Visibility.Visible;
            }
            else if (_currentUserRole == "Technologist")
            {
                AddTaskButton.Visibility = Visibility.Visible;
                AddPlanButton.Visibility = Visibility.Collapsed;
                EditTaskButton.Visibility = Visibility.Visible;
                DeleteTaskButton.Visibility = Visibility.Collapsed;
            }
            else if (_currentUserRole == "Worker")
            {
                AddTaskButton.Visibility = Visibility.Collapsed;
                AddPlanButton.Visibility = Visibility.Collapsed;
                EditTaskButton.Visibility = Visibility.Visible;
                DeleteTaskButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainCalendar.SelectedDate.Value;
                LoadTasksForDate(selectedDate);
            }
        }

        private void LoadTasksForDate(DateTime date)
        {
            TasksHeader.Text = (date.Date == DateTime.Today)
                ? "Задачи на сегодня"
                : $"Задачи на {date.ToShortDateString()}";

            try
            {
                DateTime startDate = date.Date;
                DateTime endDate = startDate.AddDays(1);

                var tasks = context.WorkTasks
                    .AsNoTracking()
                    .Where(t => t.DueDate >= startDate && t.DueDate < endDate)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                if (tasks.Any())
                {
                    TasksList.ItemsSource = tasks.Select(t => new
                    {
                        TaskId = t.Id,
                        Time = t.DueDate.ToShortTimeString(),
                        Description = t.Description,
                        Status = t.Status,
                        AssignedTo = t.AssignedTo ?? "Не назначен"
                    }).ToList();
                }
                else
                {
                    TasksList.ItemsSource = new List<string> { $"На {date.ToShortDateString()} задач не найдено" };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных из БД: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для добавления задач", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addTaskWindow = new AddTaskWindow(MainCalendar.SelectedDate ?? DateTime.Today);
            if (addTaskWindow.ShowDialog() == true)
            {
                LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
            }
        }

        private void AddPlanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
            {
                MessageBox.Show("У вас нет прав для добавления планов", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addPlanWindow = new AddPlanWindow();
            if (addPlanWindow.ShowDialog() == true)
            {
                LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
            }
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksList.SelectedItem != null)
            {
                dynamic selectedTask = TasksList.SelectedItem;
                int taskId = selectedTask.TaskId;

                if (_currentUserRole == "Worker")
                {
                    var completeTaskWindow = new CompleteTaskWindow(taskId);
                    if (completeTaskWindow.ShowDialog() == true)
                    {
                        LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
                    }
                }
                else
                {
                    var editTaskWindow = new EditTaskWindow(taskId);
                    if (editTaskWindow.ShowDialog() == true)
                    {
                        LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите задачу");
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
            {
                MessageBox.Show("У вас нет прав для удаления задач", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TasksList.SelectedItem != null)
            {
                dynamic selectedTask = TasksList.SelectedItem;
                int taskId = selectedTask.TaskId;

                var result = MessageBox.Show("Удалить выбранную задачу?", "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var taskToDelete = context.WorkTasks.Find(taskId);
                        if (taskToDelete != null)
                        {
                            context.WorkTasks.Remove(taskToDelete);
                            context.SaveChanges();
                            LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
                            MessageBox.Show("Задача удалена");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите задачу для удаления");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }
    }
}