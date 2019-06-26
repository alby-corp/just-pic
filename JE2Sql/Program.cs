namespace JE2Sql
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using static System.Console;

    public static class JE
    {
        public class Response
        {
            public Menu Menu { get; set; }
        }

        public class Id
        {
            [JsonPropertyName("Id")]
            public int Value { get; set; }
        }

        public class Menu
        {
            public IReadOnlyList<Category> Categories { get; set; }

            [JsonPropertyName("accessories")]
            public IReadOnlyList<Accessory> Accessories { get; set; }

            [JsonPropertyName("products")]
            public IReadOnlyList<Product> Products { get; set; }
        }

        public class Category
        {
            public string Name { get; set; }

            public IReadOnlyList<Item> Items { get; set; }
        }

        public class Item
        {
            public IReadOnlyList<Id> Products { get; set; }
        }

        public class Accessory
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal Price { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }

            public string Name { get; set; }

            [JsonPropertyName("Desc")]
            public string Description { get; set; }

            [JsonPropertyName("Syn")]
            public string Variation { get; set; }

            public decimal Price { get; set; }
        }
    }

    public static class Sql
    {
        public class Type
        {
            public Guid Id { get; set; }

            public string Description { get; set; }

            public ISet<int> JEProductIds { get; set; }
        }

        public class Ingredient
        {
            public Guid Id { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        public class FoodIngredient
        {
            public Guid FoodId { get; set; }
            public Guid IngredientId { get; set; }
        }

        public class Food
        {
            public Guid Id { get; set; }
            public Guid TypeId { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }
            public bool Visible { get; set; }
            public IReadOnlyList<string> JEIngredientNames { get; set; }
        }
    }

    class Program
    {
        static string ToSql(string value) => $"'{value.Replace("'", "''")}'";
        static string ToSql(Guid value) => ToSql(value.ToString("D"));

        static string ToSql(decimal value) => value.ToString("##.##", new NumberFormatInfo { NumberDecimalSeparator = "." });

        static string ToSql(bool value) => value ? "true" : "false";

        static async Task<int> Main(string[] args)
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri("http://www.justeat.it")
            };


            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Error.WriteLine("Misssing parameter menuId or restaurant name.");
                return 1;
            }

            var selection = args[0];
            var menuId = int.TryParse(selection, out _) 
                ? selection
                : await GetMenuId(http, selection);

            if (string.IsNullOrEmpty(menuId))
            {
                Error.WriteLine($"Could not find menu id for {selection}.");
                return 2;
            }
            
            var stream = await http.GetStreamAsync($"/menu/getproductsformenu?menuId={menuId}");

            var response = await JsonSerializer.ReadAsync<JE.Response>(stream);

            var menu = response.Menu;

            var types = menu.Categories.Select(category => new Sql.Type
            {
                Id = Guid.NewGuid(),
                Description = category.Name,
                JEProductIds = category.Items.SelectMany(item => item.Products.Select(id => id.Value)).ToHashSet()
            });

            var ingredients = menu.Accessories.Select(accessory => new Sql.Ingredient
            {
                Id = Guid.NewGuid(),
                Name = accessory.Name,
                Price = accessory.Price
            });

            var foods = menu.Products.Select(product => new Sql.Food
            {
                Id = Guid.NewGuid(),
                Name = product.Name,
                Visible = true,
                Price = product.Price,
                TypeId = types.First(type => type.JEProductIds.Contains(product.Id)).Id,
                JEIngredientNames = product.Description.Split(new[] { ",", " e " }, StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim()).ToArray()
            });

            var foodIngredients = foods.SelectMany(food =>
            {
                var matching = food.JEIngredientNames
                    .SelectMany(name => ingredients.Where(ingredient => string.Equals(ingredient.Name, name, StringComparison.InvariantCultureIgnoreCase)).Take(count: 1));

                return matching.Select(ingredient => new Sql.FoodIngredient
                {
                    FoodId = food.Id,
                    IngredientId = ingredient.Id
                });
            });

            WriteLine("// type");

            foreach (var type in types)
            {
                WriteLine($"insert into type (id, description) values ({ToSql(type.Id)}, {type.Description});");
            }

            WriteLine();
            WriteLine("// ingredient");

            foreach (var ingredient in ingredients)
            {
                WriteLine($"insert into ingredient (id, name, price) values ({ToSql(ingredient.Id)}, {ToSql(ingredient.Name)}, {ToSql(ingredient.Price)});");
            }

            WriteLine();
            WriteLine("// food");

            foreach (var food in foods)
            {
                WriteLine($"insert into food (id, name, price, type, visible) values ({ToSql(food.Id)}, {ToSql(food.Name)}, {ToSql(food.Price)}, {ToSql(food.TypeId)}, {ToSql(food.Visible)});");
            }

            WriteLine();
            WriteLine("// food_ingredient");

            foreach (var foodIngredient in foodIngredients)
            {
                WriteLine($"insert into food_ingredient (food, ingredient) values ({foodIngredient.FoodId}, {foodIngredient.IngredientId});");
            }

            return 0;
        }

        static async Task<string> GetMenuId(HttpClient http, string restaurant)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/restaurants-{restaurant}", UriKind.Relative))
            {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" },
                },
            };

            var page = await http.SendAsync(request);
            if (!page.IsSuccessStatusCode)
            {
                return null;
            }

            var regex = new Regex("JustEatData.MenuId = '(.+)';");
            return regex.Match(await page.Content.ReadAsStringAsync()).Groups[1].Value;
        }
    }
}