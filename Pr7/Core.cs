using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pr7
{
    internal class Core
    {
        public static AutoServiceBDEntities Context = new AutoServiceBDEntities();

        public static List<parts> GetParts() => Context.parts.ToList();
        public static List<supply_orders> GetSupplyOrders() => Context.supply_orders.ToList();
        public static game_stats GetGameState() => Context.game_stats.First();
        public static void Save() => Context.SaveChanges();

        public static int GetInventoryQuantity(int? partId)
        {
            var part = Context.parts.FirstOrDefault(p => p.id == partId);
            return part?.quantity ?? 0;
        }

        public static void UpdateInventory(int? partId, int? delta)
        {
            var part = Context.parts.First(p => p.id == partId);
            part.quantity += delta;
            if (part.quantity < 0) part.quantity = 0;
        }
    }
}
