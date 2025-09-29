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

        // Конструктор
        public Entity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
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
        }

        // Абстрактний метод
        public abstract string GetDetails();
    }

    // Клас для персоналу. Наслідується від Person.
    public abstract class Staff : Person
    {
        // Статичне поле
        public static int TotalStaffCount = 0;

        public string Department { get; set; }
        public decimal Salary { get; set; }

        // Конструктор
        public Staff(string firstName, string lastName, string department, decimal salary)
            : base(firstName, lastName)
        {
            Department = department;
            Salary = salary;
            // Використання статичного поля
            TotalStaffCount++;
        }

        // Метод
        public void Promote(decimal raiseAmount)
        {
            Salary += raiseAmount;
        }
    }

    // Клас викладача. Наслідується від Staff.
    public class Professor : Staff
    {
        public string ScientificDegree { get; set; }
        public List<string> CoursesTaught { get; set; } = new List<string>();

        // Конструктор
        public Professor(string firstName, string lastName, string department, decimal salary, string degree)
            : base(firstName, lastName, department, salary)
        {
            ScientificDegree = degree;
        }

        // Перевизначення методу з базового класу Person
        public override string GetDetails()
        {
            return $"Професор: {LastName}, {FirstName}. Кафедра: {Department}. Ступінь: {ScientificDegree}.";
        }
    }

    // Клас студента. Наслідується від Person.
    public class Student : Person
    {
        public int CourseYear { get; set; }
        public string Group { get; set; }
        public double AverageGrade { get; private set; }

        // Делегат
        public delegate void GradeAlertHandler(string studentName, double newGrade);
        // Подія
        public event GradeAlertHandler LowGradeAlert;

        // Конструктор
        public Student(string firstName, string lastName, string group, int courseYear)
            : base(firstName, lastName)
        {
            Group = group;
            CourseYear = courseYear;
        }

        // Метод, який використовує подію
        public void SetNewAverageGrade(double grade)
        {
            AverageGrade = grade;
            // Перевірка умови та виклик події
            if (AverageGrade < 3.5)
            {
                LowGradeAlert?.Invoke(GetDetails(), AverageGrade);
            }
        }

        // Перевизначення методу з базового класу Person
        public override string GetDetails()
        {
            return $"Студент: {LastName}, {FirstName}. Група: {Group}. Курс: {CourseYear}. Середній бал: {AverageGrade:F2}";
        }
    }

    // Клас курсу.
    public class Course
    {
        // Статичний метод
        public static string GetCourseCode(string courseName)
        {
            string acronym = new string(courseName.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(s => s[0]).ToArray()).ToUpper();
            return $"{acronym}-{new Random().Next(100, 999)}";
        }

        public string Name { get; set; }
        public string Code { get; private set; }
        public int Credits { get; set; }
        public Professor Instructor { get; set; }
        public List<Student> EnrolledStudents { get; set; } = new List<Student>();

        // Конструктор
        public Course(string name, int credits)
        {
            Name = name;
            Credits = credits;
            // Використання статичного методу
            Code = GetCourseCode(name);
        }
    }


    // Клас-контейнер для методів розширення має бути статичним
    public static class StudentExtensions
    {
        // Метод розширення. Додає функціонал до класу Student.
        public static string GetStudentInfo(this Student student)
        {
            return $"[EXT INFO] Студент: {student.LastName}, Курс {student.CourseYear}. ID: {student.Id}";
        }
    }

    //Інтерфейс загального CRUD сервісу
    public interface ICrudService<T> where T : Entity
    {
        T Create(T entity);
        T Read(Guid id);
        IEnumerable<T> ReadAll();
        T Update(T entity);
        void Delete(Guid id);

        // Бонусні методи Load та Save для роботи з файлами
        void Save(string filePath);
        void Load(string filePath);
    }

    // Конкретна реалізація CRUD сервісу
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
            {
                throw new KeyNotFoundException($"Неможливо оновити. Об'єкт {typeof(T).Name} з ID {entity.Id} не знайдено.");
            }
            _storage[entity.Id] = entity;
            Console.WriteLine($"[CRUD] Оновлено {typeof(T).Name} з ID: {entity.Id}");
            return entity;
        }

        public void Delete(Guid id)
        {
            if (_storage.Remove(id))
            {
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
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[CRUD] Файл {filePath} не знайдено.");
                return;
            }

            Console.WriteLine($"[CRUD] Завантаження даних {typeof(T).Name} з файлу: {filePath}...");
            string jsonString = File.ReadAllText(filePath);
            var loadedList = JsonSerializer.Deserialize<List<T>>(jsonString);

            _storage.Clear();
            foreach (var entity in loadedList)
            {
                _storage.Add(entity.Id, entity);
            }
            Console.WriteLine($"[CRUD] Успішно завантажено {_storage.Count} об'єктів.");
        }
    }
}