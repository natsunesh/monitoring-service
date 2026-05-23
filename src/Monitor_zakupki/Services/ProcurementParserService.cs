using HtmlAgilityPack;
using Monitor_zakupki.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace htmlparse
{
    public class ProcurementParserService : IProcurementParserService
    {
        string FilePathToAppConfig, FilePathToLogs, FilePathToSavedHtml;

        public ProcurementParserService(string filePathToAppConfig, string filePathToLogs, string filePathToSavedHtml)
        {
            FilePathToAppConfig = filePathToAppConfig ?? throw new ArgumentNullException(nameof(filePathToAppConfig));
            FilePathToLogs = filePathToLogs ?? throw new ArgumentNullException(nameof(filePathToLogs));
            FilePathToSavedHtml = filePathToSavedHtml ?? throw new ArgumentNullException(nameof(filePathToSavedHtml));
        }

        public async Task<List<ProcurementItem>> GetNewProcurementsAsync(CancellationToken cancellationToken = default)
        {

            List<ProcurementItem> procurementItems = new List<ProcurementItem>();
            try
            {
                await CheckSavedHTML();
                var items = JsonSerializer.Deserialize<List<SavedHTML>>(File.ReadAllText(FilePathToSavedHtml)); ;
                foreach (var item in items)
                {
                    var RawHtmlFromFile = item.RawHtml;
                    var RawHtmlFromSite = await DownloadHtmlSilentlyAsync($"https://zakupki.gov.ru/epz/orderplan/search/results.html?searchString={item.Inn}&morphology=on&search-filter=Дате+размещения&structuredCheckBox=on&structured=true&notStructured=false&fz44=on&fz223=on&actualPeriodRangeYearFrom=2020&sortBy=BY_MODIFY_DATE&pageNumber=1&sortDirection=false&recordsPerPage=_10&showLotsInfoHidden=false&searchType=false");

                    if (RawHtmlFromFile != RawHtmlFromSite)
                    {
                        var Doc = new HtmlDocument();
                        Doc.LoadHtml(RawHtmlFromSite);

                        var parentNodes = Doc.DocumentNode.SelectNodes("//div[contains(@class, 'row no-gutters registry-entry__form mr-0')]");

                        if (parentNodes != null && parentNodes.Count > 0)
                        {
                            foreach (var parent in parentNodes)
                            {
                                string newElementHtml = parent.OuterHtml;

                                if (!RawHtmlFromFile.Contains(newElementHtml))
                                {
                                    var NumberAndUrlNode = parent.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]");
                                    var NameAndInnNode = parent.SelectSingleNode(".//div[contains(@class, 'registry-entry__body-href')]");

                                    if (NumberAndUrlNode != null && NameAndInnNode != null)
                                    {
                                        var NumberAndUrl = NumberAndUrlNode.SelectSingleNode(".//a");
                                        var NameAndInn = NameAndInnNode.SelectSingleNode(".//a");

                                        if (NumberAndUrl != null && NameAndInn != null)
                                        {
                                            var Number = NumberAndUrl.InnerText.Trim();
                                            var Url = "https://zakupki.gov.ru" + NumberAndUrl.GetAttributeValue("href", "").ToString();
                                            var Name = NameAndInn.InnerText.Trim();
                                            var Inn = NameAndInn.GetAttributeValue("href", "").Split(new[] { "inn=" }, StringSplitOptions.None)[1].Split('&')[0];
                                            var DateNode = parent.SelectSingleNode(".//div[contains(@class, 'data-block__value')]");
                                            var Date = DateNode != null ? DateNode.InnerText.Trim() : "Дата не найдена";

                                            procurementItems.Add(await SetProcurementItem(Number, Inn, Name, Url, Date));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Элементы с указанным классом не найдены.");
                        }
                        item.RawHtml = RawHtmlFromSite;
                    }
                }
                string json = JsonSerializer.Serialize(items);
                File.WriteAllText(FilePathToSavedHtml, json);
                return procurementItems;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return procurementItems; }
        }
        protected Task<ProcurementItem> SetProcurementItem(string Number, string Inn, string Name, string Url, string Date)
        {
            try
            {
                if (!string.IsNullOrEmpty(Number) && !string.IsNullOrEmpty(Inn) && !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Date))
                {
                    var item = new ProcurementItem(Number, Inn, Name, Url, Date);
                    return Task.FromResult(item);
                }
                else { return null; }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        protected async Task CheckSavedHTML()
        {
            var SavedHTML = JsonSerializer.Deserialize<List<SavedHTML>>(File.ReadAllText(FilePathToSavedHtml));
            string _userSettings = File.ReadAllText(FilePathToAppConfig);
            var root = JsonSerializer.Deserialize<RootSettings>(_userSettings);
            string[] innList = root.UserSettings.InnList;
            if (SavedHTML.Count < innList.Length)
            {
                for (int i = SavedHTML.Count; i < innList.Length; i++)
                {
                    SavedHTML.Add(new SavedHTML());
                }
            }

            for (int i = 0; i < innList.Length; i++)
            {
                var item = SavedHTML[i];

                if (item.Inn != innList[i])
                {
                    item.Inn = innList[i];
                    item.RawHtml = await DownloadHtmlSilentlyAsync($"https://zakupki.gov.ru/epz/orderplan/search/results.html?searchString={item.Inn}&morphology=on&search-filter=Дате+размещения&structuredCheckBox=on&structured=true&notStructured=false&fz44=on&fz223=on&actualPeriodRangeYearFrom=2020&sortBy=BY_MODIFY_DATE&pageNumber=1&sortDirection=false&recordsPerPage=_10&showLotsInfoHidden=false&searchType=false");
                }
                Console.WriteLine(item.Inn);
            }

            string json = JsonSerializer.Serialize(SavedHTML);
            File.WriteAllText(FilePathToSavedHtml, json);
        }


        static async Task<string> DownloadHtmlSilentlyAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }

    }
}