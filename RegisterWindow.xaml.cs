using AuroraLauncher;
using Npgsql;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow() => InitializeComponent();

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginBox.Text.Trim();
            var pass = PasswordBox.Password.Trim();
            var repeat = RepeatPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            if (pass != repeat)
            {
                MessageBox.Show("Пароли не совпадают!");
                return;
            }

            using var conn = DbCon.GetConnection();

            // Проверка на существование логина
            var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login = @login", conn);
            checkCmd.Parameters.AddWithValue("login", login);
            var exists = (long)checkCmd.ExecuteScalar();
            if (exists > 0)
            {
                MessageBox.Show("Логин уже занят.");
                return;
            }

            // Вставка нового пользователя
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO users (login, password_hash, nickname, role_id)
                VALUES (@login, @pass, @nick, 1)", conn);

            insertCmd.Parameters.AddWithValue("login", login);
            insertCmd.Parameters.AddWithValue("pass", pass);
            insertCmd.Parameters.AddWithValue("nick", login);

            insertCmd.ExecuteNonQuery();
            MessageBox.Show("Успешная регистрация!");
            new LoginWindow().Show();
            Close();
        }

        private void OpenLogin_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}
