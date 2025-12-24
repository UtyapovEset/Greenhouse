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
        private GreenhouseFacade _facade;
        private string _currentUserRole;
        private string _currentViewMode = "Задачи на день";

        public Calendar_Window(string userRole)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _currentUserRole = userRole;
            MainCalendar.SelectedDate = DateTime.Today;

            CheckUserPermissions();
            this.Loaded += (s, e) =>
            {
                LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
            };
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
                if (_currentViewMode == "Задачи на день")
                {
                    LoadTasksForDate(selectedDate);
                }
                else if (_currentViewMode == "Планы работ")
                {
                    LoadPlansForDate(selectedDate);
                }
            }
        }

        private void ViewModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModeComboBox.SelectedItem != null)
            {
                _currentViewMode = (ViewModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                switch (_currentViewMode)
                {
                    case "Задачи на день":
                        LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
                        break;
                    case "Все задачи":
                        LoadAllTasks();
                        break;
                    case "Планы работ":
                        LoadAllPlans();
                        break;
                }
            }
        }

        private void LoadTasksForDate(DateTime date)
        {
            if (TasksHeader == null)
                return;

            TasksHeader.Text = (date.Date == DateTime.Today)
                ? "Задачи на сегодня"
                : $"Задачи на {date.ToShortDateString()}";

            try
            {
                var tasks = _facade.GetTasksForDate(date);

                TasksList.ItemsSource = tasks.Select(t => new
                {
                    TaskId = t.Id,
                    Date = t.DueDate.ToShortDateString(),
                    Time = t.DueDate.ToShortTimeString(),
                    Description = t.Description,
                    Status = t.Status,
                    AssignedTo = t.AssignedTo ?? "Не назначен",
                    PlanName = t.WorkPlans != null ? $"План #{t.WorkPlanId}" : "Без плана"
                }).ToList();

                TasksCountText.Text = $"(Всего: {tasks.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
                TasksCountText.Text = "";
            }
        }

        private void LoadAllTasks()
        {
            TasksHeader.Text = "Все задачи";
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var tasks = context.WorkTasks
                        .AsNoTracking()
                        .Include(t => t.WorkPlans)
                        .OrderByDescending(t => t.DueDate)
                        .Take(100)
                        .ToList();

                    TasksList.ItemsSource = tasks.Select(t => new
                    {
                        TaskId = t.Id,
                        Date = t.DueDate.ToShortDateString(),
                        Time = t.DueDate.ToShortTimeString(),
                        Description = t.Description,
                        Status = t.Status,
                        AssignedTo = t.AssignedTo ?? "Не назначен",
                        PlanName = t.WorkPlans != null ? $"План #{t.WorkPlanId}" : "Без плана"
                    }).ToList();

                    TasksCountText.Text = $"(Показано: {tasks.Count}, всего в БД: {context.WorkTasks.Count()})";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки всех задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
                TasksCountText.Text = "";
            }
        }

        private void LoadAllPlans()
        {
            TasksHeader.Text = "Все планы работ";
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var plans = context.WorkPlans
                        .AsNoTracking()
                        .Include(p => p.WorkTasks)
                        .OrderByDescending(p => p.StartDate)
                        .ToList();

                    TasksList.ItemsSource = plans.Select(p => new
                    {
                        PlanId = p.Id,
                        Date = $"{p.StartDate.ToShortDateString()} - {p.EndDate.ToShortDateString()}",
                        Time = "",
                        Description = $"План #{p.Id} для теплицы",
                        Status = p.Status,
                        AssignedTo = $"Задач: {p.WorkTasks.Count}",
                        PlanName = $"План #{p.Id}"
                    }).ToList();

                    TasksCountText.Text = $"(Всего планов: {plans.Count})";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
                TasksCountText.Text = "";
            }
        }

        private void LoadPlansForDate(DateTime date)
        {
            TasksHeader.Text = $"Планы на {date.ToShortDateString()}";
            try
            {
                var plans = _facade.GetPlansForDate(date);

                TasksList.ItemsSource = plans.Select(p => new
                {
                    PlanId = p.Id,
                    Date = $"{p.StartDate.ToShortDateString()} - {p.EndDate.ToShortDateString()}",
                    Time = "",
                    Description = $"План #{p.Id} для теплицы",
                    Status = p.Status,
                    AssignedTo = $"Задач: {p.WorkTasks.Count}",
                    PlanName = $"План #{p.Id}"
                }).ToList();

                TasksCountText.Text = $"(Найдено планов: {plans.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
                TasksCountText.Text = "";
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
                RefreshCurrentView();
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
                RefreshCurrentView();
            }
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TasksList.SelectedItem == null)
            {
                MessageBox.Show("Выберите задачу или план");
                return;
            }

            dynamic item = TasksList.SelectedItem;

            bool isPlan =
                _currentViewMode == "Планы работ" ||
                (_currentViewMode == "Все задачи" && item.TaskId == null);

            if (isPlan)
            {
                if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
                {
                    MessageBox.Show("У вас нет прав для редактирования планов",
                        "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int planId = item.PlanId;
                var editPlanWindow = new EditPlanWindow(planId);

                if (editPlanWindow.ShowDialog() == true)
                    RefreshCurrentView();

                return;
            }

            int taskId = item.TaskId;

            if (_currentUserRole == "Worker")
            {
                var completeTaskWindow = new CompleteTaskWindow(taskId);
                if (completeTaskWindow.ShowDialog() == true)
                    RefreshCurrentView();
            }
            else
            {
                var editTaskWindow = new EditTaskWindow(taskId);
                if (editTaskWindow.ShowDialog() == true)
                    RefreshCurrentView();
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
            {
                MessageBox.Show("У вас нет прав для удаления",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TasksList.SelectedItem == null)
            {
                MessageBox.Show("Выберите задачу или план для удаления");
                return;
            }

            dynamic item = TasksList.SelectedItem;

            bool isPlan =
                _currentViewMode == "Планы работ" ||
                (_currentViewMode == "Все задачи" && item.TaskId == null);

            if (isPlan)
            {
                int planId = item.PlanId;

                var result = MessageBox.Show(
                    "Удалить выбранный план и все его задачи?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                try
                {
                    using (var context = new Greenhouse_AtenaEntities())
                    {
                        var planToDelete = context.WorkPlans.Find(planId);
                        if (planToDelete == null)
                        {
                            MessageBox.Show("План не найден");
                            return;
                        }

                        context.WorkPlans.Remove(planToDelete);
                        context.SaveChanges();

                        RefreshCurrentView();
                        MessageBox.Show("План удалён");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления плана: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return;
            }

            int taskId = item.TaskId;

            var taskResult = MessageBox.Show(
                "Удалить выбранную задачу?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (taskResult != MessageBoxResult.Yes)
                return;

            try
            {
                _facade.DeleteWorkTask(taskId);
                RefreshCurrentView();
                MessageBox.Show("Задача удалена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления задачи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCurrentView()
        {
            switch (_currentViewMode)
            {
                case "Задачи на день":
                    LoadTasksForDate(MainCalendar.SelectedDate ?? DateTime.Today);
                    break;
                case "Все задачи":
                    LoadAllTasks();
                    break;
                case "Планы работ":
                    LoadAllPlans();
                    break;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }

        private void TasksList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TasksList.SelectedItem != null)
            {
                dynamic selectedItem = TasksList.SelectedItem;

                if (selectedItem.PlanId != null && selectedItem.PlanName != null && selectedItem.PlanName.Contains("План"))
                {
                    int planId = selectedItem.PlanId;
                    OpenPlanTasks(planId);
                }
            }
        }

        private void OpenPlanTasks(int planId)
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var planTasksWindow = new Window
                    {
                        Title = $"Задачи плана #{planId}",
                        Width = 800,
                        Height = 500,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this
                    };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var plan = context.WorkPlans.Find(planId);
                    var headerText = new TextBlock
                    {
                        Text = plan != null
                            ? $"Задачи плана #{planId} (Теплица: {context.Greenhouses.Find(plan.GreenhouseId)?.Name}, {plan.StartDate:dd.MM.yyyy} - {plan.EndDate:dd.MM.yyyy})"
                            : $"Задачи плана #{planId}",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Grid.SetRow(headerText, 0);
                    grid.Children.Add(headerText);

                    var tasksListView = new ListView();
                    var gridView = new GridView();
                    gridView.Columns.Add(new GridViewColumn { Header = "Дата", DisplayMemberBinding = new Binding("DueDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = 120 });
                    gridView.Columns.Add(new GridViewColumn { Header = "Описание", DisplayMemberBinding = new Binding("Description"), Width = 300 });
                    gridView.Columns.Add(new GridViewColumn { Header = "Статус", DisplayMemberBinding = new Binding("Status"), Width = 100 });
                    gridView.Columns.Add(new GridViewColumn { Header = "Исполнитель", DisplayMemberBinding = new Binding("AssignedTo"), Width = 120 });
                    tasksListView.View = gridView;
                    Grid.SetRow(tasksListView, 1);
                    grid.Children.Add(tasksListView);

                    var tasks = context.WorkTasks
                        .Where(t => t.WorkPlanId == planId)
                        .OrderBy(t => t.DueDate)
                        .ToList();

                    tasksListView.ItemsSource = tasks;

                    planTasksWindow.Content = grid;
                    planTasksWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия задач плана: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}