using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greenhose
{
    public class GreenhouseFacade
    {
        private Greenhouse_AtenaEntities _context;

        public GreenhouseFacade()
        {
            _context = new Greenhouse_AtenaEntities();
        }

        public object AuthenticateUser(string username, string password)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.Username == username && u.Password == password)
                .Select(u => new { u.Username, u.UserRoles.Name })
                .FirstOrDefault();

            return user;
        }

        public List<Greenhouses> GetGreenhouses()
        {
            return _context.Greenhouses.ToList();
        }

        public Greenhouses GetGreenhouseWithDetails(int id)
        {
            return _context.Greenhouses
                .Include(g => g.PlantingZones)
                .Include(g => g.ClimateData)
                .FirstOrDefault(g => g.Id == id);
        }

        public void AddGreenhouse(Greenhouses greenhouse)
        {
            _context.Greenhouses.Add(greenhouse);
            _context.SaveChanges();
        }

        public void UpdateGreenhouse(Greenhouses greenhouse)
        {
            var existing = _context.Greenhouses.Find(greenhouse.Id);
            if (existing != null)
            {
                existing.Name = greenhouse.Name;
                existing.Description = greenhouse.Description;
                _context.SaveChanges();
            }
        }

        public void DeleteGreenhouse(int id)
        {
            var greenhouse = _context.Greenhouses.Find(id);
            if (greenhouse != null)
            {
                _context.Greenhouses.Remove(greenhouse);
                _context.SaveChanges();
            }
        }

        public List<Crops> GetCrops()
        {
            return _context.Crops.Include(c => c.Type_Crops).ToList();
        }

        public Crops GetCropWithDetails(int id)
        {
            return _context.Crops
                .Include(c => c.Type_Crops)
                .Include(c => c.PlantingZones)
                .FirstOrDefault(c => c.Id == id);
        }

        public void AddCrop(Crops crop)
        {
            _context.Crops.Add(crop);
            _context.SaveChanges();
        }

        public void UpdateCrop(Crops crop)
        {
            var existing = _context.Crops.Find(crop.Id);
            if (existing != null)
            {
                existing.Name = crop.Name;
                existing.Sort = crop.Sort;
                existing.GrowthDays = crop.GrowthDays;
                existing.OptimalTemperature = crop.OptimalTemperature;
                existing.OptimalHumidity = crop.OptimalHumidity;
                _context.SaveChanges();
            }
        }

        public void DeleteCrop(int id)
        {
            var crop = _context.Crops.Find(id);
            if (crop != null)
            {
                _context.Crops.Remove(crop);
                _context.SaveChanges();
            }
        }

        public List<WorkTasks> GetTasksForDate(DateTime date)
        {
            DateTime start = date.Date;
            DateTime end = start.AddDays(1);

            return _context.WorkTasks
                .Include(t => t.WorkPlans)
                .Where(t => t.DueDate >= start && t.DueDate < end)
                .OrderBy(t => t.DueDate)
                .ToList();
        }

        public List<WorkPlans> GetPlansForDate(DateTime date)
        {
            return _context.WorkPlans
                .Include(p => p.WorkTasks)
                .Where(p => p.StartDate <= date && p.EndDate >= date)
                .OrderBy(p => p.StartDate)
                .ToList();
        }

        public void AddWorkPlan(WorkPlans plan)
        {
            _context.WorkPlans.Add(plan);
            _context.SaveChanges();
        }

        public void UpdateWorkPlan(WorkPlans plan)
        {
            var existing = _context.WorkPlans.Find(plan.Id);
            if (existing != null)
            {
                existing.GreenhouseId = plan.GreenhouseId;
                existing.StartDate = plan.StartDate;
                existing.EndDate = plan.EndDate;
                existing.Status = plan.Status;
                _context.SaveChanges();
            }
        }

        public void AddWorkTask(WorkTasks task)
        {
            _context.WorkTasks.Add(task);
            _context.SaveChanges();
        }

        public void UpdateWorkTask(WorkTasks task)
        {
            var existing = _context.WorkTasks.Find(task.Id);
            if (existing != null)
            {
                existing.WorkPlanId = task.WorkPlanId;
                existing.PlantingZoneId = task.PlantingZoneId;
                existing.DueDate = task.DueDate;
                existing.Description = task.Description;
                existing.AssignedTo = task.AssignedTo;
                existing.Comments = task.Comments;

                var adapter = new TaskStatusAdapter(existing);

                if (task.Status == "Выполнена")
                    adapter.SetCompleted();
                else if (task.Status == "В работе")
                    adapter.SetInProgress();
                else
                    adapter.SetPlanned();

                _context.SaveChanges();
            }
        }

        public void DeleteWorkTask(int id)
        {
            var task = _context.WorkTasks.Find(id);
            if (task != null)
            {
                _context.WorkTasks.Remove(task);
                _context.SaveChanges();
            }
        }

        public void AddUser(Users user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public List<ClimateData> GetClimateData(int greenhouseId, int count = 10)
        {
            return _context.ClimateData
                .Where(c => c.GreenhouseId == greenhouseId)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .ToList();
        }

        public void AddClimateData(ClimateData data)
        {
            _context.ClimateData.Add(data);
            _context.SaveChanges();
        }

        public List<PlantingZones> GetPlantingZones(int greenhouseId)
        {
            return _context.PlantingZones
                .Where(z => z.GreenhouseId == greenhouseId)
                .Include(z => z.Crops)
                .ToList();
        }

        public void AddPlantingZone(PlantingZones zone)
        {
            _context.PlantingZones.Add(zone);
            _context.SaveChanges();
        }

        public void UpdatePlantingZone(PlantingZones zone)
        {
            var existing = _context.PlantingZones.Find(zone.Id);
            if (existing != null)
            {
                existing.ZoneName = zone.ZoneName;
                existing.GreenhouseId = zone.GreenhouseId;
                existing.CropId = zone.CropId;
                existing.Area = zone.Area;
                existing.Status = zone.Status;
                existing.ExpectedHarvestDate = zone.ExpectedHarvestDate;
                _context.SaveChanges();
            }
        }

        public void DeletePlantingZone(int id)
        {
            var zone = _context.PlantingZones.Find(id);
            if (zone != null)
            {
                _context.PlantingZones.Remove(zone);
                _context.SaveChanges();
            }
        }

        public List<WorkTasks> GetOverdueTasks()
        {
            return _context.WorkTasks
                .Where(t => t.DueDate < DateTime.Now && t.Status != "Выполнена")
                .Include(t => t.WorkPlans)
                .ToList();
        }

        public List<dynamic> GetPlantingZonesWithCrops(int greenhouseId)
        {
            return _context.PlantingZones
                .Where(z => z.GreenhouseId == greenhouseId)
                .Include(z => z.Crops)
                .Select(z => new
                {
                    ZoneName = z.ZoneName,
                    CropName = z.Crops.Name,
                    Status = z.Status,
                    Area = z.Area
                })
                .ToList<dynamic>();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}