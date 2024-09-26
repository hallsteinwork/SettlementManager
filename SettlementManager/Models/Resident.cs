namespace SettlementManager.Models
{
    public class Resident
    {
        public int Id { get; set; }
        
        public int Coins { get; set; } // Star Coins
        
        public int Money { get; set; } // Coins
        public string Name { get; set; }
        public int SettlementId { get; set; } // Связь с поселением
        public string Occupation { get; set; } // Профессия
        public int? DistrictId { get; set; } // ID района (может быть null)
        public bool IsImportant { get; set; }  // Важный поселенец
        public string Description { get; set; } // Описание
        public bool IsAscended { get; set; } = false; // Является ли вознесенным
        public int GradeOfAscending { get; set; } = 0; // Грейд вознесения
        public DateTime DateAdded { get; set; } = DateTime.UtcNow; // Дата добавления

    }
}