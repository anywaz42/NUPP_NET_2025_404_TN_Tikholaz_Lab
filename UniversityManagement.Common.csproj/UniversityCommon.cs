using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace University.Common
{

    //Абстрактний базовий клас для всіх сутностей.
    public abstract class Entity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; private set; }

        public Entity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        // Метод
        public void Deactivate()
        {
            IsActive = false;
        }
    }

    // Абстрактний базовий клас для всіх людей. Наслідується від Entity.
    public abstract class Person : Entity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }

        // Конструктор
        public Person(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            DateOfBirth = DateTime.UtcNow.AddYears(-20);
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }

    public class Staff : Person
    {
        public string Position { get; set; }

        public Staff(string firstName, string lastName, string position)
            : base(firstName, lastName)
        {
            Position = position;
        }
    }

    public class Professor : Person
    {
        public string Department { get; set; }
        public double Salary { get; set; }

        public Professor(string firstName, string lastName, string department)
            : base(firstName, lastName)
        {
            Department = department;
            Salary = 0;
        }

        // Статичний фабричний метод для генерації випадкового професора
        public static Professor CreateNew()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            string[] names = new[] { "Ivan", "Petro", "Olena", "Anna", "Mykola", "Sofia" };
            string[] surnames = new[] { "Shevchenko", "Ivanov", "Kovalenko" };
            var prof = new Professor(names[rnd.Next(names.Length)], surnames[rnd.Next(surnames.Length)], "Department")
            {
                Salary = rnd.Next(300, 1500)
            };
            return prof;
        }
    }

    public class Student : Person
    {
        public int CourseYear { get; set; }
        public string Group { get; set; }
        public double AverageGrade { get; set; }

        // Делегат
        public delegate void GradeAlertHandler(string studentName, double newGrade);
        // Подія
        public event GradeAlertHandler? LowGradeAlert;

        // Конструктор
        public Student(string firstName, string lastName, string group, int courseYear)
            : base(firstName, lastName)
        {
            Group = group;
            CourseYear = courseYear;
            AverageGrade = 0;
        }

        public void UpdateGrade(double newGrade)
        {
            AverageGrade = newGrade;
            if (AverageGrade < 50)
            {
                LowGradeAlert?.Invoke($"{FirstName} {LastName}", AverageGrade);
            }
        }

        // Статичний фабричний метод для генерації випадкового студента
        public static Student CreateNew()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            string[] names = new[] { "Ivan", "Petro", "Olena", "Anna", "Mykola", "Sofia", "Dmytro", "Kateryna" };
            string[] surnames = new[] { "Shevchenko", "Ivanov", "Kovalenko", "Tkachenko", "Bondarenko" };
            string group = $"G{rnd.Next(1, 10)}{(char)('A' + rnd.Next(0, 3))}";
            int courseYear = rnd.Next(1, 6);
            var student = new Student(names[rnd.Next(names.Length)], surnames[rnd.Next(surnames.Length)], group, courseYear)
            {
                AverageGrade = Math.Round(rnd.NextDouble() * 100, 2)
            };
            return student;
        }
    }

    public class Course : Entity
    {
        public string Title { get; set; }
        public int Semester { get; set; }
        public int Credits { get; set; }

        public Course(string title, int semester)
        {
            Title = title;
            Semester = semester;
            Credits = 3;
        }

        // Статичний фабричний метод для генерації випадкового курсу
        public static Course CreateNew()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            string[] names = new[] { "Mathematics", "Physics", "Programming", "History", "Philosophy", "Economics" };
            var course = new Course(names[rnd.Next(names.Length)], rnd.Next(1, 6))
            {
                Credits = rnd.Next(1, 7)
            };
            return course;
        }
    }

    // Розширення для студентів
    public static class StudentExtensions
    {
        public static string FullName(this Student s) => $"{s.FirstName} {s.LastName}";
    }

    // Синхронний CRUD сервіс (оригінальний, не змінював)
    public interface ICrudService<T> where T : Entity
    {
        T Create(T entity);
        T Read(Guid id);
        IEnumerable<T> ReadAll();
        T Update(T entity);
        void Delete(Guid id);
        void Save(string filePath);
        void Load(string filePath);
    }

    public class CrudService<T> : ICrudService<T> where T : Entity
    {
        private readonly Dictionary<Guid, T> _storage = new Dictionary<Guid, T>();

        public T Create(T entity)
        {
            _storage.Add(entity.Id, entity);
            Console.WriteLine($"[CRUD] Створено {typeof(T).Name} з ID: {entity.Id}");
            return entity;
        }

        public T Read(Guid id)
        {
            if (_storage.TryGetValue(id, out T entity))
            {
                return entity;
            }
            throw new KeyNotFoundException($"Об'єкт {typeof(T).Name} з ID {id} не знайдено.");
        }

        public IEnumerable<T> ReadAll() => _storage.Values.ToList();

        public T Update(T entity)
        {
            if (!_storage.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Об'єкт {typeof(T).Name} з ID {entity.Id} не знайдено.");

            _storage[entity.Id] = entity;
            return entity;
        }

        public void Delete(Guid id)
        {
            if (_storage.ContainsKey(id))
            {
                _storage.Remove(id);
                Console.WriteLine($"[CRUD] Видалено {typeof(T).Name} з ID: {id}");
                return;
            }
            throw new KeyNotFoundException($"Неможливо видалити. Об'єкт {typeof(T).Name} з ID {id} не знайдено.");
        }

        // метод Save (використовує JSON серіалізацію)
        public void Save(string filePath)
        {
            Console.WriteLine($"[CRUD] Збереження даних {typeof(T).Name} у файл: {filePath}...");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(_storage.Values.ToList(), options);
            File.WriteAllText(filePath, jsonString);
            Console.WriteLine($"[CRUD] Успішно збережено {_storage.Count} об'єктів.");
        }

        // метод Load (використовує JSON десеріалізацію)
        public void Load(string filePath)
        {
            if (!File.Exists(filePath)) return;

            Console.WriteLine($"[CRUD] Завантаження даних {typeof(T).Name} з файлу: {filePath}...");
            string jsonString = File.ReadAllText(filePath);
            var loadedList = JsonSerializer.Deserialize<List<T>>(jsonString);

            _storage.Clear();
            if (loadedList != null)
            {
                foreach (var entity in loadedList)
                {
                    _storage.Add(entity.Id, entity);
                }
            }
            Console.WriteLine($"[CRUD] Успішно завантажено {_storage.Count} об'єктів.");
        }
    }
}
