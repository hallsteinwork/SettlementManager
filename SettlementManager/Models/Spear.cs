namespace SettlementManager.Models;

public class Spear
{
    public int Id { get; set; }
    public int DistrictId { get; set; } // Связь с районом
    public List<Resident> Residents { get; set; } = new List<Resident>(); // Список поселенцев в копье
    public int LeaderId { get; set; } // ID лидера
    public int Grade { get; set; } // Грейд копья (0-4)
}
