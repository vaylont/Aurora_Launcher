namespace Aurora_Launcher.Models
{
    public class Developer
    {
        public int DeveloperId { get; set; }
        public string Name { get; set; } = "";
    }

    public class GameStat
    {
        public string Title { get; set; } = "";
        public long SalesCount { get; set; }
        public decimal Price { get; set; }
        public decimal Revenue => SalesCount * Price;
    }
}
