using System;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class Purchase
{
    public int Id { get; set; }
    public string PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Discount Discount { get; set; }
}

public class Discount
{
    public int Id { get; set; }
    public decimal DiscountAmount { get; set; }
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; }
}
public class StoreContext : DbContext
{
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server = MARCHENKO\\SQLEXPRESS;Initial Catalog = Store; Trusted_Connection=True;Integrated Security=True;Trust Server Certificate=True;");
    }
}
public class DatabaseInitializer
{
    private const string MasterConnectionString = "Server=MARCHENKO\\SQLEXPRESS;Initial Catalog=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private const string StoreConnectionString = "Server=MARCHENKO\\SQLEXPRESS;Initial Catalog=Store;Trusted_Connection=True;TrustServerCertificate=True;";

    public static void InitializeDatabase()
    {
        using (var connection = new SqlConnection(MasterConnectionString))
        {
            connection.Open();

            string createDatabaseQuery = "IF DB_ID('Store') IS NULL CREATE DATABASE Store;";
            using (var command = new SqlCommand(createDatabaseQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
        using (var connection = new SqlConnection(StoreConnectionString))
        {
            connection.Open();
            string createPurchasesTable = @"
                IF OBJECT_ID('Purchases', 'U') IS NULL
                CREATE TABLE Purchases (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    PurchaseDate NVARCHAR(50) NOT NULL,
                    TotalAmount MONEY NOT NULL
                );";
            string createDiscountsTable = @"
                IF OBJECT_ID('Discounts', 'U') IS NULL
                CREATE TABLE Discounts (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    DiscountAmount MONEY NOT NULL,
                    PurchaseId INT NOT NULL FOREIGN KEY REFERENCES Purchases(Id) ON DELETE CASCADE
                );";
            using (var command = new SqlCommand(createPurchasesTable, connection))
            {
                command.ExecuteNonQuery();
            }
            using (var command = new SqlCommand(createDiscountsTable, connection))
            {
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Таблицы 'Purchases' и 'Discounts' созданы");
        }
    }
    public static void SeedData()
    {
        using (var connection = new SqlConnection(StoreConnectionString))
        {
            connection.Open();
            string insertPurchases = @"
                INSERT INTO Purchases (PurchaseDate, TotalAmount) VALUES
                ('2024-11-28', 100.00),
                ('2024-11-28', 100.00),
                ('2024-11-29', 200.00);";
            using (var command = new SqlCommand(insertPurchases, connection))
            {
                command.ExecuteNonQuery();
            }
            string insertDiscounts = @"
                INSERT INTO Discounts (DiscountAmount, PurchaseId) VALUES
                (10.00, 1),
                (20.00, 2);";
            using (var command = new SqlCommand(insertDiscounts, connection))
            {
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Данные добавлены");
        }
    }
}
class Manager
{
    private const string ConnectionString = "Server = MARCHENKO\\SQLEXPRESS;Initial Catalog = Store; Trusted_Connection=True;Integrated Security=True;Trust Server Certificate=True;";

    public static void AddPurchase(string purchaseDate, decimal totalAmount)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            string query =  "INSERT INTO Purchases (PurchaseDate, TotalAmount) VALUES (@PurchaseDate, @TotalAmount);";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PurchaseDate", purchaseDate);
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);

                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Покупки добавлены");
    }
    public static void GetPurchases()
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            string query = "SELECT Id, PurchaseDate, TotalAmount FROM Purchases;";

            using (var command = new SqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("Список покупок:");
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string date = reader.GetString(1);
                    decimal amount = reader.GetDecimal(2);

                    Console.WriteLine($"ID: {id}, Date: {date}, Amount: {amount}");
                }
            }
        }
    }
    public static void UpdatePurchase(int purchaseId, string newDate, decimal newAmount)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            string query = "UPDATE Purchases SET PurchaseDate = @PurchaseDate, TotalAmount = @TotalAmount WHERE Id = @Id;";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PurchaseDate", newDate);
                command.Parameters.AddWithValue("@TotalAmount", newAmount);
                command.Parameters.AddWithValue("@Id", purchaseId);

                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine($"Покупки с {purchaseId} обновлены.");
    }
    public static void DeletePurchase(int purchaseId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();

            string query = "DELETE FROM Purchases WHERE Id = @Id;";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", purchaseId);

                command.ExecuteNonQuery();
            }
        }
        Console.WriteLine($"Покупка с {purchaseId} удалена");
    }
    public static void AddDiscount(decimal discountAmount, int purchaseId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            connection.ChangeDatabase("Store");

            var query = $"INSERT INTO Discounts (DiscountAmount, PurchaseId) VALUES ({discountAmount}, {purchaseId})";
            connection.Execute(query);
        }
        Console.WriteLine("Скидка добавлена");
    }
    public static void GetDiscounts()
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            connection.ChangeDatabase("Store");

            var discounts = connection.Query<Discount>("SELECT * FROM Discounts");
            foreach (var discount in discounts)
            {
                Console.WriteLine($"ID: {discount.Id}, Amount: {discount.DiscountAmount}, Purchase ID: {discount.PurchaseId}");
            }
        }
    }
    public static void UpdateDiscount(int discountId, decimal newAmount)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            connection.ChangeDatabase("Store");

            var query = $"UPDATE Discounts SET DiscountAmount = {newAmount} WHERE Id = {discountId}";
            connection.Execute(query);
        }
        Console.WriteLine("Скидка обновлена");
    }

    public static void DeleteDiscount(int discountId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            connection.ChangeDatabase("Store");

            var query = $"DELETE FROM Discounts WHERE Id = {discountId}";
            connection.Execute(query);
        }
        Console.WriteLine("Скидка удалена");
    }

    public static void GetPurchasesWithDiscounts()
    {
        using (var context = new StoreContext())
        {
            var purchases = context.Purchases.Include(p => p.Discount).ToList();
            foreach (var purchase in purchases)
            {
                decimal discountAmount;
                if (purchase.Discount != null)
                {
                    discountAmount = purchase.Discount.DiscountAmount;
                }
                else
                {
                    discountAmount = 0;
                }
                var finalAmount = purchase.TotalAmount - discountAmount;
                Console.WriteLine($"ID: {purchase.Id}, Total: {purchase.TotalAmount}, Discount: {discountAmount}, Final: {finalAmount}");
            }
        }
    }
}
class Program
{
    public static void Main()
    {
        using (var DB = new StoreContext())
        {
            bool breakFlag = true;
            while (breakFlag == true)
            {
                Console.WriteLine("Выберите операцию: \n1-Создать бд с таблицами\n2-Заполить бд\n3-Получить покупки\n4-Получить скидки\n5-Работа с покупками\n6-Работа со скидками\n7-Вывод общей табл\n8-Выход");
                int vibor = Convert.ToInt32(Console.ReadLine());
                switch (vibor)
                {
                    case 1:
                        try
                        {
                            DatabaseInitializer.InitializeDatabase();
                            DB.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Такая бд уже есть");
                        }
                        break;
                    case 2:
                        try
                        {
                            DatabaseInitializer.SeedData();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Такие таблицы уже созданы");
                        }
                        break;
                    case 3:
                        Manager.GetPurchases();
                        break;
                    case 4:
                        Manager.GetDiscounts();
                        break;
                    case 5:
                        Console.WriteLine("Выберите действие: \n1-Добавить новую запись\n2-Редактировать запись\n3-Удалить запись");
                        int vibor2 = Convert.ToInt32(Console.ReadLine());
                        switch (vibor2)
                        {
                            case 1:
                                Console.WriteLine("Введите данные(дата покупки, сумма)");
                                string purchaseDate = Console.ReadLine();
                                decimal totalAmount = Convert.ToDecimal(Console.ReadLine());
                                Manager.AddPurchase(purchaseDate, totalAmount);
                                break;
                            case 2:
                                Console.WriteLine("Введите id по которому изменяем,затем данные которые нужно изменить(дата покупки, сумма) ");
                                int purchaseId = Convert.ToInt32(Console.ReadLine());
                                string newData = Console.ReadLine();
                                decimal newAmount = Convert.ToDecimal(Console.ReadLine());
                                Manager.UpdatePurchase(purchaseId, newData, newAmount);
                                break;
                            case 3:
                                Console.WriteLine("Введите id по которому нужно удалить");
                                int purchaseIdDelete = Convert.ToInt32(Console.ReadLine());
                                Manager.DeletePurchase(purchaseIdDelete);
                                break;
                        }
                        break;
                    case 6:
                        Console.WriteLine("Выберите действие: \n1-Добавить новую запись\n2-Редактировать запись\n3-Удалить запись");
                        int vibor3 = Convert.ToInt32(Console.ReadLine());
                        switch (vibor3)
                        {
                            case 1:
                                Console.WriteLine("Введите данные(скидка, purchaseId)");
                                decimal discountAmount = Convert.ToDecimal(Console.ReadLine());
                                int purchaseId = Convert.ToInt32(Console.ReadLine());
                                Manager.AddDiscount(discountAmount, purchaseId);
                                break;
                            case 2:
                                Console.WriteLine("Введите id по которому изменяем,затем данные которые нужно изменить( сумма) ");
                                int discountId = Convert.ToInt32(Console.ReadLine());
                                decimal newAmount = Convert.ToDecimal(Console.ReadLine());
                                Manager.UpdateDiscount(discountId, newAmount);
                                break;
                            case 3:
                                Console.WriteLine("Введите id по которому нужно удалить");
                                int discountIdDelete = Convert.ToInt32(Console.ReadLine());
                                Manager.DeleteDiscount(discountIdDelete);
                                break;
                        }
                        break;
                    case 7:
                        Manager.GetPurchasesWithDiscounts();
                        break;
                    case 8:
                        breakFlag = false;
                        break;
                }
            }
        }
    }
}
//Console.OutputEncoding = Encoding.UTF8
////or
//Console.OutputEncoding = Encoding.GetEncoding(1251);
////or
//Console.OutputEncoding = Encoding.GetEncoding(866);