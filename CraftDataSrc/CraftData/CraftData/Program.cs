using System.IO.MemoryMappedFiles;
using SaintCoinach;
using SaintCoinach.Xiv;
using SaintCoinach.Ex;
using System.Text;
using SaintCoinach.Xiv.ItemActions;
using System.Runtime.InteropServices;
using SaintCoinach.Xiv.Sheets;
using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.CSharp;

class Program
{
    static void Main(string[] args)
    {
        // Specify the path to the game directory
        const string ConfigFilePath = @"config.txt";

        // Shared Memory 
        const string sharedMemoryName = "Global\\CraftData";
        int sharedMemorySize = 4;

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
            // Load each translation
            ARealmReversed realm = new ARealmReversed(gameDirectory, Language.English);
            ARealmReversed realmJA = new ARealmReversed(gameDirectory, Language.Japanese);
            ARealmReversed realmDE = new ARealmReversed(gameDirectory, Language.German);
            ARealmReversed realmFR = new ARealmReversed(gameDirectory, Language.French);

            // Retrieve the Crafting Recipe sheet
            IXivSheet<Recipe> recipes = realm.GameData.GetSheet<Recipe>();
            IXivSheet<Recipe> recipesJA = realmJA.GameData.GetSheet<Recipe>();
            IXivSheet<Recipe> recipesDE = realmDE.GameData.GetSheet<Recipe>();
            IXivSheet<Recipe> recipesFR = realmFR.GameData.GetSheet<Recipe>();

            // Each recipe has a corresponding table
            IXivSheet recipeLevelTable = realm.GameData.GetSheet<RecipeLevelTable>();

            // Load item sheet
            IXivSheet<Item> items = realm.GameData.GetSheet<Item>();
            IXivSheet<Item> itemsJA = realmJA.GameData.GetSheet<Item>();
            IXivSheet<Item> itemsDE = realmDE.GameData.GetSheet<Item>();
            IXivSheet<Item> itemsFR = realmFR.GameData.GetSheet<Item>();

            // Each item has a corresponding itemAction sheet and each itemAction sheet links to a itemFood sheet
            IXivSheet<ItemAction> itemAction = realm.GameData.GetSheet<ItemAction>();
            IXivSheet<ItemFood> itemFood = realm.GameData.GetSheet<ItemFood>();

            StringBuilder csvItem = new StringBuilder();

            csvItem.AppendLine( // TODO: dynamic header to match item values
                "Key" +
                "Category," +
                "Name" +
                "NameJA" +
                "NameDE" +
                "NameFR" +
                "BuffType" +
                "PercentageValue," +
                "MaxValue," +
                "PercentageValueHQ," +
                "MaxValueHQ," +
                "BuffType" +
                "PercentageValue," +
                "MaxValue," +
                "PercentageValueHQ," +
                "MaxValueHQ,"
            );

            foreach (var item in items)
            {
                if (item.ItemSearchCategory.ToString() == "Medicine" || item.ItemSearchCategory.ToString() == "Meals")
                {
                    if (Int32.TryParse(item.ItemAction["Data[1]"].ToString(), out int j))
                    {
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
                                        break;
                                    }
                                    if (buff[k].ToString() != "True")
                                    {
                                        values.Append($"{buff[k]},");
                                    }
                                }
                                csvItem.AppendLine(
                                $"{itemKey}," +
                                $"{item.ItemSearchCategory}," +
                                $"{item.Name}," +
                                $"{itemsJA[itemKey].Name}," +
                                $"{itemsDE[itemKey].Name}," +
                                $"{itemsFR[itemKey].Name}," +
                                values
                               );
                            }
                        }
                    }


                }
            }

            // Console.OutputEncoding = System.Text.Encoding.Unicode;
            // System.Console.WriteLine(csvItem);
            // File.WriteAllText("item.csv", csvItem.ToString());


            // Open or create the CSV file
                    StringBuilder csv = new StringBuilder();
                    csv.AppendLine(
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
                        csv.AppendLine(
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
                    byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
                    sharedMemorySize += bytes.Length;

                    using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(sharedMemoryName, sharedMemorySize))
                    {
                        using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                        {
                            accessor.Write(0, sharedMemorySize);
                            accessor.WriteArray(4, bytes, 0, bytes.Length);
                            Console.WriteLine("terminate");
                            Console.ReadLine();
                        }
                    }

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
