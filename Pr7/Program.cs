using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pr7
{
    internal class Program
    {
        static void Main(string[] args)
        {

        }
        static void ShowInventory()
        {
            Console.Write("📦 Склад: ");
            var parts = Core.GetParts().Where(p => p.quantity > 0).ToList();
            if (!parts.Any())
            {
                Console.WriteLine("пусто!");
            }
            else
            {
                Console.WriteLine(string.Join(", ", parts.Select(p => $"{p.name}: {p.quantity}")));
            }
        }
    }
}