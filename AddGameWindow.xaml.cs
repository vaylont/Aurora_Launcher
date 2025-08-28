using AuroraLauncher;
using Microsoft.Win32;
using Npgsql;
using System;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class AddGameWindow : Window
    {
        private readonly int _developerId;

        public AddGameWindow(int developerId)
        {
            InitializeComponent();
            _developerId = developerId;
            LoadGenres(); // <== Загрузка жанров
        }

        public class Genre
        {
            public int GenreId { get; set; }
            public string Name { get; set; } = "";
        }

        private void LoadGenres()
        {
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("SELECT genre_id, name FROM genres ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();

            var genres = new System.Collections.Generic.List<Genre>();
            while (reader.Read())
            {
                genres.Add(new Genre
                {
                    GenreId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            GenreComboBox.ItemsSource = genres;
        }

        private void BrowseCover_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg",
                Title = "Выберите обложку игры"
            };

            if (dlg.ShowDialog() == true)
            {
                CoverBox.Text = dlg.FileName;
            }
        }
        private int EnsureDeveloperRecord(int userId)
        {
            using var conn = DbCon.GetConnection();

            // Проверяем — уже есть?
            var check = new NpgsqlCommand("SELECT developer_id FROM developers WHERE name = (SELECT nickname FROM users WHERE user_id = @id)", conn);
            check.Parameters.AddWithValue("id", userId);

            var existing = check.ExecuteScalar();
            if (existing != null)
                return Convert.ToInt32(existing);

            // Получаем никнейм
            var getNick = new NpgsqlCommand("SELECT nickname FROM users WHERE user_id = @id", conn);
            getNick.Parameters.AddWithValue("id", userId);
            var nickname = (string)getNick.ExecuteScalar();

            // Вставляем в developers
            var insert = new NpgsqlCommand("INSERT INTO developers (name) VALUES (@n) RETURNING developer_id", conn);
            insert.Parameters.AddWithValue("n", nickname);
            return (int)insert.ExecuteScalar();
        }


        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            var title = TitleBox.Text.Trim();
            var desc = DescriptionBox.Text.Trim();
            var cover = CoverBox.Text.Trim();
            var url = DownloadUrlBox.Text.Trim();
            var price = PriceBox.Text.Trim();

            if (GenreComboBox.SelectedValue is not int genreId)
            {
                MessageBox.Show("Выберите жанр.");
                return;
            }


            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url) || !decimal.TryParse(price, out var priceVal))
            {
                MessageBox.Show("Заполните все поля корректно.");
                return;
            }

            using var conn = DbCon.GetConnection();
            int devId = EnsureDeveloperRecord(_developerId); // ✅ Получаем developer_id

            var cmd = new NpgsqlCommand(@"
INSERT INTO games (title, description, developer_id, cover_url, release_date, download_url, price, genre_id)
VALUES (@t, @d, @dev, @c, @r, @u, @price, @genre)", conn);


            cmd.Parameters.AddWithValue("genre", genreId);
            cmd.Parameters.AddWithValue("t", title);
            cmd.Parameters.AddWithValue("d", desc);
            cmd.Parameters.AddWithValue("dev", devId); // ✅ теперь корректный внешний ключ
            cmd.Parameters.AddWithValue("c", string.IsNullOrEmpty(cover) ? (object)DBNull.Value : cover);
            cmd.Parameters.AddWithValue("r", DateTime.Now.Date);
            cmd.Parameters.AddWithValue("u", url);
            cmd.Parameters.AddWithValue("price", priceVal);


            cmd.ExecuteNonQuery();

            MessageBox.Show("Игра добавлена!");
            Close();
        }
    }
}
