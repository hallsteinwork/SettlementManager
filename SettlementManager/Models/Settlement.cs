using System.Collections;

namespace SettlementManager.Models;

public class Settlement
{
    public int Id { get; set; }
    public string Name { get; set; } // Название поселения
    public string Description { get; set; }

    public int Glory { get; set; } = 0;
    public List<District> Districts { get; set; } = new List<District>(); // Список районов
    public List<Resident> Residents { get; set; } = new List<Resident>(); // Список поселенцев
    public List<Resource> Resources { get; set; } = new List<Resource>();
    public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Дата создания
    public List<Spear> Spears { get; set; } = new List<Spear>();

    public bool ResourcesAreSufficient(District district)
    {

        var availableResources = new Dictionary<ResourceType, int>();
        foreach (var resource in Resources)
        {
            if (availableResources.ContainsKey(resource.Type))
            {
                availableResources[resource.Type] += resource.Amount;
            }
            else
            {
                availableResources[resource.Type] = resource.Amount;
            }
        }

        // // Проверяем, хватает ли ресурсов
        // foreach (var requiredResource in district.RequiredResources)
        // {
        //     if (!availableResources.ContainsKey(requiredResource.Key) || availableResources[requiredResource.Key] < requiredResource.Value)
        //     {
        //         return false; // Не хватает ресурса
        //     }
        // }

        return true; // Все ресурсы достаточны
    }
}
