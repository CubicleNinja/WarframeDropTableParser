using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace WarframeDropTableParser
{
    class Program
    {
        static string dataString = string.Empty;
        static List<WarframeDropTable> DropTables = new List<WarframeDropTable>();
        static Regex dropChanceRegex = new Regex(".*\\((.*?)\\)");

        static void Main(string[] args)
        {
            Uri apiURI = new Uri($"https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html?ver={Guid.NewGuid()}");
            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(apiURI).Result;
                if (response.IsSuccessStatusCode)
                {
                    dataString = response.Content.ReadAsStringAsync().Result;
                }
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(dataString);

            LoadTableData(doc, "missionRewards", 1);
            LoadTableData(doc, "relicRewards", 0);
            LoadTableData(doc, "keyRewards", 0);
            LoadTableData(doc, "cetusRewards", 2);
            LoadTableData(doc, "solarisRewards", 2);
            LoadTableData(doc, "deimosRewards", 2);
            LoadDropData(doc, "ModByAvatar", 2);
            LoadDropData(doc, "blueprintByAvatar", 2);
            LoadDropData(doc, "resourceByAvatar", 2);
            LoadDropData(doc, "sigilByAvatar", 2);
            LoadDropData(doc, "additionalItemByAvatar", 2);
            LoadTableData(doc, "transientRewards", 1);

            String output = JsonConvert.SerializeObject(DropTables);

            if (File.Exists("dropTableData_prev.json"))
            {
                File.Delete("dropTableData_prev.json");
            }
            if (File.Exists("dropTableData.json"))
            {
                File.Move("dropTableData.json", "dropTableData_prev.json");
            }
            
            using (StreamWriter sw = new StreamWriter("dropTableData.json"))
            {
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    jw.WriteRaw(output);
                }
            }

            if (FileCompare("dropTableData.json", "dropTableData_prev.json"))
            {
                File.Delete("dropTableData.json");
                File.Move("dropTableData_prev.json", "dropTableData.json");
            }
            else
            {
                using (StreamWriter sw = new StreamWriter("dropTableUpdated.json"))
                {
                    var outputString = $"{{\"LastUpdated\":\"{DateTime.Now}\"}}";
                    using (JsonWriter jw = new JsonTextWriter(sw))
                    {
                        jw.WriteRaw(outputString);
                    }
                }
            }
        }
        static void LoadDropData(HtmlDocument doc, String labelKey, int numLevels)
        {
            var indexPoint = doc.GetElementbyId(labelKey);
            if (indexPoint == null)
                return;

            HtmlNode dropTableData = indexPoint.NextSibling;
            while (dropTableData != null && dropTableData.NodeType != HtmlNodeType.Element)
            {
                dropTableData = dropTableData.NextSibling;
            }
            if (dropTableData == null)
                return;

            var dropTableRows = dropTableData.SelectNodes("tr");

            if (dropTableRows == null)
            {
                dropTableRows = dropTableData.FirstChild.SelectNodes("tr");
            }

            WarframeDropTable warframeDropTable = new WarframeDropTable();
            String rootSourceName = String.Empty;

            foreach (var currentRow in dropTableRows)
            {
                var dropTableHeader = currentRow.SelectNodes("th");

                if (dropTableHeader != null)
                {
                    if (warframeDropTable != null
                        && warframeDropTable.DropTableRewards.Count > 0)
                    {
                        DropTables.Add(warframeDropTable);
                        warframeDropTable = new WarframeDropTable();
                    }

                    if (dropTableHeader.Count() == 2)
                    {
                        warframeDropTable.SourceName = $"{dropTableHeader[0].InnerText}";
                        warframeDropTable.SourceSubName = $"{dropTableHeader[1].InnerText}";
                    }
                }
                else
                {
                    var dropTableItem = currentRow.SelectNodes("td");
                    if (dropTableItem.Count == 3)
                    {
                        string dropChance = dropTableItem[2].InnerText;
                        Match match = dropChanceRegex.Match(dropChance);
                        if (match.Success)
                        {
                            dropChance = match.Groups[1].Value;
                        }

                        DropTableItem relicRewardItem = new DropTableItem
                        {
                            DropTableItemName = dropTableItem[1].InnerText,
                            DropTableItemChance = dropChance
                        };

                        warframeDropTable.DropTableRewards.Add(relicRewardItem);
                    }
                }
            }

            if (warframeDropTable != null && warframeDropTable.DropTableRewards.Count > 0)
            {
                DropTables.Add(warframeDropTable);
                warframeDropTable = new WarframeDropTable();
            }
        }

        static void LoadTableData(HtmlDocument doc, String labelKey, int numLevels)
        {
            var indexPoint = doc.GetElementbyId(labelKey);
            if (indexPoint == null)
                return;

            HtmlNode dropTableData = indexPoint.NextSibling;
            while (dropTableData != null && dropTableData.NodeType != HtmlNodeType.Element)
            {
                dropTableData = dropTableData.NextSibling;
            }
            if (dropTableData == null)
                return;

            var dropTableRows = dropTableData.SelectNodes("tr");

            if (dropTableRows == null)
            {
                dropTableRows = dropTableData.FirstChild.SelectNodes("tr");
            }

            WarframeDropTable warframeDropTable = new WarframeDropTable();
            int numHeader = 0;
            String rootSourceName = String.Empty;
            String subSourceName = String.Empty;

            foreach (var currentRow in dropTableRows)
            {
                var dropTableHeader = currentRow.SelectNodes("th");

                if (dropTableHeader != null)
                {
                    if (warframeDropTable != null
                        && warframeDropTable.DropTableRewards.Count > 0)
                    {
                        #region Temp logic to correct Orphix Venom tables
                        if (warframeDropTable.SourceName.Equals("Operation: Orphix Venom"))
                        {
                            DropTableItem lavosBP = warframeDropTable.DropTableRewards.Where(x => x.DropTableItemName.Equals("Lavos Blueprint") || x.DropTableItemName.Equals("Necramech Aviator") || x.DropTableItemName.Equals("Cedo Blueprint")).FirstOrDefault();
                            if (lavosBP.DropTableItemChance.StartsWith("4."))
                            {
                                warframeDropTable.SourceName = "Operation: Orphix Venom - Endurance";
                            }
                            else if (lavosBP.DropTableItemChance.StartsWith("3."))
                            {
                                warframeDropTable.SourceName = "Operation: Orphix Venom - Advanced";
                            }
                            else if (lavosBP.DropTableItemChance.StartsWith("2."))
                            {
                                warframeDropTable.SourceName = "Operation: Orphix Venom - Normal";
                            }
                        } 
                        #endregion
                        DropTables.Add(warframeDropTable);
                        warframeDropTable = new WarframeDropTable();
                        numHeader = 0;
                    }

                    if (numHeader == 0)
                    {
                        String curHeader = dropTableHeader.First().InnerText;

                        if (!(curHeader.IndexOf("Rotation") >= 0) && !(curHeader.IndexOf("Stage") >= 0))
                        {
                            warframeDropTable.SourceName = curHeader;
                            rootSourceName = curHeader;
                            subSourceName = String.Empty;
                        }
                        else if (!(curHeader.IndexOf("Stage") >= 0))
                        {
                            warframeDropTable.SourceName = $"{rootSourceName}";
                            warframeDropTable.SourceSubName = $"{curHeader}";
                            subSourceName = curHeader;
                        }
                        else
                        {
                            warframeDropTable.SourceName = $"{rootSourceName}";

                            if (!String.IsNullOrEmpty(subSourceName))
                                warframeDropTable.SourceSubName = $"{subSourceName} - {curHeader}";
                            else
                                warframeDropTable.SourceSubName = $"{curHeader}";
                        }
                    }
                    else
                    {
                        String curHeader = dropTableHeader.First().InnerText;

                        if (!(curHeader.IndexOf("Stage") >= 0))
                        {
                            warframeDropTable.SourceName = $"{rootSourceName}";

                            if (!String.IsNullOrEmpty(subSourceName))
                                warframeDropTable.SourceSubName = $"{subSourceName} - {curHeader}";
                            else
                                warframeDropTable.SourceSubName = $"{curHeader}";

                            if (numHeader < numLevels)
                            {
                                subSourceName = warframeDropTable.SourceSubName;
                            }
                        }
                        else
                        {
                            warframeDropTable.SourceName = $"{rootSourceName}";
                            if (!String.IsNullOrEmpty(subSourceName))
                                warframeDropTable.SourceSubName = $"{subSourceName} - {curHeader}";
                            else
                                warframeDropTable.SourceSubName = $"{curHeader}";
                        }
                    }

                    numHeader++;
                }
                else
                {
                    var dropTableItem = currentRow.SelectNodes("td");
                    if (dropTableItem.Count == 2)
                    {
                        string dropChance = dropTableItem[1].InnerText;
                        Match match = dropChanceRegex.Match(dropChance);
                        if (match.Success)
                        {
                            dropChance = match.Groups[1].Value;
                        }
                        DropTableItem relicRewardItem = new DropTableItem
                        {
                            DropTableItemName = dropTableItem[0].InnerText,
                            DropTableItemChance = dropChance
                        };

                        warframeDropTable.DropTableRewards.Add(relicRewardItem);
                    }
                    else if (dropTableItem.Count == 3)
                    {
                        string dropChance = dropTableItem[2].InnerText;
                        Match match = dropChanceRegex.Match(dropChance);
                        if (match.Success)
                        {
                            dropChance = match.Groups[1].Value;
                        }

                        DropTableItem relicRewardItem = new DropTableItem
                        {
                            DropTableItemName = dropTableItem[1].InnerText,
                            DropTableItemChance = dropChance
                        };

                        warframeDropTable.DropTableRewards.Add(relicRewardItem);
                    }
                }
            }

            if (warframeDropTable != null && warframeDropTable.DropTableRewards.Count > 0)
            {
                #region Temp logic to correct Orphix Venom tables
                if (warframeDropTable.SourceName.Equals("Operation: Orphix Venom"))
                {
                    DropTableItem lavosBP = warframeDropTable.DropTableRewards.Where(x => x.DropTableItemName.Equals("Lavos Blueprint") || x.DropTableItemName.Equals("Necramech Aviator") || x.DropTableItemName.Equals("Cedo Blueprint")).FirstOrDefault();
                    if (lavosBP.DropTableItemChance.StartsWith("4."))
                    {
                        warframeDropTable.SourceName = "Operation: Orphix Venom - Endurance";
                    }
                    else if (lavosBP.DropTableItemChance.StartsWith("3."))
                    {
                        warframeDropTable.SourceName = "Operation: Orphix Venom - Advanced";
                    }
                    else if (lavosBP.DropTableItemChance.StartsWith("2."))
                    {
                        warframeDropTable.SourceName = "Operation: Orphix Venom - Normal";
                    }
                }
                #endregion
                DropTables.Add(warframeDropTable);
                warframeDropTable = new WarframeDropTable();
                numHeader = 0;
            }
        }

        // This method accepts two strings the represent two files to
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the
        // files are not the same.
        static  bool FileCompare(string file1, string file2)
        {
            if (!File.Exists(file1) || !File.Exists(file2))
            {
                return false;
            }

            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is
            // equal to "file2byte" at this point only if the files are
            // the same.
            return ((file1byte - file2byte) == 0);
        }
    }
}
