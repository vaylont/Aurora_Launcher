using AuroraLauncher;
using Microsoft.Win32;
using Npgsql;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class EditProfileWindow : Window
    {
        private readonly int _userId;

        public EditProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadCurrentData();
        }

        private void LoadCurrentData()
        {
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("SELECT nickname, avatar_url FROM users WHERE user_id = @id", conn);
            cmd.Parameters.AddWithValue("id", _userId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                NicknameBox.Text = reader.GetString(0);
                if (!reader.IsDBNull(1))
                    AvatarPathBox.Text = reader.GetString(1);
            }
        }

        private void BrowseAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg",
                Title = "Выберите аватарку"
            };

            if (dlg.ShowDialog() == true)
            {
                AvatarPathBox.Text = dlg.FileName;
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var newNick = NicknameBox.Text.Trim();
            var avatarPath = AvatarPathBox.Text.Trim();

            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand(@"
                UPDATE users
                SET nickname = @nick, avatar_url = @avatar
                WHERE user_id = @id", conn);

            cmd.Parameters.AddWithValue("nick", newNick);
            cmd.Parameters.AddWithValue("avatar", string.IsNullOrWhiteSpace(avatarPath) ? (object)DBNull.Value : avatarPath);
            cmd.Parameters.AddWithValue("id", _userId);

            cmd.ExecuteNonQuery();
            MessageBox.Show("Профиль обновлён!");
            Close();
        }
    }
}
