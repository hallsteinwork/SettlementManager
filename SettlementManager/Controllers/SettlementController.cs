using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SettlementManager.Interfaces;
using SettlementManager.Models;
using SettlementManager.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using JsonException = System.Text.Json.JsonException;

namespace SettlementManager.Controllers
{
    public class SettlementController : Controller
    {
        private readonly ISettlementService _settlementService;
        private readonly ILogger<SettlementController> _logger;

        public SettlementController(ISettlementService settlementService, ILogger<SettlementController> logger)
        {
            _settlementService = settlementService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var settlements = await _settlementService.GetAllSettlements();
                return View(settlements ?? new List<Settlement>());
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON Error: {jsonEx.Message}");
                return View(new List<Settlement>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return View(new List<Settlement>());
            }
        }

        public async Task<IActionResult> Manage(int settlementId)
        {
            _logger.LogInformation($"Selected Settlement ID from session: {settlementId}");
            HttpContext.Session.SetInt32("SelectedSettlementId", settlementId);

            var settlement = await _settlementService.GetInfoSettlement(settlementId);
            if (settlement == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Управление поселением: " + settlement.Name;
            return View(settlement);
        }
                        
        [HttpPost]
        public async Task<IActionResult> AddResource(Resource resource)
        {
            var settlementIdNullable = HttpContext.Session.GetInt32("SelectedSettlementId");
            _logger.LogInformation($"Selected Settlement ID from session: {settlementIdNullable}");


            // Validate settlementId
            if (settlementIdNullable == null)
            {
                return Json(new { success = false, message = "No selected settlement." });
            }

            int settlementId = settlementIdNullable.Value;
            resource.SettlementId = settlementId;

            _logger.LogInformation($"Settlement ID: {settlementId}");

            var settlement = await _settlementService.GetInfoSettlement(settlementId);
            if (settlement == null)
            {
                return Json(new { success = false, message = "Settlement not found." });
            }

            try
            {
                // Handle standard resources (those that need to be summed)
                if (!resource.IsTool)
                {
                    var existingResource = settlement.Resources.FirstOrDefault(r => r.Type == resource.Type && r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingResource != null)
                    {
                        existingResource.Amount += resource.Amount; // Sum the amount
                        await _settlementService.UpdateResourceAsync(existingResource); // Ensure to have an update method
                        _logger.LogInformation($"Updated existing resource: {existingResource.Name}, New Amount: {existingResource.Amount}");
                    }
                    else
                    {
                        // Add new standard resource
                        await _settlementService.AddResourceAsync(settlementId, resource);
                    }
                }
                else
                {
                    // Handle tools (unique by name, can sum if names are the same)
                    var existingTool = settlement.Resources.FirstOrDefault(r => r.IsTool && r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingTool != null)
                    {
                        existingTool.Amount += resource.Amount; // Sum the amount
                        await _settlementService.UpdateResourceAsync(existingTool); // Ensure to have an update method
                        _logger.LogInformation($"Updated existing tool: {existingTool.Name}, New Amount: {existingTool.Amount}");
                    }
                    else
                    {
                        // Add new tool as it has a unique name
                        await _settlementService.AddResourceAsync(settlementId, resource);
                    }
                }

                // Redirect to the Manage action for the settlement
                return RedirectToAction("Manage", "Settlement", new { settlementId = settlementId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding resource.");
                return Json(new { success = false, message = "An error occurred while adding the resource." });
            }
        }

        // GET: /Settlement/Create
        public IActionResult AddSettlement()
        {
            return View();
        }

        // POST: /Settlement/Create
        [HttpPost]
        public async Task<IActionResult> AddSettlement(Settlement settlement)
        {
            if (ModelState.IsValid)
            {
                var settlements = await _settlementService.GetAllSettlements() ?? new List<Settlement>();
                settlement.Id = settlements.Count > 0 ? settlements.Max(s => s.Id) + 1 : 1;
                await _settlementService.CreateSettlement(settlement);
                return RedirectToAction("Index");
            }
            return View(settlement);
        }

        public async Task<IActionResult> AddResident()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            var settlement = await _settlementService.GetInfoSettlement(settlementId.Value);
            if (settlement == null)
            {
                return NotFound();
            }

            ViewBag.Districts = settlement.Districts; 
            ViewBag.SettlementId = settlementId.Value; 

            return View(new Resident());
        }

        [HttpPost]
        public async Task<IActionResult> AddResident(Resident resident)
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                resident.SettlementId = settlementId.Value;
                await _settlementService.AddResident(resident, settlementId.Value);
                return RedirectToAction("Manage", new { settlementId = resident.SettlementId });
            }
            return View(resident);
        }

        public IActionResult AddDistrict()
        {
            return View(new District());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDistrict(District district, string resourcesList, string toolsList)
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(resourcesList))
                    {
                        district.RequiredResources = JsonConvert.DeserializeObject<List<RequiredResource>>(resourcesList);
                    }

                    if (!string.IsNullOrEmpty(toolsList))
                    {
                        district.RequiredTools = JsonConvert.DeserializeObject<List<string>>(toolsList);
                    }

                    district.SettlementId = settlementId.Value; 
                    await _settlementService.SaveDistrictAsync(district);
                    return RedirectToAction("Manage", new { settlementId = settlementId.Value });
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine("JSON deserialization error: " + jsonEx.Message);
                    ModelState.AddModelError("", "There was an error processing your request. Please check the data and try again.");
                }
            }
            return View(district);
        }

        public async Task<IActionResult> BuildDistricts()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            try
            {
                var districts = await _settlementService.GetDistricts();
                var settlement = await _settlementService.GetInfoSettlement(settlementId.Value);

                if (settlement == null)
                {
                    return NotFound();
                }

// Use the new method to get districts for the specific settlement
                var districtsForSettlement = await _settlementService.GetDistrictsForSetllement(settlementId.Value);

// Count the number of each type of district built in the settlement
                var builtDistrictsCount = districtsForSettlement
                    .GroupBy(d => d.Name)
                    .ToDictionary(g => g.Key, g => g.Count());

                var model = new BuildDistrictsViewModel
                {
                    SettlementId = settlementId.Value,
                    Districts = districts,
                    SettlementResources = settlement.Resources,
                    BuiltDistrictsCount = builtDistrictsCount // Populate this property
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while building districts for settlementId: {SettlementId}", settlementId);
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<IActionResult> BuildDistrict(string description, string districtName)
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                _logger.LogWarning("Selected settlement ID not found in session.");
                return NotFound();
            }
        
            // Load the existing districts from the JSON file
            var districts = await _settlementService.GetDistricts();
            
            // Find the district that matches the name
            var district = districts.FirstOrDefault(d => d.Name.Equals(districtName, StringComparison.OrdinalIgnoreCase));
            if (district == null)
            {
                _logger.LogWarning($"District '{districtName}' not found for settlement ID {settlementId.Value}.");
                return NotFound();
            }
        
            var settlements = await _settlementService.GetAllSettlements();
            var currentSettlement = settlements.FirstOrDefault(s => s.Id == settlementId.Value);
            if (currentSettlement == null)
            {
                _logger.LogWarning($"Current settlement not found for ID {settlementId.Value}.");
                return NotFound();
            }
        
            // Check if resources and tools are available
            bool canBuild = CheckResourcesAndTools(currentSettlement, district.RequiredResources, district.RequiredTools);
            if (!canBuild)
            {
                _logger.LogWarning("Insufficient resources or tools to build the district.");
                return Json(new { success = false, message = "Недостаточно ресурсов или инструментов для строительства района." });
            }
        
            // Deduct the required resources from the settlement
            foreach (var reqResource in district.RequiredResources)
            {
                var resource = currentSettlement.Resources.FirstOrDefault(r => r.Type == reqResource.Type);
                if (resource != null)
                {
                    resource.Amount -= reqResource.Quantity; // Reduce the amount
                    _logger.LogInformation($"Deducted {reqResource.Quantity} of {resource.Type} from settlement ID {settlementId.Value}.");
                }
            }
        
            // Deduct the required tools from the settlement
            foreach (var tool in district.RequiredTools)
            {
                var resource = currentSettlement.Resources.FirstOrDefault(r => r.Name.Equals(tool, StringComparison.OrdinalIgnoreCase) && r.IsTool);
                if (resource != null)
                {
                    resource.Amount -= 1; // Assuming each tool can only be used once per build
                    _logger.LogInformation($"Deducted one of tool '{tool}' from settlement ID {settlementId.Value}.");
                }
            }
        
            // Add the new district to the settlement
            currentSettlement.Districts.Add(district);
            _logger.LogInformation($"Added district '{district.Name}' to settlement ID {settlementId.Value}.");
        
            // Save the updated settlements back to the JSON file
            await _settlementService.SaveSettlementsAsync(settlements);
            _logger.LogInformation($"Saved updated settlements after adding district '{district.Name}'.");
        
            return Json(new { success = true, message = "Район успешно построен." });
        }


        
        private bool CheckResourcesAndTools(Settlement settlement, List<RequiredResource> requiredResources, List<string> requiredTools)
        {
            // Check for required resources
            foreach (var reqResource in requiredResources)
            {
                var availableResource = settlement.Resources.FirstOrDefault(r => r.Type == reqResource.Type);
                if (availableResource == null || availableResource.Amount < reqResource.Quantity) // Use Quantity from RequiredResource
                {
                    return false; // Not enough of this resource
                }
            }

            // Check for required tools
            foreach (var tool in requiredTools)
            {
                var hasTool = settlement.Resources.Any(r => r.Name.Equals(tool, StringComparison.OrdinalIgnoreCase) && r.IsTool);
                if (!hasTool)
                {
                    return false; // Required tool not available
                }
            }

            return true; // All checks passed
        }


        
        private void DeductResources(Settlement settlement, List<Resource> requiredResources)
        {
            foreach (var req in requiredResources)
            {
                var resource = settlement.Resources.FirstOrDefault(r => r.Type == req.Type);
                if (resource != null)
                {
                    resource.Amount -= req.Amount;
                    // Ensure the resource amount does not go below zero
                    if (resource.Amount < 0)
                    {
                        resource.Amount = 0; // Prevent negative values
                    }
                }
            }
        }

        // GET: /Settlement/GetInfoSettlement
        public async Task<IActionResult> GetInfoSettlement()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            var settlement = await _settlementService.GetInfoSettlement(settlementId.Value);
            if (settlement == null)
            {
                return NotFound();
            }
            return View(settlement);
        }

        // GET: /Settlement/GetResidents
        public async Task<IActionResult> GetResidents()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            var residents = await _settlementService.GetResidents(settlementId.Value);
            return View(residents);
        }

        // GET: /Settlement/CalculateSaldo
        public async Task<IActionResult> CalculateSaldo()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            var saldo = await _settlementService.CalculateSaldo(settlementId.Value);
            return View(saldo);
        }

        // POST: /Settlement/AddSpear
        [HttpPost("AddSpear")]
        public async Task<IActionResult> AddSpear()
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            await _settlementService.AddSpear(settlementId.Value);
            return RedirectToAction("Index");
        }

        // POST: /Settlement/AddSpearMember
        [HttpPost("AddSpearMember")]
        public async Task<IActionResult> AddSpearMember(Resident member)
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            await _settlementService.AddSpearMember(settlementId.Value, member);
            return RedirectToAction("Index");
        }

        // POST: /Settlement/SetAsSpearLeader
        [HttpPost("SetAsSpearLeader")]
        public async Task<IActionResult> SetAsSpearLeader(int leaderId)
        {
            var settlementId = HttpContext.Session.GetInt32("SelectedSettlementId");
            if (!settlementId.HasValue)
            {
                return NotFound();
            }

            await _settlementService.SetAsSpearLeader(settlementId.Value, leaderId);
            return RedirectToAction("Index");
        }

        // Дополнительные методы для управления поселением
    }
}
