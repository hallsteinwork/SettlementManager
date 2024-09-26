namespace SettlementManager.Models;

public class District
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SettlementId { get; set; }
    public List<Resident> Residents { get; set; } = new List<Resident>(); 
    public string ProductionMode { get; set; }
    public string Description { get; set; }
    public DistrictGrade Grade { get; set; }
    public string BuffForPlayers { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    
    public List<RequiredResource>? RequiredResources { get; set; } = new List<RequiredResource>();
    public List<string>? RequiredTools { get; set; }
    public List<int>? ResidentsIds { get; set; }
    
    public List<ResourceOutput>? OutputResource { get; set; } // Output resources
    public List<RequiredResource>? InputResources { get; set; } // Resources consumed for operation
    public Dictionary<ResourceType, int> DailyOutput { get; set; } = new Dictionary<ResourceType, int>();
    public IEnumerable<string?> Output { get; set; } = new List<string>();

}

public class ResourceOutput
{
    public ResourceType Type { get; set; }
    public int Quantity { get; set; }
}

public class RequiredResource
{
    public ResourceType Type { get; set; }
    public int Quantity { get; set; }
}


public enum DistrictGrade
{
    Ordinary,
    Special,
    Rare,
    Unique,
    Mystical
}

public static class DistrictExtensions
{
    public static string ToRussian(this DistrictGrade grade)
    {
        return grade switch
        {
            DistrictGrade.Ordinary => "Обычный",
            DistrictGrade.Special => "Специальный",
            DistrictGrade.Rare => "Редкий",
            DistrictGrade.Unique => "Уникальный",
            DistrictGrade.Mystical => "Мистический",
            _ => "Неизвестный"
        };
    }
}