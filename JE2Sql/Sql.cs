namespace JE2Sql
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public static class Sql
    {
        static string ToSql(string value)
            => $"'{value.Replace("'", "''")}'";

        static string ToSql(Guid value)
            => ToSql(value.ToString("D"));

        static string ToSql(decimal value)
            => value.ToString("0.00", new NumberFormatInfo { NumberDecimalSeparator = "." });

        static string ToSql(bool value)
            => value ? "true" : "false";
        
        public class Type
        {
            public Guid Id { get; set; }

            public string Description { get; set; }

            public ISet<int> JEProductIds { get; set; }

            public string Render()
                => $@"insert into ""type"" (""id"", ""description"") values ({ToSql(Id)}, {ToSql(Description)});";
        }

        public class Ingredient
        {
            public Guid Id { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }

            public string Render() 
                => $@"insert into ""ingredient"" (""id"", ""name"", ""price"") values ({ToSql(Id)}, {ToSql(Name)}, {ToSql(Price)});";
        }

        public class FoodIngredient
        {
            public Guid FoodId { get; set; }
            public Guid IngredientId { get; set; }

            public string Render() 
                => $@"insert into ""food_ingredient"" (""food"", ""ingredient"") values ({ToSql(FoodId)}, {ToSql(IngredientId)});";
        }

        public class Food
        {
            public Guid Id { get; set; }
            public Guid TypeId { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }
            public bool Visible { get; set; }
            public IReadOnlyList<string> JEIngredientNames { get; set; }

            public string Render() 
                => $@"insert into ""food"" (""id"", ""name"", ""price"", ""type"", ""visible"") values ({ToSql(Id)}, {ToSql(Name)}, {ToSql(Price)}, {ToSql(TypeId)}, {ToSql(Visible)});";
        }
    }
}