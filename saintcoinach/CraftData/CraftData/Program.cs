using System;
using System.IO;
using System.Linq;
using SaintCoinach;
using SaintCoinach.Xiv;
using SaintCoinach.Ex;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // Specify the path to the game directory
        const string ConfigFilePath = @"config.txt";

        // Check if the config file exists
        if (!File.Exists(ConfigFilePath))
        {
            Console.WriteLine("Error: Config file not found: " + ConfigFilePath);
            return;
        }
        // Read the game directory path from the config file
        string gameDirectory = File.ReadAllText(ConfigFilePath).Trim();

        // Check if the directory exists
        if (!Directory.Exists(gameDirectory))
        {
            Console.WriteLine("Error: The game directory does not exist: " + gameDirectory);
            return;
        }

        // Initialize SaintCoinach
        try
        {
            var realm = new ARealmReversed(gameDirectory, Language.English);

            // Retrieve the Crafting Recipe sheet
            var recipes = realm.GameData.GetSheet<SaintCoinach.Xiv.Recipe>();

            // Create the CSV file path
            var csvFilePath = @"./export/Recipe.csv";

            // Open or create the CSV file
            using (var writer = new StreamWriter(csvFilePath))
            {
                // Write the header row to the CSV file
                writer.WriteLine("#,CraftType,RecipeLevelTable,Name,MaterialQualityFactor,DifficultyFactor,QualityFactor,DurabilityFactor,RequiredControl,RequiredCraftsmanship");

                // Loop through each recipe and write it to the CSV file
                foreach (var recipe in recipes)
                {
                    // Write recipe data to CSV file (you can add more columns if needed)
                    writer.WriteLine($"{recipe.Key},{recipe.CraftType},{recipe.RecipeLevelTable},{recipe},{recipe.MaterialQualityFactor},{recipe.DifficultyFactor},{recipe.QualityFactor},{recipe.DurabilityFactor},{recipe.RequiredControl},{recipe.RequiredCraftsmanship}");
                }
            }

            Console.WriteLine("Recipe data has been successfully written");
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine("Directory not found: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }
}
