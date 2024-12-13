﻿using System;
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
            var realmJA = new ARealmReversed(gameDirectory, Language.Japanese);
            var realmDE = new ARealmReversed(gameDirectory, Language.German);
            var realmFR = new ARealmReversed(gameDirectory, Language.French);

            // Retrieve the Crafting Recipe sheet
            var recipes = realm.GameData.GetSheet<Recipe>();
            var recipesJA = realmJA.GameData.GetSheet<Recipe>();
            var recipesDE = realmDE.GameData.GetSheet<Recipe>();
            var recipesFR = realmFR.GameData.GetSheet<Recipe>();
            var recipeLevelTable = realm.GameData.GetSheet<RecipeLevelTable>();

            // Create the CSV file path
            var csvRecipe = @"./export/Recipe.csv";

            // Open or create the CSV file
            using (var writer = new StreamWriter(csvRecipe))
            {
                // Write the header row to the CSV file
                writer.WriteLine(
                    "Key," +
                    "Level," +
                    "CraftType," +
                    "Name," +
                    "NameJA," +
                    "NameDE," +
                    "NameFR," +
                    "ClassJobLevel," +
                    "MaterialQualityFactor," +
                    "DifficultyFactor," +
                    "QualityFactor," +
                    "DurabilityFactor," +
                    "SuggestedCrafsmanship," +
                    "Stars," +
                    "Difficulty," +
                    "Quality," +
                    "Durability," +
                    "ProgressDivider," +
                    "ProgressModifier," +
                    "QualityDivider," +
                    "QualityModifier"
                    );

                // Loop through each recipe and write it to the CSV file
                foreach (var recipe in recipes)
                {
                    // Write recipe data to CSV file, referenced 'Definitions/RecipeLevelTable.json' to get index values
                    writer.WriteLine(
                    $"{recipe.Key}," +
                    $"{recipe.RecipeLevelTable.Key}," +
                    $"{recipe.CraftType}," +
                    $"{recipe.ResultItem.Name}," +
                    $"{recipesJA[recipe.Key].ResultItem.Name}," +
                    $"{recipesDE[recipe.Key].ResultItem.Name}," +
                    $"{recipesFR[recipe.Key].ResultItem.Name}," +
                    $"{recipe.RecipeLevelTable.ClassJobLevel}," +
                    $"{recipe.MaterialQualityFactor}," +
                    $"{recipe.DifficultyFactor}," +
                    $"{recipe.QualityFactor}," +
                    $"{recipe.DurabilityFactor}," +
                    $"{recipe.RecipeLevelTable[2]}," +
                    $"{recipe.RecipeLevelTable.Stars}," +
                    $"{recipe.RecipeLevelTable.Difficulty}," +
                    $"{recipe.RecipeLevelTable.Quality}," +
                    $"{recipe.RecipeLevelTable[9]}," +
                    $"{recipe.RecipeLevelTable[5]}," +
                    $"{recipe.RecipeLevelTable[7]}," +
                    $"{recipe.RecipeLevelTable[6]}," +
                    $"{recipe.RecipeLevelTable[8]}" 
                    );
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
