using System;
using System.Linq;
// Коментар: Обов'язкове підключення простору імен, де знаходяться всі класи
using University.Common;

public class Program
{
    // Коментар: Метод-обробник події LowGradeAlert
    public static void OnLowGradeAlert(string studentName, double newGrade)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! СПОВІЩЕННЯ !!! Студент {studentName} має низький середній бал: {newGrade:F2}");
        Console.ResetColor();
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("    СИСТЕМА УПРАВЛІННЯ УНІВЕРСИТЕТОМ    ");

        // 1. Ініціалізація та Create (C)
        // Використовуємо клас CrudService<T> з простору імен University.Common
        var studentService = new CrudService<Student>();

        var student1 = new Student("Іван", "Коваленко", "КН-31", 3);
        var student2 = new Student("Олена", "Петрова", "ПМ-22", 2);

        // Підписка на Подію (Event)
        student2.LowGradeAlert += OnLowGradeAlert;

        studentService.Create(student1);
        studentService.Create(student2);

        // 2. Демонстрація Статичних членів та Наслідування
        Console.WriteLine("\n--- 2. Статичні Члени та Наслідування ---");

        // Створення об'єкта Professor для ініціалізації Staff.TotalStaffCount
        var prof = new Professor("Сергій", "Іванов", "Кібербезпеки", 30000m, "К.т.н.");
        // Використання статичного поля з класу Staff
        Console.WriteLine($"Загальна кількість співробітників (Staff.TotalStaffCount): {Staff.TotalStaffCount}");

        // Використання статичного методу з класу Course
        Console.WriteLine($"Код курсу 'Бази даних': {Course.GetCourseCode("Бази даних")}");

        // 3. Read All (R) та Метод Розширення
        Console.WriteLine("\n--- 3. Read All та Метод Розширення ---");
        foreach (var s in studentService.ReadAll())
        {
            // Виклик методу GetDetails() (перевизначення з Person)
            Console.WriteLine($"- Деталі: {s.GetDetails()}");
            // Використання методу розширення
            Console.WriteLine($"- {s.GetStudentInfo()}");
        }

        // 4. Update (U) та Демонстрація Події (Event/Delegate)
        Console.WriteLine("\n--- 4. Update та Подія ---");

        // Оновлення об'єкта
        student1.CourseYear = 4;
        studentService.Update(student1);

        // Виклик методу, що ініціює подію.
        student2.SetNewAverageGrade(3.2); // Спрацює обробник OnLowGradeAlert

        // 5. Демонстрація Бонусних методів (Save/Load)
        Console.WriteLine("\n--- 5. Save/Load (Бонус) ---");
        string filePath = "students_data.json";
        studentService.Save(filePath);

        // Створюємо новий сервіс, щоб довести, що дані завантажуються з файлу
        var loadedStudentService = new CrudService<Student>();
        loadedStudentService.Load(filePath);
        Console.WriteLine($"Перевірка: Студентів після завантаження: {loadedStudentService.ReadAll().Count()}");

        // 6. Delete (D)
        Console.WriteLine("\n--- 6. Delete ---");
        // Беремо ID першого завантаженого студента для видалення
        Guid idToDelete = loadedStudentService.ReadAll().First().Id;
        loadedStudentService.Delete(idToDelete);

        Console.WriteLine($"Студентів після видалення: {loadedStudentService.ReadAll().Count()}");
    }
}