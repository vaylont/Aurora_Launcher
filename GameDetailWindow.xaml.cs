using AuroraLauncher;
using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aurora_Launcher
{
    public partial class GameDetailWindow : Window
    {
        private readonly int _userId;
        private readonly int _gameId;
        private decimal _price;

        public GameDetailWindow(int userId, int gameId)
        {
            InitializeComponent();
            _userId = userId;
            _gameId = gameId;
            LoadData();
        }

        private void LoadData()
        {
            using var conn = DbCon.GetConnection();

            // Загрузка информации об игре + разработчик
            var cmd = new NpgsqlCommand(
    "SELECT g.title, g.description, g.cover_url, g.download_url, g.price, g.release_date, d.name, genres.name " +
    "FROM games g " +
    "JOIN developers d ON g.developer_id = d.developer_id " +
    "JOIN genres ON g.genre_id = genres.genre_id " +
    "WHERE g.game_id = @id", conn);



            cmd.Parameters.AddWithValue("id", _gameId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                TitleText.Text = reader.GetString(0);
                DescriptionText.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string? cover = reader.IsDBNull(2) ? null : reader.GetString(2);
                string? downloadUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
                _price = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4);
                DateTime release = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);
                string developerName = reader.IsDBNull(6) ? "Неизвестен" : reader.GetString(6);

                if (!string.IsNullOrEmpty(cover))
                    CoverImage.Source = new BitmapImage(new Uri(cover, UriKind.RelativeOrAbsolute));

                PriceText.Text = $"Цена: {_price:0.00} ₽";
                ReleaseDateText.Text = $"Дата выхода: {release:dd.MM.yyyy}";
                DeveloperText.Text = $"Разработчик: {developerName}";
                GenreText.Text = $"Жанр: {reader.GetString(7)}";

            }

            reader.Close();
            var ownedCmd = new NpgsqlCommand("SELECT COUNT(*) FROM purchases WHERE user_id = @u AND game_id = @g", conn);
            ownedCmd.Parameters.AddWithValue("u", _userId);
            ownedCmd.Parameters.AddWithValue("g", _gameId);
            long owned = (long)ownedCmd.ExecuteScalar();

            if (owned > 0)
            {
                var buyButton = FindName("BuyButton") as Button;
                if (buyButton != null)
                {
                    buyButton.Content = "✔ Приобретено";
                    buyButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff7f"));
                    buyButton.Click -= Buy_Click; // отключаем обработчик
                    buyButton.Cursor = System.Windows.Input.Cursors.Arrow;

                }
            }

            // Баланс
            var balCmd = new NpgsqlCommand("SELECT balance FROM users WHERE user_id = @id", conn);
            balCmd.Parameters.AddWithValue("id", _userId);
            BalanceText.Text = $"Баланс: {balCmd.ExecuteScalar()} ₽";
        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new ShopWindow(_userId).Show();
            Close();
        }

        private void Buy_Click(object sender, RoutedEventArgs e)
        {
            using var conn = DbCon.GetConnection();

            // Проверка, есть ли уже игра
            var check = new NpgsqlCommand("SELECT COUNT(*) FROM purchases WHERE user_id = @u AND game_id = @g", conn);
            check.Parameters.AddWithValue("u", _userId);
            check.Parameters.AddWithValue("g", _gameId);
            long count = (long)check.ExecuteScalar();
            if (count > 0)
            {
                MessageBox.Show("Игра уже куплена!");
                return;
            }

            // Проверка баланса
            var getBal = new NpgsqlCommand("SELECT balance FROM users WHERE user_id = @id", conn);
            getBal.Parameters.AddWithValue("id", _userId);
            decimal balance = (decimal)getBal.ExecuteScalar();

            if (balance < _price)
            {
                MessageBox.Show("Недостаточно средств.");
                return;
            }

            // Списываем средства и сохраняем покупку
            var trans = conn.BeginTransaction();

            var update = new NpgsqlCommand("UPDATE users SET balance = balance - @p WHERE user_id = @id", conn);
            update.Parameters.AddWithValue("p", _price);
            update.Parameters.AddWithValue("id", _userId);
            update.ExecuteNonQuery();

            var insert = new NpgsqlCommand("INSERT INTO purchases (user_id, game_id) VALUES (@u, @g)", conn);
            insert.Parameters.AddWithValue("u", _userId);
            insert.Parameters.AddWithValue("g", _gameId);
            insert.ExecuteNonQuery();

            trans.Commit();

            MessageBox.Show("Покупка успешна!");
            new ShopWindow(_userId).Show();
            Close();
        }
    }
}
