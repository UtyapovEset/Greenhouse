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
    /// Логика взаимодействия для ReportsWindow.xaml
    /// </summary>
    public partial class ReportsWindow : Window
    {
        private GreenhouseFacade _facade;
        private string _currentUserRole;

        public ReportsWindow()
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
        }

        public ReportsWindow(string userRole) : this()
        {
            _currentUserRole = userRole;
        }

        private void LoadReports()
        {
            try
            {
                LoadDailyReport();
                LoadYieldForecast();
                LoadCropStatus();
                LoadOverdueTasks();
                LoadTaskCompletion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}");
            }
        }

        private void LoadDailyReport()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var nextWeek = today.AddDays(7);

                try
                {
                    var climateRaw = context.ClimateData
                        .Include(c => c.Greenhouses)
                        .Where(c => c.Timestamp >= today && c.Timestamp < tomorrow)
                        .ToList();

                    var climateData = climateRaw
                        .GroupBy(c => c.GreenhouseId)
                        .Select(g => new
                        {
                            Greenhouse = g.First().Greenhouses?.Name ?? "Не указана",
                            AvgTemp = Math.Round(g.Average(x => x.Temperature), 1),
                            AvgHumidity = Math.Round(g.Average(x => x.Humidity), 1)
                        })
                        .ToList();

                    DailyClimateList.ItemsSource = climateData;

                    var todayTasks = context.WorkTasks
                        .Where(t => t.DueDate >= today && t.DueDate < tomorrow)
                        .ToList();

                    var completedToday = todayTasks.Count(t =>
                        t.Status == "Выполнена");

                    var activeToday = todayTasks.Count(t =>
                        (t.Status == "Запланирована" || t.Status == "В работе") &&
                        t.DueDate >= today);

                    TasksCompletedText.Text = $"{completedToday} выполнено, {activeToday} в работе";

                    var problemZones = context.PlantingZones
                        .Include(z => z.Greenhouses)
                        .Where(z => z.Status == "Проблема" || z.Status == "На обслуживании")
                        .ToList()
                        .Select(z => new
                        {
                            Zone = z.ZoneName ?? "Не указана",
                            Greenhouse = z.Greenhouses?.Name ?? "Не указана",
                            Problem = "Требует внимания"
                        })
                        .ToList();

                    ProblemZonesList.ItemsSource = problemZones;

                    var harvestRaw = context.PlantingZones
                        .Include(z => z.Crops)
                        .Include(z => z.Greenhouses)
                        .Where(z => z.ExpectedHarvestDate >= today &&
                                    z.ExpectedHarvestDate <= nextWeek)
                        .ToList();

                    var upcoming = harvestRaw
                        .Select(z => new
                        {
                            Crop = z.Crops?.Name ?? "Не указана",
                            Zone = z.ZoneName ?? "Не указана",
                            Date = z.ExpectedHarvestDate?.ToString("dd.MM.yyyy") ?? "Не указана",
                            Greenhouse = z.Greenhouses?.Name ?? "Не указана"
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    UpcomingHarvestList.ItemsSource = upcoming;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ежедневного отчета: {ex.Message}");
                }
            }
        }

        private void LoadYieldForecast()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                var today = DateTime.Today;

                try
                {
                    var forecast7Days = GetHarvestForecast(context, today, 7);
                    var forecast30Days = GetHarvestForecast(context, today, 30);
                    var forecast60Days = GetHarvestForecast(context, today, 60);

                    Forecast7DaysText.Text = forecast7Days.Count.ToString();
                    Forecast30DaysText.Text = forecast30Days.Count.ToString();
                    Forecast60DaysText.Text = forecast60Days.Count.ToString();

                    YieldForecastList.ItemsSource = forecast7Days;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки прогноза урожайности: {ex.Message}");
                    Forecast7DaysText.Text = "0";
                    Forecast30DaysText.Text = "0";
                    Forecast60DaysText.Text = "0";
                    YieldForecastList.ItemsSource = new List<dynamic>();
                }
            }
        }

        private List<dynamic> GetHarvestForecast(Greenhouse_AtenaEntities context, DateTime startDate, int days)
        {
            var endDate = startDate.AddDays(days);

            var zones = context.PlantingZones
                .Include(z => z.Crops)
                .Include(z => z.Greenhouses)
                .Where(z => z.ExpectedHarvestDate.HasValue &&
                            z.ExpectedHarvestDate.Value >= startDate &&
                            z.ExpectedHarvestDate.Value <= endDate)
                .ToList();

            var cropGrowthDays = context.Crops.ToDictionary(c => c.Id, c => c.GrowthDays);

            return zones
                .Select(z => new
                {
                    Crop = z.Crops?.Name ?? "Не указана",
                    Greenhouse = z.Greenhouses?.Name ?? "Не указана",
                    HarvestDate = z.ExpectedHarvestDate?.ToString("dd.MM.yyyy") ?? "Не указана",
                    DaysLeft = z.ExpectedHarvestDate.HasValue ? (z.ExpectedHarvestDate.Value - DateTime.Today).Days : 0,
                    ZoneName = z.ZoneName ?? "Не указана"
                })
                .OrderBy(x => x.DaysLeft)
                .ToList<dynamic>();
        }

        private void LoadCropStatus()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                try
                {
                    var crops = context.Crops
                        .Include(c => c.PlantingZones)
                        .Include(c => c.Type_Crops)
                        .Where(c => c.PlantingZones.Any())
                        .ToList();

                    if (!crops.Any())
                    {
                        GoodCropsText.Text = "0";
                        AttentionCropsText.Text = "0";
                        CriticalCropsText.Text = "0";
                        CropStatusList.ItemsSource = new List<dynamic>();
                        return;
                    }

                    var good = crops.Count(c =>
                        !c.PlantingZones.Any(z => z.Status == "Проблема" || z.Status == "На обслуживании"));

                    var attention = crops.Count(c =>
                        c.PlantingZones.Any(z => z.Status == "Проблема" || z.Status == "На обслуживании") &&
                        c.PlantingZones.Count(z => z.Status == "Проблема" || z.Status == "На обслуживании") <=
                        c.PlantingZones.Count * 0.5);

                    var critical = crops.Count(c =>
                        c.PlantingZones.Count(z => z.Status == "Проблема" || z.Status == "На обслуживании") >
                        c.PlantingZones.Count * 0.5);

                    GoodCropsText.Text = good.ToString();
                    AttentionCropsText.Text = attention.ToString();
                    CriticalCropsText.Text = critical.ToString();

                    CropStatusList.ItemsSource = crops
                        .Where(c => c.PlantingZones.Any(z => z.Status == "Проблема" || z.Status == "На обслуживании"))
                        .Select(c => new
                        {
                            Name = c.Name,
                            Sort = c.Type_Crops?.name_sort ?? "Не указан",
                            ProblemZones = c.PlantingZones.Count(z => z.Status == "Проблема" || z.Status == "На обслуживании"),
                            TotalZones = c.PlantingZones.Count
                        })
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки состояния культур: {ex.Message}");
                }
            }
        }

        private void LoadOverdueTasks()
        {
            try
            {
                var overdue = _facade.GetOverdueTasks();

                OverdueTasksCountText.Text = overdue.Count.ToString();
                OverdueTasksList.ItemsSource = overdue.Select(t => new
                {
                    Task = t.Description ?? "Без описания",
                    DueDate = t.DueDate.ToString("dd.MM.yyyy HH:mm"),
                    Status = t.Status ?? "Не указан",
                    AssignedTo = t.AssignedTo ?? "Не назначен",
                    Greenhouse = t.WorkPlans?.Greenhouses?.Name ?? "Общая"
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки просроченных задач: {ex.Message}");
            }
        }

        private void LoadTaskCompletion()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                try
                {
                    var startDate = PeriodStartDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);
                    var endDate = PeriodEndDatePicker.SelectedDate ?? DateTime.Today;

                    var tasks = context.WorkTasks
                        .Include(t => t.WorkPlans)
                        .Include(t => t.WorkPlans.Greenhouses)
                        .Where(t => t.DueDate >= startDate && t.DueDate <= endDate)
                        .ToList();

                    var today = DateTime.Today;

                    var completed = tasks.Count(t =>
                        t.Status == "Выполнена");

                    var active = tasks.Count(t =>
                        (t.Status == "Запланирована" || t.Status == "В работе") &&
                        t.DueDate >= today);

                    var overdue = tasks.Count(t =>
                        (t.Status == "Запланирована" || t.Status == "В работе") &&
                        t.DueDate < today);

                    TotalTasksText.Text = tasks.Count.ToString();
                    CompletedTasksText.Text = completed.ToString();
                    NotCompletedTasksText.Text = (active + overdue).ToString();

                    TaskCompletionList.ItemsSource = tasks
                        .OrderByDescending(t => t.DueDate)
                        .Take(15)
                        .Select(t => new
                        {
                            Date = t.DueDate.ToString("dd.MM.yyyy HH:mm"),
                            Description = t.Description ?? "Без описания",
                            Status = t.Status ?? "Не указан",
                            AssignedTo = t.AssignedTo ?? "Не назначен",
                            Greenhouse = t.WorkPlans?.Greenhouses?.Name ?? "Общая"
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки выполнения задач: {ex.Message}");
                    TotalTasksText.Text = "0";
                    CompletedTasksText.Text = "0";
                    NotCompletedTasksText.Text = "0";
                    TaskCompletionList.ItemsSource = new List<dynamic>();
                }
            }
        }

        private void PeriodStartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodStartDatePicker.SelectedDate.HasValue)
                LoadTaskCompletion();
        }

        private void PeriodEndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodEndDatePicker.SelectedDate.HasValue)
                LoadTaskCompletion();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReports();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentUserRole))
            {
                MessageBox.Show("Ошибка: роль пользователя не определена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!PeriodStartDatePicker.SelectedDate.HasValue)
                PeriodStartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);

            if (!PeriodEndDatePicker.SelectedDate.HasValue)
                PeriodEndDatePicker.SelectedDate = DateTime.Today;

            LoadReports();
        }

        private void UpdateHarvestDatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var cropsWithZones = context.Crops
                        .Include(c => c.PlantingZones)
                        .ToList();

                    foreach (var crop in cropsWithZones)
                    {
                        foreach (var zone in crop.PlantingZones)
                        {
                            zone.ExpectedHarvestDate = DateTime.Today.AddDays(crop.GrowthDays);
                            _facade.UpdatePlantingZone(zone);
                        }
                    }

                    MessageBox.Show("Даты сбора урожая обновлены!");
                    LoadReports();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления дат: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}