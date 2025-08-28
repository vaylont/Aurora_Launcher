namespace Aurora_Launcher.Models
{
    using System.IO;
    using System.Windows.Media;

    public class Game
    {
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
        public decimal Price { get; set; }
        public string? DownloadUrl { get; set; }

        public bool IsInstalled { get; set; }       // установлена ли игра
        public string InstallPath { get; set; } = ""; // полный путь до папки с игрой

        // Вычисляемый текст для кнопки («Установить» или «Запустить»)
        public string ButtonText => IsInstalled ? "Запустить" : "Установить";

        // Вычисляемый цвет для кнопки (синий, если не установлен, и зелёный, если установлен)
        public Brush ButtonColor => IsInstalled
            ? new SolidColorBrush(Color.FromRgb(0x00, 0x80, 0x00))   // тёмно-зелёный
            : new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));  // синий (#007ACC)

        public string PriceText => $"{Price:0.00} ₽";
    }
}
