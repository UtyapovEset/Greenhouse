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
    /// Логика взаимодействия для EditPlanWindow.xaml
    /// </summary>
    public partial class EditPlanWindow : Window
    {
        private GreenhouseFacade _facade;
        private int _planId;

        public EditPlanWindow(int planId)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _planId = planId;
            LoadPlanData();
            LoadGreenhouses();
        }

        private void LoadPlanData()
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var plan = context.WorkPlans.Find(_planId);
                    if (plan != null)
                    {
                        StartDatePicker.SelectedDate = plan.StartDate;
                        EndDatePicker.SelectedDate = plan.EndDate;

                        foreach (ComboBoxItem item in StatusComboBox.Items)
                        {
                            if (item.Content.ToString() == plan.Status)
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
                MessageBox.Show($"Ошибка загрузки данных плана: {ex.Message}");
            }
        }

        private void LoadGreenhouses()
        {
            try
            {
                var greenhouses = _facade.GetGreenhouses();
                GreenhouseComboBox.ItemsSource = greenhouses;
                GreenhouseComboBox.DisplayMemberPath = "Name";
                GreenhouseComboBox.SelectedValuePath = "Id";

                using (var context = new Greenhouse_AtenaEntities())
                {
                    var plan = context.WorkPlans.Find(_planId);
                    if (plan != null)
                    {
                        GreenhouseComboBox.SelectedValue = plan.GreenhouseId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (GreenhouseComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите теплицу");
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите даты начала и окончания");
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания");
                return;
            }

            try
            {
                var plan = new WorkPlans
                {
                    Id = _planId,
                    GreenhouseId = (int)GreenhouseComboBox.SelectedValue,
                    StartDate = StartDatePicker.SelectedDate.Value,
                    EndDate = EndDatePicker.SelectedDate.Value,
                    Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                _facade.UpdateWorkPlan(plan);
                MessageBox.Show("План работ обновлен");
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