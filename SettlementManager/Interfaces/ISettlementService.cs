using SettlementManager.Models;

namespace SettlementManager.Interfaces
{
    public interface ISettlementService
    {
        Task AddSpear(int districtId);
        Task AddSpearMember(int spearId, Resident member);
        Task SetAsSpearLeader(int spearId, int leaderId);
        Task AddResident(Resident resident, int settlementId);
        Task SetResidentToDistrict(int residentId, int districtId);
        Task CalculateOutput(int districtId);
        Task CalculateInput(int districtId);
        Task<decimal> CalculateSaldo(int settlementId);
        Task AddDistrict(District district);
        Task<bool> BuildDistricts(int settlementId, District district);
        Task<Settlement> GetInfoSettlement(int settlementId);
        Task<List<Resident>> GetResidents(int districtId);
        
        Task<List<District>> GetDistricts();
        
        Task<List<District>> GetDistrictsForSetllement(int settlementId);
        Task<List<District>> GetAllDistrictsAsync();
        
        Task SaveDistrictAsync(District district);
        Task<List<Settlement>> GetAllSettlements(); // Получение всех поселений
        Task CreateSettlement(Settlement settlement); // Создание нового поселения
        Task AddResourceAsync(int settlementId, Resource resource);
        Task<Resource?> GetResourceByNameAndTypeAsync(int settlementId, string resourceName, ResourceType resourceType);
        Task UpdateResourceAsync(Resource existingResource);
        Task SaveSettlementsAsync(List<Settlement> settlements);
    }
}