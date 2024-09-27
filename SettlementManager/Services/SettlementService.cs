using Newtonsoft.Json;
using SettlementManager.Interfaces;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using SettlementManager.Models;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SettlementManager.Services
{
    public class SettlementService : ISettlementService
    {
        // Updated file paths to point to the DataFiles directory
        private readonly string _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DataFiles");
        public readonly string _settlementFilePath = Path.Combine("DataFiles", "settlements.json");
        private readonly string _districtFilePath = Path.Combine("DataFiles", "districts.json");
        private readonly string _residentFilePath = Path.Combine("DataFiles", "residents.json");
        private readonly string _spearFilePath = Path.Combine("DataFiles", "spears.json");

        private readonly ILogger<SettlementService> _logger;
        private ISettlementService _settlementServiceImplementation;

        public SettlementService(ILogger<SettlementService> logger)
        {
            _logger = logger;
        }
        public async Task<List<Settlement>> GetAllSettlements()
        {
            var settlements = await LoadFromFile<List<Settlement>>(_settlementFilePath);
            return settlements ?? new List<Settlement>(); // Return an empty list if null
        }

        
        public async Task<List<District>> GetDistricts()
        {
            var districts = await LoadDistricts(); // Load all districts from the JSON file
            return districts.ToList(); // Filter districts by SettlementId
        }
        
        public async Task AddRequiredResources(District district, List<RequiredResource> requiredResources)
        {
            if (district == null) throw new ArgumentNullException(nameof(district));
            if (requiredResources == null) throw new ArgumentNullException(nameof(requiredResources));

            district.RequiredResources = requiredResources; // Assign list directly
            await SaveDistrictAsync(district);
        }

        public async Task<List<RequiredResource>> GetRequiredResources(District district)
        {
            if (district == null) throw new ArgumentNullException(nameof(district));

            return district.RequiredResources; // Return the list directly
        }

        public async Task<bool> RemoveResourceAsync(int settlementId, ResourceType resourceType, int resourceId, int amount, string name)
        {
            var resources = await LoadResourcesForSettlement(settlementId);

            var resource = resources.FirstOrDefault(r => r.Type == resourceType && r.Id == resourceId);

            if (resource == null)
            {
                _logger.LogWarning($"Resource not found. Type={resourceType}, Id={resourceId}");
                return false; // Ресурс не найден
            }

            if (resource.Amount < amount)
            {
                _logger.LogWarning($"Not enough resource. Requested={amount}, Available={resource.Amount}");
                return false; // Недостаточно ресурсов
            }

            // Уменьшение количества ресурса
            resource.Amount -= amount;

            // Логирование изменения
            _logger.LogInformation($"Reduced resource {resource.Name} (Id={resource.Id}) by {amount}. New amount: {resource.Amount}");

            // Обновление ресурса
            await UpdateResourceAsync(resource);

            return true;
        }



        public async Task SaveDistrictAsync(District district)
        {
            Console.WriteLine($"Saving district: {district.Description}");
            List<District> districts = await GetAllDistrictsAsync();
            districts.Add(district);
            await File.WriteAllTextAsync(_districtFilePath, JsonConvert.SerializeObject(districts, Formatting.Indented));
        }
        
        public async Task<List<District>> GetDistrictsForSetllement(int settlementId)
        {
            // Load from JSON
            var settlements = await GetAllSettlements(); // Your method to load settlements
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
    
            // Ensure districts are populated correctly
            return settlement?.Districts ?? new List<District>();
        }



        public async Task<List<District>> GetAllDistrictsAsync()
        {
            if (!File.Exists(_districtFilePath))
            {
                return new List<District>();
            }

            var jsonData = await File.ReadAllTextAsync(_districtFilePath);
            return JsonConvert.DeserializeObject<List<District>>(jsonData) ?? new List<District>();
        }

        public async Task CreateSettlement(Settlement settlement)
        {
            if (settlement == null)
            {
                throw new ArgumentNullException(nameof(settlement), "Settlement cannot be null");
            }

            // Initialize collections if they are null
            settlement.Districts ??= new List<District>();
            settlement.Residents ??= new List<Resident>();
            settlement.Resources ??= new List<Resource>();

            // Load existing settlements
            var settlements = await GetAllSettlements();

            // Load existing districts to check for the one with ID 0
            var districts = await LoadDistricts();
            var defaultDistrict = districts.FirstOrDefault(d => d.Id == 0); // Find the district with ID 0
            // If the default district exists, add it to the new settlement
            if (defaultDistrict != null)
            {
                settlement.Districts.Add(defaultDistrict);
            }
            // Assign a new ID to the settlement
            settlement.Id = settlements.Count > 0 ? settlements.Max(s => s.Id) + 1 : 1;

            settlements.Add(settlement);
            await SaveToFile(_settlementFilePath, settlements);
        } 
        public async Task AddResourceAsync(int settlementId, Resource resource)
            {
                var settlements = await LoadSettlements();
                var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
                
                if (settlement != null)
                {
                    // Найти максимальный существующий идентификатор
                    int newResourceId = settlement.Resources.Count > 0 
                        ? settlement.Resources.Max(r => r.Id) + 1 
                        : 1; // Если нет ресурсов, начинаем с 1

                    // Обработка стандартных ресурсов
                    if (!resource.IsTool)
                    {
                        var existingResource = settlement.Resources.FirstOrDefault(r => r.Type == resource.Type && r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));

                        if (existingResource != null)
                        {
                            existingResource.Amount += resource.Amount; // Суммируем количество
                            _logger.LogInformation($"Updated existing resource: {existingResource.Name}, New Amount: {existingResource.Amount}");
                        }
                        else
                        {
                            resource.Id = newResourceId; // Присваиваем новый идентификатор
                            settlement.Resources.Add(resource); // Добавляем новый стандартный ресурс
                            _logger.LogInformation($"Added new resource: {resource.Name}, Amount: {resource.Amount}, ID: {resource.Id}");
                        }
                    }
                    else
                    {
                        // Обработка инструментов
                        var existingTool = settlement.Resources.FirstOrDefault(r => r.IsTool && r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));

                        if (existingTool != null)
                        {
                            existingTool.Amount += resource.Amount; // Суммируем количество
                            _logger.LogInformation($"Updated existing tool: {existingTool.Name}, New Amount: {existingTool.Amount}");
                        }
                        else
                        {
                            resource.Id = newResourceId; // Присваиваем новый идентификатор для инструмента
                            settlement.Resources.Add(resource); // Добавляем новый инструмент
                            _logger.LogInformation($"Added new tool: {resource.Name}, Amount: {resource.Amount}, ID: {resource.Id}");
                        }
                    }

                    // Сохранить обновленный список поселений
                    await SaveToFile(_settlementFilePath, settlements);
                }
                else
                {
                    _logger.LogWarning("Settlement not found for ID: {SettlementId}", settlementId);
                }
            }


        public async Task<Resource?> GetResourceByNameAndTypeAsync(int settlementId, string name, ResourceType type)
        {
            // Load resources for the settlement
            var resources = await LoadResourcesForSettlement(settlementId);

            // Find the tool with the specified name and type
            return resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && r.Type == type);
        }
        public async Task UpdateResourceAsync(Resource resource)
        {
            // Загрузить все поселения
            var settlements = await LoadSettlements(); // Метод, который загружает все поселения из файла

            // Найти поселение по заданному SettlementId
            var settlement = settlements.FirstOrDefault(s => s.Id == resource.SettlementId);

            if (settlement != null)
            {
                // Найти ресурс для обновления внутри ресурсов поселения
                var existingResource = settlement.Resources.FirstOrDefault(r => r.Id == resource.Id);

                if (existingResource != null)
                {
                    // Обновить необходимые свойства
                    existingResource.Amount = resource.Amount; // Обновить количество или другие свойства по мере необходимости

                    // Сохранить все поселения обратно в файл
                    await SaveToFile(_settlementFilePath, settlements); // Сохранение всего списка поселений
                }
                else
                {
                    throw new Exception("Resource not found."); // Обработка случая, когда ресурс не найден
                }
            }
            else
            {
                throw new Exception("Settlement not found."); // Обработка случая, когда поселение не найдено
            }
        }

        public async Task SaveSettlementsAsync(List<Settlement> settlements)
        {
            var json = JsonConvert.SerializeObject(settlements, Formatting.Indented);
            var filePath = Path.Combine("DataFiles", "settlements.json"); // Adjust the path as needed

            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
        }

        public async Task<List<Resident>> LoadResidents(int settlementId)
        {
            // Загрузка всех поселений
            var settlements = await LoadSettlements();

            // Находим нужное поселение
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
        
            // Если поселение не найдено, возвращаем пустой список
            if (settlement == null)
            {
                return new List<Resident>();
            }

            // Возвращаем список резидентов из найденного поселения
            return settlement.Residents ?? new List<Resident>();
        }

        public async Task<Resource> GetResourceByTypeAndSettlement(string resourceType, int settlementId)
        {
            var resources = await LoadResourcesForSettlement(settlementId);
            return resources.FirstOrDefault(r => r.Type.ToString() == resourceType);
        }


        public async Task<List<Resource>> LoadResourcesForSettlement(int settlementId)
        {
            // Load all settlements
            var settlements = await LoadSettlements(); // Assuming this method loads all settlements
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);

            // Return the resources for the specified settlement
            return settlement?.Resources ?? new List<Resource>();
        }



        public async Task AddSpear(int districtId)
        {
            var districts = await LoadDistricts();
            var district = districts.FirstOrDefault(d => d.Id == districtId);
            if (district != null)
            {
                var spear = new Spear { DistrictId = districtId };
                var spears = await LoadSpears();
                spears.Add(spear);
                await SaveToFile(_spearFilePath, spears);
            }
        }

        public async Task AddSpearMember(int spearId, Resident member)
        {
            var spears = await LoadSpears();
            var spear = spears.FirstOrDefault(s => s.Id == spearId);
            if (spear != null)
            {
                spear.Residents.Add(member);
                await SaveToFile(_spearFilePath, spears);
            }
        }

        public async Task SetAsSpearLeader(int spearId, int leaderId)
        {
            var spears = await LoadSpears();
            var spear = spears.FirstOrDefault(s => s.Id == spearId);
            if (spear != null && spear.Residents.Any(r => r.Id == leaderId))
            {
                spear.LeaderId = leaderId;
                await SaveToFile(_spearFilePath, spears);
            }
        }
        public async Task AddResident(Resident resident, int settlementId)
        {
            // Убедитесь, что SettlementId установлен правильно
            resident.SettlementId = settlementId;

            var residents = await LoadResidents(settlementId); // Загрузите резидентов из данного поселения

            // Устанавливаем уникальный ID для нового поселенца
            resident.Id = residents.Any() ? residents.Max(r => r.Id) + 1 : 1;

            // Добавляем нового поселенца в список
            residents.Add(resident);

            // Сохраняем поселенцев в файл
            await SaveToFile(_residentFilePath, residents);

            // Теперь обновляем файл поселений
            var settlements = await LoadSettlements(); // Загрузка всех поселений
            var settlement = settlements.FirstOrDefault(s => s.Id == resident.SettlementId); // Находим нужное поселение

            if (settlement != null)
            {
                // Добавляем нового поселенца в список жителей поселения
                settlement.Residents.Add(resident); // Здесь добавляем сам объект Resident

                // Проверяем, установлен ли DistrictId
                if (resident.DistrictId > 0) // Предполагаем, что DistrictId больше 0, если установлен
                {
                    // Находим дистрикт по его ID
                    var district = settlement.Districts.FirstOrDefault(d => d.Id == resident.DistrictId);
                    if (district != null)
                    {
                        // Создаем временный список ID резидентов, если его еще нет
                        if (district.ResidentsIds == null)
                        {
                            district.ResidentsIds = new List<int>(); // Предполагаем, что это поле для хранения ID резидентов
                        }

                        // Добавляем ID резидента в список резидентов дистрикта
                        district.ResidentsIds.Add(resident.Id); // Используем список ID резидентов
                    }
                }

                // Сохраняем обновлённое поселение
                await SaveToFile(_settlementFilePath, settlements);
            }
        }


// Assuming you have a method to get all spears for a settlement
        public async Task<int> CalculateStarlightBloodDaily(int settlementId)
        {
            var settlement = await GetInfoSettlement(settlementId);
            if (settlement == null)
                return 0;

            int totalDrops = 0;

            // Loop through each spear in the settlement
            foreach (var spear in settlement.Spears)
            {
                int baseDrops = GetBaseDrops(spear.Grade);
                int additionalDrops = spear.Residents.Count(r => r.IsAscended) * 5; // +5 drops for each ascended resident

                // Total drops for this spear
                totalDrops += baseDrops + additionalDrops;
            }

            return totalDrops;
        }

        private int GetBaseDrops(int spearGrade)
        {
            // Base drops increase by 1.5 times for each level
            return (int)(4 * Math.Pow(1.5, spearGrade));
        }


        public async Task SetResidentToDistrict(int residentId, int districtId)
        {
            var residents = await LoadResidents();
            var resident = residents.FirstOrDefault(r => r.Id == residentId);
            if (resident != null)
            {
                resident.DistrictId = districtId;
                await SaveToFile(_residentFilePath, residents);
            }
        }

        public async Task CalculateOutput(int districtId)
        {
            // Логика расчета выхлопа
        }

        public async Task CalculateInput(int districtId)
        {
            // Логика расчета входа
        }

        public async Task<decimal> CalculateSaldo(int settlementId)
        {
            var settlements = await LoadSettlements();
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
            return settlement?.Resources.Sum(r => r.Amount) ?? 0;
        }

        public async Task AddDistrict(District district)
        {
            var districts = await LoadDistricts();
            districts.Add(district);
            await SaveToFile(_districtFilePath, districts);
        }

        public async Task<bool> BuildDistricts(int settlementId, District district)
        {
            var settlements = await LoadSettlements();
            var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
            if (settlement != null && settlement.ResourcesAreSufficient(district))
            {
                settlement.Districts.Add(district);
                await SaveToFile(_settlementFilePath, settlements);
                return true;
            }
            return false;
        }

        public async Task<Settlement> GetInfoSettlement(int settlementId)
        {
            var settlements = await LoadSettlements();
            return settlements.FirstOrDefault(s => s.Id == settlementId);
        }

        public async Task<List<Resident>> GetResidents(int districtId)
        {
            var residents = await LoadResidents();
            return residents.Where(r => r.DistrictId == districtId).ToList();
        }
        
public async Task UpdateDailyOutput(int settlementId)
{
    // Load settlement and districts data
    var settlements = await LoadSettlements();
    var settlement = settlements.FirstOrDefault(s => s.Id == settlementId);
    if (settlement == null) return;

    var districts = await LoadDistricts();
    var settlementDistricts = districts.Where(d => d.SettlementId == settlementId).ToList();

    // Initialize a dictionary to accumulate resource balances
    var resourceBalances = new Dictionary<ResourceType, int>();

    foreach (var district in settlementDistricts)
    {
        // Process Daily Output Resources
        if (district.DailyOutput != null)
        {
            foreach (var output in district.DailyOutput)
            {
                // Add to resource balances based on Daily Output
                if (resourceBalances.ContainsKey(output.Key))
                    resourceBalances[output.Key] += output.Value; // Accumulate output
                else
                    resourceBalances[output.Key] = output.Value; // Initialize with current output
            }
        }

        // Process Input Resources
        if (district.InputResources != null)
        {
            foreach (var input in district.InputResources)
            {
                if (resourceBalances.ContainsKey(input.Type))
                    resourceBalances[input.Type] -= input.Quantity; // Deduct input from balances
                else
                    resourceBalances[input.Type] = -input.Quantity; // Initialize with negative input
            }
        }
    }

    // Update the settlement's resources with the calculated balances
    foreach (var resourceBalance in resourceBalances)
    {
        var existingResource = settlement.Resources.FirstOrDefault(r => r.Type == resourceBalance.Key);
        if (existingResource != null)
            existingResource.Amount += resourceBalance.Value; // Update existing resource amount
        else
            settlement.Resources.Add(new Resource { Type = resourceBalance.Key, Amount = resourceBalance.Value }); // Add new resource
    }

    // Save the updated settlement data
    await SaveToFile(_settlementFilePath, settlements);
}


       
        private async Task<T> LoadFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return Activator.CreateInstance<T>(); // Return a new instance of T if file doesn't exist
            }

            var json = await File.ReadAllTextAsync(filePath);
            Console.WriteLine($"Content of {filePath}: {json}"); // Log file content

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"File is empty: {filePath}");
                return Activator.CreateInstance<T>(); // Return a new instance of T if the file is empty
            }

            try
            {
                // You can add options here if needed
                return JsonSerializer.Deserialize<T>(json) ?? Activator.CreateInstance<T>(); // Deserialize JSON
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Deserialization error: {ex.Message} at {filePath}");
                return Activator.CreateInstance<T>(); // Return a new instance if deserialization fails
            }
        }


        private async Task<List<Settlement>> LoadSettlements()
        {
            if (!File.Exists(_settlementFilePath))
            {
                return new List<Settlement>();
            }

            string jsonData = await File.ReadAllTextAsync(_settlementFilePath);
            return JsonConvert.DeserializeObject<List<Settlement>>(jsonData) ?? new List<Settlement>();
        }

        public async Task<List<District>> LoadDistricts()
        {
            if (!File.Exists(_districtFilePath))
            {
                return new List<District>();
            }

            var jsonData = await File.ReadAllTextAsync(_districtFilePath);
            return JsonConvert.DeserializeObject<List<District>>(jsonData) ?? new List<District>();
        }

        
        public async Task<List<Resident>> LoadResidents()
        {
            if (!File.Exists(_residentFilePath))
            {
                return new List<Resident>();
            }

            var jsonData = await File.ReadAllTextAsync(_residentFilePath);
            return JsonConvert.DeserializeObject<List<Resident>>(jsonData) ?? new List<Resident>();
        }

        public async Task<List<Spear>> LoadSpears()
        {
            if (!File.Exists(_spearFilePath))
            {
                return new List<Spear>();
            }

            var jsonData = await File.ReadAllTextAsync(_spearFilePath);
            return JsonConvert.DeserializeObject<List<Spear>>(jsonData) ?? new List<Spear>();
        }

        private async Task SaveToFile<T>(string filePath, T data)
        {
            var jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, jsonData);
        }
    }
}