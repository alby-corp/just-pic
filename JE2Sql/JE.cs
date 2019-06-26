namespace JE2Sql
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public static class JE
    {
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
}