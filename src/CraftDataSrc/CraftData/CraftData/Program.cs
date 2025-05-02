using System.Data.Entity.Core.Metadata.Edm;
using System.Drawing.Printing;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Net;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Xiv;
using SaintCoinach.Xiv.ItemActions;
using SaintCoinach.Xiv.Sheets;

class Utils
{
    public Dictionary<Language, dynamic> GetGameData(string gameDirectory, List<Language> languages)
    {
        Dictionary<Language, dynamic> gameData = new() { };

        for (int i = 0; i < languages.Count; i++)
        {
            Language lang = languages[i];
            try
            {
                ARealmReversed realm = new(gameDirectory, lang);
                IXivSheet<Recipe> recipes = realm.GameData.GetSheet<Recipe>();
                IXivSheet<Item> items = realm.GameData.GetSheet<Item>();
                IXivSheet recipeLevelTable = realm.GameData.GetSheet<RecipeLevelTable>();
                IXivSheet<ItemAction> itemAction = realm.GameData.GetSheet<ItemAction>();
                IXivSheet<ItemFood> itemFood = realm.GameData.GetSheet<ItemFood>();

                Item _ = items[0]; // this calls the item so that it fails if not there and removes the lang
                gameData[lang] = new Dictionary<string, dynamic>();

                gameData[lang]["recipes"] = recipes;
                gameData[lang]["items"] = items;
                gameData[lang]["level_table"] = recipeLevelTable;
                gameData[lang]["item_action"] = itemAction;
                gameData[lang]["item_food"] = itemFood;
            }
            catch (Exception) // temp
            {
                Console.WriteLine("Language not found: " + lang);
                languages.Remove(lang);
                i--;
                continue;
            }
        }
        return gameData;
    }

    public StringBuilder GetBuffsCsv(
        Dictionary<Language, dynamic> gameData,
        List<Language> languages
    )
    {
        StringBuilder csvItem = new();
        csvItem.Append("Key," + "Category,");
        foreach (Language lang in languages)
        {
            switch (lang)
            {
                case Language.English:
                    csvItem.Append("Name,");
                    break;
                case Language.Japanese:
                    csvItem.Append("NameJA,");
                    break;
                case Language.German:
                    csvItem.Append("NameDE,");
                    break;
                case Language.French:
                    csvItem.Append("NameFR,");
                    break;
                case Language.Korean:
                    csvItem.Append("NameKO,");
                    break;
            }
        }
        csvItem.Append(
            "BuffType1,"
                + "PercentageValue1,"
                + "MaxValue1,"
                + "PercentageValueHQ1,"
                + "MaxValueHQ1,"
                + "BuffType2,"
                + "PercentageValue2,"
                + "MaxValue2,"
                + "PercentageValueHQ2,"
                + "MaxValueHQ2,"
                + "BuffType3,"
                + "PercentageValue3,"
                + "MaxValue3,"
                + "PercentageValueHQ3,"
                + "MaxValueHQ3\n"
        );
        foreach (var item in gameData[languages[0]]["items"])
        {
            if (
                item.ItemSearchCategory.ToString() == "Medicine"
                || item.ItemSearchCategory.ToString() == "Meals"
            )
            {
                if (int.TryParse(item.ItemAction["Data[1]"].ToString(), out int j))
                {
                    IXivSheet<ItemFood> itemFood = gameData[languages[0]]["item_food"];
                    if (j <= itemFood.Keys.Count() - 1)
                    {
                        ItemFood buff = itemFood[j];
                        string? buffType = buff[1].ToString();
                        string[] acceptedBuffs = { "Control", "Craftsmanship", "CP" };
                        int i = buff.SourceRow.ColumnValues().Count();
                        if (acceptedBuffs.Contains(buffType) && item.Name.ToString() != "Potion")
                        {
                            int itemKey = item.Key;
                            var values = new StringBuilder();
                            for (int k = 1; k != i; k++) // first value is exp bonus
                            {
                                if (buff[k].ToString() == "")
                                {
                                    values.Append("Nothing");
                                }
                                if (buff[k].ToString() != "True" && buff[k].ToString() != "False")
                                {
                                    if (k == i - 1) // last value doesn't get a comma
                                    {
                                        values.Append($"{buff[k].ToString()}");
                                    }
                                    else
                                    {
                                        values.Append($"{buff[k].ToString()},");
                                    }
                                }
                            }

                            csvItem.Append($"\n{itemKey}," + $"{item.ItemSearchCategory},");
                            foreach (Language lang in languages)
                            {
                                try
                                {
                                    csvItem.Append($"{gameData[lang]["items"][itemKey].Name},");
                                }
                                catch (FileNotFoundException)
                                {
                                    continue;
                                }
                            }
                            csvItem.Append(values.ToString() + "\n");
                        }
                    }
                }
            }
        }
        return csvItem;
    }

    public StringBuilder GetRecipesCsv(
        Dictionary<Language, dynamic> gameData,
        List<Language> languages
    )
    {
        StringBuilder csv = new();
        csv.Append("Key," + "Level," + "CraftType,");
        foreach (Language lang in languages)
        {
            switch (lang)
            {
                case Language.English:
                    csv.Append("Name,");
                    break;
                case Language.Japanese:
                    csv.Append("NameJA,");
                    break;
                case Language.German:
                    csv.Append("NameDE,");
                    break;
                case Language.French:
                    csv.Append("NameFR,");
                    break;
                case Language.Korean:
                    csv.Append("NameKO,");
                    break;
            }
        }
        csv.Append(
            "ClassJobLevel,"
                + "MaterialQualityFactor,"
                + "DifficultyFactor,"
                + "QualityFactor,"
                + "DurabilityFactor,"
                + "SuggestedCraftsmanship,"
                + "Stars,"
                + "Difficulty,"
                + "Quality,"
                + "Durability,"
                + "ProgressDivider,"
                + "ProgressModifier,"
                + "QualityDivider,"
                + "QualityModifier\n"
        );
        List<string> crafters = new();
        foreach (Recipe recipe in gameData[languages[0]]["recipes"])
        {
            if (!crafters.Contains(recipe.CraftType.ToString()))
            {
                crafters.Add(recipe.CraftType.ToString());
            }
            csv.Append(
                $"\n{recipe.Key}," + $"{recipe.RecipeLevelTable.Key}," + $"{recipe.CraftType},"
            );

            foreach (Language lang in languages)
            {
                if (lang == Language.Korean)
                {
                    dynamic keyKO = recipe[3];
                    csv.Append($"{gameData[Language.Korean]["items"][keyKO]},");
                }
                else
                {
                    int key = recipe.ResultItem.Key;
                    csv.Append($"{gameData[lang]["items"][key]},");
                }
            }

            csv.Append(
                $"{recipe.RecipeLevelTable.ClassJobLevel},"
                    + $"{recipe.MaterialQualityFactor},"
                    + $"{recipe.DifficultyFactor},"
                    + $"{recipe.QualityFactor},"
                    + $"{recipe.DurabilityFactor},"
                    + $"{recipe.RecipeLevelTable[2]},"
                    + $"{recipe.RecipeLevelTable.Stars},"
                    + $"{recipe.RecipeLevelTable.Difficulty},"
                    + $"{recipe.RecipeLevelTable.Quality},"
                    + $"{recipe.RecipeLevelTable[9]},"
                    + $"{recipe.RecipeLevelTable[5]},"
                    + $"{recipe.RecipeLevelTable[7]},"
                    + $"{recipe.RecipeLevelTable[6]},"
                    + $"{recipe.RecipeLevelTable[8]}\n"
            );
        }
        foreach (string crafter in crafters)
        {
            System.Console.WriteLine(crafter);
        }
        return csv;
    }

    public void WriteCsvMemory(
        StringBuilder csvRecipe,
        StringBuilder csvItem,
        int sharedMemorySize,
        string sharedMemoryName
    )
    {
        byte[] bytesRecipe = Encoding.UTF8.GetBytes(csvRecipe.ToString());
        byte[] bytesItem = Encoding.UTF8.GetBytes(csvItem.ToString());
        const int sharedMemoryOffest = 8;
        sharedMemorySize += bytesRecipe.Length + bytesItem.Length + sharedMemoryOffest;

        using MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(
            sharedMemoryName,
            sharedMemorySize
        );
        using MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
        accessor.Write(0, bytesRecipe.Length);
        accessor.Write(4, bytesItem.Length);
        accessor.WriteArray(sharedMemoryOffest, bytesRecipe, 0, bytesRecipe.Length);
        accessor.WriteArray(bytesRecipe.Length, bytesItem, 0, bytesItem.Length);
        Console.WriteLine("terminate");
        Console.ReadLine();
    }

    public Config? GetConfig()
    {
        try
        {
            string filePath = "config.json";
            string jsonString = File.ReadAllText(filePath);

            Config? config = JsonSerializer.Deserialize<Config>(jsonString);
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading config file: " + ex.Message);
            return null;
        }
    }
}

class Config
{
    public required string GameDirectory { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        string logFilePath = "log.txt";
        File.WriteAllText(logFilePath, string.Empty);
        using (TextWriter writer = File.AppendText(logFilePath))
        {
            Console.OutputEncoding = Encoding.UTF8;
            Utils utils = new();
            const string ConfigFilePath = @"config.json";
            Config? config = utils.GetConfig();

            const string sharedMemoryName = "Global\\CraftData";
            int sharedMemorySize = 0;
            List<Language> languages = new()
            {
                Language.English,
                Language.Japanese,
                Language.German,
                Language.French,
                Language.Korean,
                Language.ChineseSimplified,
                // Language.ChineseTraditional
            };

            if (!File.Exists(ConfigFilePath))
            {
                writer.WriteLine("Config was not found: " + ConfigFilePath);
                return;
            }
            string gameDirectory = config!.GameDirectory;

            if (!Directory.Exists(gameDirectory))
            {
                writer.WriteLine("Error: The game directory does not exist: " + gameDirectory);
                return;
            }
            try
            {
                Dictionary<Language, dynamic> gameData = utils.GetGameData(
                    gameDirectory,
                    languages
                );
                StringBuilder csvItem = utils.GetBuffsCsv(gameData, languages);
                StringBuilder csvRecipe = utils.GetRecipesCsv(gameData, languages);

                // File.WriteAllText("item.csv", csvItem.ToString());
                // File.WriteAllText("recipe.csv", csvRecipe.ToString());
                // File.WriteAllText("item.csv", csvItem.ToString());
                utils.WriteCsvMemory(csvRecipe, csvItem, sharedMemorySize, sharedMemoryName);
            }
            catch (DirectoryNotFoundException ex)
            {
                writer.WriteLine("Directory not found: " + ex.Message);
            }
            catch (Exception ex)
            {
                writer.WriteLine("Unhandled exception: " + ex.Message + "\n\n" + ex.StackTrace);
            }
        }
    }
}
