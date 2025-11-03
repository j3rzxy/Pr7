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
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("🚗 Добро пожаловать в Автосервис 'Гараж Удачи'!\n");

            try
            {
                while (true)
                {
                    ProcessPendingOrders(); // уменьшаем delivery_delay и добавляем на склад
                    var gameState = Core.GetGameState();

                    if (gameState.balance < 0)
                    {
                        Console.WriteLine($"❌ Вы разорились! Игра окончена. Дней проработано: {gameState.day_count}");
                        break;
                    }

                    gameState.day_count++;
                    Core.Save();

                    Console.WriteLine($"\n📆 День {gameState.day_count} | 💰 Баланс: {gameState.balance:C}");
                    ShowInventory();

                    // Генерация поломки
                    var parts = Core.GetParts().Where(p => p.quantity >= 0).ToList();
                    if (!parts.Any()) continue;

                    var random = new Random();
                    var brokenPart = parts[random.Next(parts.Count)];
                    decimal? repairCost = brokenPart.purchase_price + brokenPart.labor_cost;

                    Console.WriteLine($"\n❗ Приехал клиент! Сломалась деталь: '{brokenPart.name}'");
                    Console.WriteLine($"💰 Клиент заплатит: {repairCost:C}");

                    Console.WriteLine("\nВыберите действие:");
                    Console.WriteLine("1 — Починить");
                    Console.WriteLine("2 — Отказать");
                    Console.WriteLine("3 — Закупить запчасти");
                    Console.WriteLine("4 — Выйти");

                    string input = Console.ReadLine();
                    bool success = false;
                    string status = "";

                    switch (input)
                    {
                        case "1":
                            if (Core.GetInventoryQuantity(brokenPart.id) > 0)
                            {
                                // Успешный ремонт
                                Core.UpdateInventory(brokenPart.id, -1);
                                gameState.balance += repairCost;
                                status = "repaired";
                                Console.WriteLine($"✅ Ремонт выполнен! Получено {repairCost:C}.");
                                success = true;
                            }
                            else
                            {
                                // Есть ли вообще хоть одна деталь на складе?
                                var anyPart = Core.GetParts().FirstOrDefault(p => p.quantity > 0);
                                if (anyPart != null)
                                {
                                    // Случайная замена
                                    Core.UpdateInventory(anyPart.id, -1);
                                    Console.WriteLine($"⚠️ Нет '{brokenPart.name}'! Установлена случайная деталь: '{anyPart.name}'");
                                    Console.WriteLine("😡 Клиент недоволен!");
                                    decimal? penalty = repairCost * 2;
                                    gameState.balance -= penalty;
                                    Console.WriteLine($"📉 Штраф: {penalty:C}");
                                    status = "failed";
                                }
                                else
                                {
                                    Console.WriteLine("❌ На складе вообще нет запчастей!");
                                    decimal? penalty = repairCost * 2;
                                    gameState.balance -= penalty;
                                    status = "failed";
                                }
                            }
                            break;

                        case "2":
                            const decimal refusalPenalty = 40m;
                            gameState.balance -= refusalPenalty;
                            Console.WriteLine($"❌ Отказ. Штраф: {refusalPenalty:C}");
                            status = "refused";
                            break;

                        case "3":
                            HandlePurchase();
                            continue;

                        case "4":
                            Console.WriteLine("Спасибо за игру!");
                            return;

                        default:
                            Console.WriteLine("Неверный ввод. Попробуйте снова.");
                            continue;
                    }

                    // Логируем клиента
                    Core.Context.customer_history.Add(new customer_history
                    {
                        day = gameState.day_count,
                        broken_part_id = brokenPart.id,
                        repair_cost = repairCost,
                        status = status
                    });
                    Core.Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Критическая ошибка: {ex.Message}");
                Console.WriteLine("Но игра продолжается...");
                Main(args); // или просто продолжить без перезапуска
            }
        }
        static void HandlePurchase()
        {
            var parts = Core.GetParts();
            Console.WriteLine("\n🛒 Доступные запчасти:");
            for (int i = 0; i < parts.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {parts[i].name} — закупка: {parts[i].purchase_price:C}");
            }

            Console.Write("Выберите номер запчасти: ");
            if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > parts.Count)
            {
                Console.WriteLine("Неверный номер.");
                return;
            }

            var selectedPart = parts[idx - 1];

            Console.Write("Введите количество: ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            {
                Console.WriteLine("Количество должно быть положительным целым числом.");
                return;
            }

            decimal? totalCost = selectedPart.purchase_price * qty;
            var gameState = Core.GetGameState();

            if (gameState.balance < totalCost)
            {
                Console.WriteLine($"❌ Недостаточно средств. Нужно {totalCost:C}, у вас {gameState.balance:C}.");
                return;
            }

            gameState.balance -= totalCost;

            Core.Context.supply_orders.Add(new supply_orders
            {
                part_id = selectedPart.id,
                quantity = qty,
                delivery_delay = 2
            });

            Core.Save();
            Console.WriteLine($"✅ Заказ на {qty} шт. '{selectedPart.name}' оформлен. Поступление через 2 дня.");
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
        static void ProcessPendingOrders()
        {
            var orders = Core.Context.supply_orders.Where(o => o.delivery_delay > 0).ToList();
            foreach (var order in orders)
            {
                order.delivery_delay--;
                if (order.delivery_delay == 0)
                {
                    Core.UpdateInventory(order.part_id, order.quantity);
                    Core.Context.supply_orders.Remove(order);
                }
            }
            Core.Save(); // ← обязательно!
        }
    }
}