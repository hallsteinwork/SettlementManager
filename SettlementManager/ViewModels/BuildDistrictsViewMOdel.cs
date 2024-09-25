using SettlementManager.Models;

namespace SettlementManager.ViewModels;

public class BuildDistrictsViewModel
{
    public int SettlementId { get; set; }
    public List<District> Districts { get; set; }
    public List<Resource> SettlementResources { get; set; }
    
    public Dictionary<string, int> BuiltDistrictsCount { get; set; } 
}
