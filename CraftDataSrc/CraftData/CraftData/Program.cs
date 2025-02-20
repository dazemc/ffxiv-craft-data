using System.Data.Entity.Core.Metadata.Edm;
using System.Drawing.Printing;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CSharp;
using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Xiv;
using SaintCoinach.Xiv.ItemActions;
using SaintCoinach.Xiv.Sheets;

class Program
{
    static void Main(string[] args)
    {
        const string ConfigFilePath = @"config.txt";
        const string sharedMemoryName = "Global\\CraftData";
        int sharedMemorySize = 0;

        if (!File.Exists(ConfigFilePath))
        {
            Console.WriteLine("Error: Config file not found: " + ConfigFilePath);
            return;
        }
        string gameDirectory = File.ReadAllText(ConfigFilePath).Trim();

        if (!Directory.Exists(gameDirectory))
        {
            Console.WriteLine("Error: The game directory does not exist: " + gameDirectory);
            return;
        }

        try
        {
            Dictionary<Language, dynamic> gameData = new() { };

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
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Language not found: " + lang);
                    languages.Remove(lang);
                    i--;
                    continue;
                }
            }

            StringBuilder csvItem = new();

            csvItem.AppendLine(
                "Key,"
                    + "Category,"
                    + "Name,"
                    + "NameJA,"
                    + "NameDE,"
                    + "NameFR,"
                    + "BuffType1,"
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
                    + "MaxValueHQ3"
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
                            if (
                                acceptedBuffs.Contains(buffType)
                                && item.Name.ToString() != "Potion"
                            )
                            {
                                int itemKey = item.Key;
                                var values = new StringBuilder();
                                for (int k = 1; k != i; k++) // first value is exp bonus
                                {
                                    if (buff[k].ToString() == "")
                                    {
                                        values.Append("Nothing");
                                    }
                                    if (
                                        buff[k].ToString() != "True"
                                        && buff[k].ToString() != "False"
                                    )
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

            //         // Console.OutputEncoding = System.Text.Encoding.Unicode;
            //         // System.Console.WriteLine(csvItem);
            //         // File.WriteAllText("item.csv", csvItem.ToString());


            StringBuilder csv = new();
            csv.AppendLine(
                "Key,"
                    + "Level,"
                    + "CraftType,"
                    + "Name,"
                    + "NameJA,"
                    + "NameDE,"
                    + "NameFR,"
                    + "ClassJobLevel,"
                    + "MaterialQualityFactor,"
                    + "DifficultyFactor,"
                    + "QualityFactor,"
                    + "DurabilityFactor,"
                    + "SuggestedCrafsmanship,"
                    + "Stars,"
                    + "Difficulty,"
                    + "Quality,"
                    + "Durability,"
                    + "ProgressDivider,"
                    + "ProgressModifier,"
                    + "QualityDivider,"
                    + "QualityModifier"
            );
            foreach (Recipe recipe in gameData[languages[0]]["recipes"])
            {
                csv.Append(
                    $"\n{recipe.Key}," + $"{recipe.RecipeLevelTable.Key}," + $"{recipe.CraftType},"
                );

                foreach (Language lang in languages)
                {
                    csv.Append($"{gameData[lang]["recipes"][recipe.Key].ResultItem.Name},");
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
            // Console.WriteLine(csv.ToString()[..600]);
            Console.OutputEncoding = Encoding.UTF8;
            System.Console.WriteLine(csvItem.ToString()[..600]);
            byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
            byte[] bytesItem = Encoding.UTF8.GetBytes(csvItem.ToString());
            const int sharedMemoryOffest = 8;
            sharedMemorySize += bytes.Length + bytesItem.Length + sharedMemoryOffest;

            using MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(
                sharedMemoryName,
                sharedMemorySize
            );
            using MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
            accessor.Write(0, bytes.Length);
            accessor.Write(4, bytesItem.Length);
            accessor.WriteArray(sharedMemoryOffest, bytes, 0, bytes.Length);
            accessor.WriteArray(bytes.Length, bytesItem, 0, bytesItem.Length);
            Console.WriteLine("terminate");
            Console.ReadLine();
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
