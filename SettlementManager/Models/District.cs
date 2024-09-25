namespace SettlementManager.Models;

public class District
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SettlementId { get; set; }
    public List<Resident> Residents { get; set; } = new List<Resident>();
    public string ProductionMode { get; set; }
    public List<string> Output { get; set; } = new List<string>();
    public string Description { get; set; }
    public DistrictGrade Grade { get; set; }
    public string BuffForPlayers { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    
    public List<RequiredResource> RequiredResources { get; set; } = new List<RequiredResource>();

    public List<string>? RequiredTools { get; set; }
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
