using AuroraLauncher;
using Npgsql;
using System;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class RechargeWindow : Window
    {
        private readonly int _userId;

        public RechargeWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private void OnRecharge_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что выбрана система оплаты
            if (!(RbSystem1.IsChecked == true || RbSystem2.IsChecked == true))
            {
                MessageBox.Show("Выберите способ оплаты.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Парсим сумму
            if (!decimal.TryParse(TbAmount.Text.Trim(), out var amount) || amount < 1m)
            {
                MessageBox.Show("Введите корректную сумму от 1 ₽.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = DbCon.GetConnection();
                // Увеличиваем баланс
                var cmd = new NpgsqlCommand(
                    "UPDATE users SET balance = balance + @amt WHERE user_id = @id", conn);
                cmd.Parameters.AddWithValue("amt", amount);
                cmd.Parameters.AddWithValue("id", _userId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении баланса: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Баланс успешно пополнен на {amount:0.00} ₽.",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void OnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
