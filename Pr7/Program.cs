using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pr7
{
    internal class Program
    {
        private static readonly Random random = new Random();
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("🚗 Добро пожаловать в Автосервис 'Гараж Удачи'!\n");

            try
            {
                while (true)
                {
                    ProcessPendingOrders();
                    var gameState = Core.GetGameState();

                    if (gameState.balance < 0)
                    {
                        Console.WriteLine($"❌ Вы разорились! Игра окончена. Дней проработано: {gameState.day_count}");
                        break;
                    }

                    gameState.day_count++; // начало нового дня
                    Core.Save();

                    Console.WriteLine($"\n📆 День {gameState.day_count} | 💰 Баланс: {gameState.balance:C}");
                    ShowInventory();

                    var availableParts = Core.GetParts().Where(p => p.quantity > 0).ToList();
                    if (!availableParts.Any())
                    {
                        Console.WriteLine("📦 Склад пуст! Закупите запчасти, чтобы продолжить.");
                        while (!availableParts.Any())
                        {
                            HandlePurchase();
                            availableParts = Core.GetParts().Where(p => p.quantity > 0).ToList();
                            if (!availableParts.Any())
                            {
                                Console.WriteLine("❌ Вы купили, но запчасти ещё не пришли. Ждите...");
                                ProcessPendingOrders(); // обработаем поставки (может, пришла?)
                                                        // Можно показать ожидание, но по логике — клиент приезжает каждый день
                            }
                        }
                    }

                    var random = new Random(); // лучше вынести в поле класса, но для простоты оставим
                    var brokenPart = availableParts[random.Next(availableParts.Count)];
                    decimal? repairCost = brokenPart.purchase_price + brokenPart.labor_cost;

                    Console.WriteLine($"\n❗ Приехал клиент! Сломалась деталь: '{brokenPart.name}'");
                    Console.WriteLine($"💰 Клиент заплатит: {repairCost:C}");

                    string status = "";
                    bool validAction = false;

                    while (!validAction) // ← цикл для повторного ввода в рамках ОДНОГО дня
                    {
                        Console.WriteLine("\nВыберите действие:");
                        Console.WriteLine("1 — Починить");
                        Console.WriteLine("2 — Отказать");
                        Console.WriteLine("3 — Закупить запчасти");
                        Console.WriteLine("4 — Выйти");

                        string input = Console.ReadLine();

                        switch (input)
                        {
                            case "1":
                                // ... логика ремонта ...
                                validAction = true;
                                break;

                            case "2":
                                // ... логика отказа ...
                                validAction = true;
                                break;

                            case "3":
                                HandlePurchase();
                                // Не завершать день! Просто покажем меню снова
                                continue; // ← остаёмся в том же дне, повторяем выбор

                            case "4":
                                Console.WriteLine("Спасибо за игру!");
                                return;

                            default:
                                Console.WriteLine("Неверный ввод. Попробуйте снова.");
                                // validAction остаётся false → цикл повторяется
                                break;
                        }
                    }

                    // Логируем клиента (только после валидного действия 1 или 2)
                    if (status != "") // можно улучшить, но для простоты
                    {
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
            ShowFullPartsCatalog();

            var parts = Core.GetParts();
            Console.Write("\nВведите номер запчасти для закупки: ");
            if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > parts.Count)
            {
                Console.WriteLine("❌ Неверный номер.");
                return;
            }

            var selectedPart = parts[idx - 1];
            Console.Write($"Сколько шт. '{selectedPart.name}' закупить? ");
            if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            {
                Console.WriteLine("❌ Количество должно быть положительным целым числом.");
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
            var inStock = Core.GetParts().Where(p => p.quantity > 0).ToList();
            if (inStock.Any())
            {
                Console.WriteLine("📦 На складе: " + string.Join(", ", inStock.Select(p => $"{p.name} ({p.quantity})")));
            }
            else
            {
                Console.WriteLine("📦 Склад пуст!");
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
        static void ShowFullPartsCatalog()
        {
            Console.WriteLine("\n📋 Каталог всех запчастей:");
            Console.WriteLine("┌──────────────────────┬──────────────┬──────────────┬──────────────┐");
            Console.WriteLine("│ Запчасть             │ Закупка      │ Ремонт       │ На складе    │");
            Console.WriteLine("├──────────────────────┼──────────────┼──────────────┼──────────────┤");

            var allParts = Core.GetParts();
            foreach (var part in allParts)
            {
                decimal? repairPrice = part.purchase_price + part.labor_cost;
                string name = part.name.Length > 20 ? part.name.Substring(0, 20) : part.name.PadRight(20);
                string stock = part.quantity >= 999 ? "∞" : part.quantity.ToString().PadLeft(12);
                Console.WriteLine($"│ {name} │ {part.purchase_price,12:C} │ {repairPrice,12:C} │ {stock} │");
            }
            Console.WriteLine("└──────────────────────┴──────────────┴──────────────┴──────────────┘");
        }
    }
}