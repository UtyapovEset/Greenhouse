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

namespace Greenhose
{
    /// <summary>
    /// Логика взаимодействия для EditTaskWindow.xaml
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        private GreenhouseFacade _facade;
        private int _taskId;

        public EditTaskWindow(int taskId)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _taskId = taskId;
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var task = context.WorkTasks.Find(_taskId);
                    if (task != null)
                    {
                        TaskDatePicker.SelectedDate = task.DueDate.Date;
                        TimeTextBox.Text = task.DueDate.ToShortTimeString();
                        DescriptionTextBox.Text = task.Description;
                        AssignedToTextBox.Text = task.AssignedTo;
                        CommentsTextBox.Text = task.Comments;

                        foreach (ComboBoxItem item in StatusComboBox.Items)
                        {
                            if (item.Content.ToString() == task.Status)
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание задачи");
                return;
            }

            if (!TaskDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату");
                return;
            }

            try
            {
                var task = new WorkTasks
                {
                    Id = _taskId,
                    DueDate = TaskDatePicker.SelectedDate.Value.Date + TimeSpan.Parse(TimeTextBox.Text),
                    Description = DescriptionTextBox.Text,
                    AssignedTo = AssignedToTextBox.Text,
                    Comments = CommentsTextBox.Text,
                    Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                _facade.UpdateWorkTask(task);
                MessageBox.Show("Задача обновлена");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}