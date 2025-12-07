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
    /// Логика взаимодействия для AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        private Greenhouse_AtenaEntities _context;

        public AddUserWindow()
        {
            InitializeComponent();
            _context = new Greenhouse_AtenaEntities();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Введите логин");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль");
                return;
            }

            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО");
                return;
            }

            if (RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль");
                return;
            }

            try
            {
                var existingUser = _context.Users.FirstOrDefault(u => u.Username == UsernameTextBox.Text);
                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует");
                    return;
                }

                string selectedRole = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var role = _context.UserRoles.FirstOrDefault(r => r.Name == selectedRole);

                if (role == null)
                {
                    var availableRoles = string.Join(", ", _context.UserRoles.Select(r => r.Name).ToList());
                    MessageBox.Show($"Ошибка: роль '{selectedRole}' не найдена. Доступные роли: {availableRoles}");
                    return;
                }

                var newUser = new Users
                {
                    Username = UsernameTextBox.Text,
                    Password = PasswordBox.Password,
                    FullName = FullNameTextBox.Text,
                    RoleId = role.Id
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                MessageBox.Show("Пользователь добавлен успешно");
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
    }
}
