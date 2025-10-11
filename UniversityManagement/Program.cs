using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using University.Common;

public class Program
{
    // Головний асинхронний метод програми
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;


        // Шлях до файлу, де зберігатимуться дані у форматі JSON
        string filePath = "students_async.json";
        var service = new CrudServiceAsync<Student>(filePath);

        int totalToCreate = 100; // Кількість студентів, яких потрібно створити

        // 🔸 Примітиви синхронізації для демонстрації роботи з потоками
        object consoleLock = new object(); // Використовується для блокування доступу до консолі (щоб уникнути "перемішаного" тексту)
        var semaphore = new SemaphoreSlim(5); // Дозволяє максимум 5 одночасних операцій (імітація обмеження доступу до файлу)
        var are = new AutoResetEvent(false); // Сигналізує про певну подію (наприклад, коли створено певну кількість студентів)

        // 🔸 Використовуємо Parallel.For для паралельного створення студентів і додавання їх у сервіс
        Console.WriteLine("Починаємо паралельне створення студентів...");
        var tasks = new List<Task>();

        // Parallel.For створює окремі потоки виконання
        Parallel.For(0, totalToCreate, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
        {
            // Для кожної ітерації створюємо асинхронне завдання
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(); // Очікуємо дозвіл на виконання (максимум 5 одночасно)
                try
                {
                    // Створюємо нового студента з випадковими даними
                    var s = Student.CreateNew();

                    // Генеруємо випадкову оцінку (0–100)
                    s.AverageGrade = Math.Round((new Random(Guid.NewGuid().GetHashCode()).NextDouble() * 100), 2);

                    // Додаємо студента у CRUD-сервіс
                    await service.CreateAsync(s);

                    // Кожні 100 створених студентів виводимо повідомлення і подаємо сигнал AutoResetEvent
                    if (i % 100 == 0)
                    {
                        lock (consoleLock)
                        {
                            Console.WriteLine($"Створено {i} студентів...");
                        }
                        are.Set(); // Подати сигнал, що подія відбулась
                    }
                }
                finally
                {
                    semaphore.Release(); // Звільняємо "місце" у семафорі
                }
            }));
        });

        // Очікуємо завершення всіх асинхронних завдань
        await Task.WhenAll(tasks);

        // Очікуємо сигнал від AutoResetEvent (або таймаут 100 мс)
        are.WaitOne(100);

        // 🔸 Обчислення статистики для полів AverageGrade та CourseYear
        var all = (await service.ReadAllAsync()).ToList();

        var minGrade = all.Min(s => s.AverageGrade);
        var maxGrade = all.Max(s => s.AverageGrade);
        var avgGrade = all.Average(s => s.AverageGrade);

        var minCourse = all.Min(s => s.CourseYear);
        var maxCourse = all.Max(s => s.CourseYear);
        var avgCourse = all.Average(s => s.CourseYear);

        // 🔸 Виведення результатів у консоль (під lock для безпечного доступу)
        lock (consoleLock)
        {
            Console.WriteLine($"Створено студентів: {all.Count}");
            Console.WriteLine($"Середній бал -> Мін: {minGrade:F2}, Макс: {maxGrade:F2}, Середнє: {avgGrade:F2}");
            Console.WriteLine($"Курс -> Мін: {minCourse}, Макс: {maxCourse}, Середнє: {avgCourse:F2}");
        }

        // 🔸 Асинхронно зберігаємо всю колекцію у файл
        await service.SaveAsync();

        Console.WriteLine("Роботу завершено. Дані збережено у файл.");
    }
}
