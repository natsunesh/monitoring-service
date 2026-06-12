using HtmlAgilityPack;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitor_zakupki.Interfaces;
using Monitor_zakupki.Models;

namespace Monitor_zakupki.Services
{
    public class ProcurementParserService : IProcurementParserService
    {
        private readonly ILogger<ProcurementParserService> _logger;
        private readonly string _filePathToAppConfig;
        private readonly string _filePathToLogs;
        private readonly string _filePathToSavedHtml;

        public ProcurementParserService(
            ILogger<ProcurementParserService> logger,
            IOptions<ParserOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var p = options.Value ?? throw new ArgumentNullException(nameof(options));

            _filePathToAppConfig = ResolvePath(p.FilePathToAppConfig);
            _filePathToLogs = ResolvePath(p.FilePathToLogs);
            _filePathToSavedHtml = ResolvePath(p.FilePathToSavedHtml);

            EnsureDirectoryForFile(_filePathToAppConfig);
            EnsureDirectoryForFile(_filePathToLogs);
            EnsureDirectoryForFile(_filePathToSavedHtml);
        }

        public async Task<List<ProcurementItem>> GetNewProcurementsAsync(CancellationToken cancellationToken = default)
        {
            var procurementItems = new List<ProcurementItem>();

            try
            {
                await CheckSavedHTML(cancellationToken);

                var json = await File.ReadAllTextAsync(_filePathToSavedHtml, cancellationToken);
                var items = JsonSerializer.Deserialize<List<SavedHTML>>(json) ?? new List<SavedHTML>();

                foreach (var savedHtmlItem in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rawHtmlFromFile = savedHtmlItem.RawHtml ?? string.Empty;
                    var rawHtmlFromSite = await DownloadHtmlSilentlyAsync(BuildSearchUrl(savedHtmlItem.Inn), cancellationToken);

                    if (rawHtmlFromFile != rawHtmlFromSite)
                    {
                        var doc = new HtmlDocument();
                        doc.LoadHtml(rawHtmlFromSite);

                        var parentNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'row no-gutters registry-entry__form mr-0')]");

                        if (parentNodes != null && parentNodes.Count > 0)
                        {
                            foreach (var parent in parentNodes)
                            {
                                var newElementHtml = parent.OuterHtml;

                                if (!rawHtmlFromFile.Contains(newElementHtml))
                                {
                                    var numberAndUrlNode = parent.SelectSingleNode(".//div[contains(@class, 'registry-entry__header-mid__number')]");
                                    var nameAndInnNode = parent.SelectSingleNode(".//div[contains(@class, 'registry-entry__body-href')]");

                                    if (numberAndUrlNode != null && nameAndInnNode != null)
                                    {
                                        var numberAndUrl = numberAndUrlNode.SelectSingleNode(".//a");
                                        var nameAndInn = nameAndInnNode.SelectSingleNode(".//a");

                                        if (numberAndUrl != null && nameAndInn != null)
                                        {
                                            var number = numberAndUrl.InnerText.Trim();
                                            var url = "https://zakupki.gov.ru" + numberAndUrl.GetAttributeValue("href", "");
                                            var name = nameAndInn.InnerText.Trim();
                                            var inn = ExtractInn(nameAndInn.GetAttributeValue("href", ""));
                                            var dateNode = parent.SelectSingleNode(".//div[contains(@class, 'data-block__value')]");
                                            var date = dateNode != null ? dateNode.InnerText.Trim() : "Дата не найдена";

                                            var procurementItem = SetProcurementItem(number, inn, name, url, date);
                                            if (procurementItem != null)
                                                procurementItems.Add(procurementItem);
                                        }
                                    }
                                }
                            }
                        }

                        savedHtmlItem.RawHtml = rawHtmlFromSite;
                    }
                }

                var updatedJson = JsonSerializer.Serialize(items);
                await File.WriteAllTextAsync(_filePathToSavedHtml, updatedJson, cancellationToken);

                return procurementItems;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Парсинг страницы отменён.");
                return procurementItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка парсера");
                await AppendLogAsync($"Ошибка парсера: {ex}");
                return procurementItems;
            }
        }

        private ProcurementItem? SetProcurementItem(string number, string inn, string name, string url, string date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(number) ||
                    string.IsNullOrWhiteSpace(inn) ||
                    string.IsNullOrWhiteSpace(url) ||
                    string.IsNullOrWhiteSpace(date))
                {
                    return null;
                }

                return new ProcurementItem(number, inn, name, url, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка чтения заказа {Number}", number);
                _ = AppendLogAsync($"Ошибка чтения заказа {number}: {ex}");
                return null;
            }
        }

        private async Task CheckSavedHTML(CancellationToken cancellationToken)
        {
            try
            {
                var savedHtmlJson = await File.ReadAllTextAsync(_filePathToSavedHtml, cancellationToken);
                var savedHtmlItems = JsonSerializer.Deserialize<List<SavedHTML>>(savedHtmlJson) ?? new List<SavedHTML>();

                var userSettingsJson = await File.ReadAllTextAsync(_filePathToAppConfig, cancellationToken);
                var root = JsonSerializer.Deserialize<RootSettings>(userSettingsJson)
                    ?? throw new InvalidOperationException("Не удалось прочитать RootSettings.");

                var innList = root.UserSettings?.InnList ?? Array.Empty<string>();

                if (savedHtmlItems.Count > innList.Length)
                    savedHtmlItems.RemoveRange(innList.Length, savedHtmlItems.Count - innList.Length);

                if (savedHtmlItems.Count < innList.Length)
                {
                    for (int i = savedHtmlItems.Count; i < innList.Length; i++)
                        savedHtmlItems.Add(new SavedHTML());
                }

                for (int i = 0; i < innList.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = savedHtmlItems[i];

                    if (item.Inn != innList[i])
                    {
                        item.Inn = innList[i];
                        item.RawHtml = await DownloadHtmlSilentlyAsync(BuildSearchUrl(item.Inn), cancellationToken);
                    }
                }

                var json = JsonSerializer.Serialize(savedHtmlItems);
                await File.WriteAllTextAsync(_filePathToSavedHtml, json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки сохранённого html");
                await AppendLogAsync($"Ошибка проверки сохранённого html: {ex}");
                throw;
            }
        }

        private static async Task<string> DownloadHtmlSilentlyAsync(string url, CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private static string BuildSearchUrl(string inn)
        {
            return $"https://zakupki.gov.ru/epz/orderplan/search/results.html?searchString={inn}&morphology=on&search-filter=Дате+размещения&structuredCheckBox=on&structured=true&notStructured=false&fz44=on&fz223=on&actualPeriodRangeYearFrom=2020&sortBy=BY_MODIFY_DATE&pageNumber=1&sortDirection=false&recordsPerPage=_10&showLotsInfoHidden=false&searchType=false";
        }

        private static string ExtractInn(string href)
        {
            const string marker = "inn=";
            var idx = href.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;

            var value = href[(idx + marker.Length)..];
            var end = value.IndexOf('&');
            return end >= 0 ? value[..end] : value;
        }

        private static string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(AppContext.BaseDirectory, path);
        }

        private static void EnsureDirectoryForFile(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
        }

        private async Task AppendLogAsync(string message)
        {
            try
            {
                await File.AppendAllTextAsync(
                    _filePathToLogs,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff} {message}{Environment.NewLine}");
            }
            catch
            {
                _logger.LogError("Крит ошибка за пределами");
            }
        }
    }
}