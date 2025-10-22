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
        static void HandlePurchase()
        {
            var parts = Core.GetParts();
            Console.WriteLine("\n🛒 Доступные запчасти: ");
            for (int i = 0; i < parts.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {parts[i].name} - закупка: {parts[i].purchase_price}");
            }

            Console.WriteLine("Выберите номер запчасти: ");
            if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > parts.Count)
            {
                Console.WriteLine("Неверный номер.");
                return;
            }

            var selectedPart = parts[idx - 1];

            Console.WriteLine("Введите количество: ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            {
                Console.WriteLine("Количество должно быть положительным числом.");
                return;
            }

            decimal totalCost = selectedPart.purchase_price * qty;
            var gameState = Core.GetGameState();

            if (gameState.balance < totalCost)
            {
                Console.WriteLine($"❌ Недостаточно средств. Нужно {totalCost}, У вас {gameState.balance}.");
                return;
            }

            gameState.balance -= totalCost;

            Core.Context.suplly_orders.Add(new supply_orders
            {
                part_id = selectedPart.id,
                quantity = qty,
                delivery_delay = 2
            });

            Core.Save();
            Console.WriteLine($"Заказ на {qty} шт. '{selectedPart}' оформленю Поступление через 2 дня.");
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