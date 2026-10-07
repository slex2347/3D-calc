using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
#if WEBVIEW2
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
#endif

namespace PrintCalc3D;

/// <summary>
/// Автообновление цены принтера. Для каждой ссылки из настроек:
///  1) обычная загрузка страницы с заголовками настоящего браузера — все ссылки ПАРАЛЛЕЛЬНО (до PerfConfig.MaxParallel),
///     загрузка и разбор страницы идут в пуле потоков, интерфейс не подвисает;
///  2) если не вышло (блокировка бота, цена строится скриптом) — страница открывается
///     в скрытом браузере (по очереди: браузерный компонент живёт только в потоке интерфейса).
/// Цена ищется в разметке товара (meta, JSON-LD, itemprop, data-price), затем по тексту «123 456 ₽».
/// Итоговая цена — медиана по всем ссылкам.
/// </summary>
public static class PriceService
{
    const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/126.0.0.0 Safari/537.36";

    const double MinPlausible = 60000;
    const double MaxPlausible = 400000;

    // Http — с системным прокси Windows; HttpDirect — напрямую (запасной вариант,
    // если системный прокси указан, но не запущен).
    static readonly HttpClient Http = CreateClient(true);
    static readonly HttpClient HttpDirect = CreateClient(false);
    static readonly SemaphoreSlim Gate = new(1, 1);

    sealed class FetchResult
    {
        public string Url = "";
        public string Host = "";
        public double? Price;
        public string Why = "";
    }

    static HttpClient CreateClient(bool useProxy)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            UseCookies = true,
            UseProxy = useProxy,
            MaxConnectionsPerServer = PerfConfig.MaxParallel
        };
        var c = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        var h = c.DefaultRequestHeaders;
        h.TryAddWithoutValidation("User-Agent", UserAgent);
        h.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        h.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.5");
        h.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        return c;
    }

    static async Task<HttpResponseMessage> GetWithFallbackAsync(string url)
    {
        try { return await Http.GetAsync(url).ConfigureAwait(false); }
        catch (HttpRequestException) { return await HttpDirect.GetAsync(url).ConfigureAwait(false); }
    }

    // ------------------------------------------------------------------ Публичный метод

    public static async Task<(bool ok, string message)> UpdateAsync(AppSettings s)
    {
        if (!await Gate.WaitAsync(0))
            return (false, "обновление уже выполняется");

        try
        {
            var urls = s.PriceUrls
                .Select(u => (u ?? "").Trim())
                .Where(u => Uri.IsWellFormedUriString(u, UriKind.Absolute))
                .Distinct()
                .ToList();
            if (urls.Count == 0)
                return (false, "не указаны ссылки на страницы товара (Настройки → Источники цены)");

            // --- Этап 1: все ссылки параллельно в пуле потоков ---
            var found = new FetchResult[urls.Count];
            using (var limit = new SemaphoreSlim(PerfConfig.MaxParallel))
            {
                var tasks = new Task[urls.Count];
                for (int i = 0; i < urls.Count; i++)
                {
                    int idx = i;
                    string url = urls[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        await limit.WaitAsync().ConfigureAwait(false);
                        try { found[idx] = await FetchHttpAsync(url).ConfigureAwait(false); }
                        finally { limit.Release(); }
                    });
                }
                await Task.WhenAll(tasks);
            }

            // --- Этап 2: скрытый браузер для тех, где обычный запрос не помог (по очереди) ---
            var prices = new List<double>();
            var log = new StringBuilder();
            foreach (var r in found)
            {
                if (r.Price == null)
                {
                    try
                    {
                        string html = await RenderAsync(r.Url);
                        r.Price = await Task.Run(() => ExtractPrice(html));   // разбор — вне потока интерфейса
                        if (r.Price != null) r.Why = "";
                        else r.Why += "; в отрисованной странице цена тоже не найдена";
                    }
                    catch (Exception ex)
                    {
                        r.Why += "; браузерный режим недоступен: " + Short(ex);
                    }
                }

                if (r.Price != null)
                {
                    prices.Add(r.Price.Value);
                    log.AppendLine($"✓ {r.Host}: {r.Price.Value:N0} ₽");
                }
                else
                {
                    log.AppendLine($"✗ {r.Host}: {r.Why}");
                }
            }

            if (prices.Count == 0)
                return (false, log.ToString().TrimEnd());

            prices.Sort();
            double median = prices.Count % 2 == 1
                ? prices[prices.Count / 2]
                : (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2.0;

            s.PrinterPrice = Math.Round(median);
            s.PriceUpdated = DateTime.Now;
            s.Save();
            return (true, log.ToString().TrimEnd());
        }
        finally
        {
            Gate.Release();
        }
    }

    // Выполняется в пуле потоков (ConfigureAwait(false)); исключения наружу не выпускает.
    static async Task<FetchResult> FetchHttpAsync(string url)
    {
        var r = new FetchResult { Url = url, Host = SafeHost(url) };
        try
        {
            using var resp = await GetWithFallbackAsync(url).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                r.Why = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
            }
            else
            {
                string html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                r.Price = ExtractPrice(html);
                if (r.Price == null) r.Why = "цена не найдена в HTML (страница строится скриптом)";
            }
        }
        catch (Exception ex)
        {
            r.Why = "ошибка сети: " + Short(ex);
        }
        return r;
    }

    static string SafeHost(string url)
    {
        try { return new Uri(url).Host; }
        catch { return url; }
    }

    static string Short(Exception ex)
    {
        var e = ex;
        while (e.InnerException != null) e = e.InnerException;
        string m = e.Message.Replace("\r", " ").Replace("\n", " ");
        return m.Length > 160 ? m.Substring(0, 160) + "…" : m;
    }

    // ------------------------------------------------------------------ Скрытое окно для браузера

    /// <summary>Окно за пределами экрана, которое не отбирает фокус и не мигает в панели задач.</summary>
    sealed class HiddenHostForm : Form
    {
        public HiddenHostForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-32000, -32000);
            Size = new Size(1280, 900);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;   // WS_EX_TOOLWINDOW — не показывать в Alt+Tab
                cp.ExStyle |= 0x08000000;   // WS_EX_NOACTIVATE — не забирать фокус у главного окна
                return cp;
            }
        }
    }

    // ------------------------------------------------------------------ Скрытый браузер

#if WEBVIEW2
    // Режим Edge WebView2 (включается свойством UseWebView2 в PrintCalc3D.csproj, нужен NuGet-пакет).
    static async Task<string> RenderAsync(string url)
    {
        string dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrintCalc3D", "webview");
        Directory.CreateDirectory(dataDir);

        using var form = new HiddenHostForm();
        var wv = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(wv);
        form.Show();

        var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
        await wv.EnsureCoreWebView2Async(env);
        wv.CoreWebView2.Settings.UserAgent = UserAgent;

        var tcs = new TaskCompletionSource<bool>();
        wv.NavigationCompleted += (_, e) => tcs.TrySetResult(e.IsSuccess);
        wv.CoreWebView2.Navigate(url);

        var finished = await Task.WhenAny(tcs.Task, Task.Delay(30000));
        if (finished != tcs.Task) throw new TimeoutException("страница не загрузилась за 30 секунд");

        await Task.Delay(4000);   // даём скриптам дорисовать цену

        string json = await wv.ExecuteScriptAsync("document.documentElement.outerHTML");
        return JsonSerializer.Deserialize<string>(json) ?? "";
    }
#else
    // Режим по умолчанию: встроенный в Windows компонент WebBrowser (движок Internet Explorer 11).
    // Не требует NuGet и интернета при сборке; современные сайты открывает хуже, чем WebView2.
    static async Task<string> RenderAsync(string url)
    {
        EnableModernIe();

        using var form = new HiddenHostForm();
        var wb = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
        form.Controls.Add(wb);
        form.Show();

        var tcs = new TaskCompletionSource<bool>();
        wb.DocumentCompleted += (_, e) =>
        {
            // событие приходит и для вложенных фреймов — ждём основной документ
            if (wb.Url != null && e.Url != null && e.Url.AbsoluteUri == wb.Url.AbsoluteUri)
                tcs.TrySetResult(true);
        };
        wb.Navigate(url);

        var finished = await Task.WhenAny(tcs.Task, Task.Delay(30000));
        if (finished != tcs.Task) throw new TimeoutException("страница не загрузилась за 30 секунд");

        await Task.Delay(4000);   // даём скриптам дорисовать цену

        var root = wb.Document?.GetElementsByTagName("html");
        return root != null && root.Count > 0 ? root[0].OuterHtml ?? "" : "";
    }

    [DllImport("urlmon.dll")]
    [PreserveSig]
    static extern int CoInternetSetFeatureEnabled(int feature, int flags, [MarshalAs(UnmanagedType.Bool)] bool enable);

    // По умолчанию WebBrowser работает в режиме совместимости IE7 — переключаем на IE11 для этого exe
    // и отключаем щелчок «навигации», который иначе звучит при каждой загрузке страницы.
    static void EnableModernIe()
    {
        try
        {
            string exe = Path.GetFileName(Environment.ProcessPath ?? "PrintCalc3D.exe");
            using var key = Registry.CurrentUser.CreateSubKey(
                @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
            key?.SetValue(exe, 11001, RegistryValueKind.DWord);
        }
        catch { /* нет доступа к реестру — останется режим по умолчанию */ }

        try { CoInternetSetFeatureEnabled(21 /* FEATURE_DISABLE_NAVIGATION_SOUNDS */, 2 /* SET_FEATURE_ON_PROCESS */, true); }
        catch { /* не критично */ }
    }
#endif

    // ------------------------------------------------------------------ Разбор страницы

    static readonly string[] MarkupPatterns =
    {
        // <meta itemprop|property="price|product:price:amount|og:price:amount" content="109990">
        @"<meta[^>]+(?:itemprop|property|name)\s*=\s*[""'](?:price|product:price:amount|og:price:amount)[""'][^>]*content\s*=\s*[""']([^""']+)[""']",
        @"<meta[^>]+content\s*=\s*[""']([^""']+)[""'][^>]*(?:itemprop|property|name)\s*=\s*[""'](?:price|product:price:amount|og:price:amount)[""']",
        // microdata: <span itemprop="price" content="109990">
        @"itemprop\s*=\s*[""']price[""'][^>]*content\s*=\s*[""']([^""']+)[""']",
        // JSON-LD и встроенный JSON: ""price"": 109990  /  ""price"": ""109 990.00""
        @"""price""\s*:\s*""?(\d[\d\s .,]*)",
        @"""lowPrice""\s*:\s*""?(\d[\d\s .,]*)",
        // data-price="109990"
        @"data-(?:product-)?price\s*=\s*[""']([^""']+)[""']",
    };

    // 109 990 ₽ / 109990 руб. / 109&nbsp;990&nbsp;₽ / 109 990 &#8381;
    const string Sep = @"(?:[\s  ]|&nbsp;|&thinsp;)";
    const string TextPattern =
        @"(\d{1,3}(?:" + Sep + @"\d{3})+|\d{5,6})\s*(?:₽|&#8381;|&#x20BD;|руб|р\.|RUB)";

    // Регулярки компилируются один раз; таймаут защищает от «зависания» на странице-монстре.
    static Regex Rx(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            TimeSpan.FromSeconds(3));

    static readonly Regex[] MarkupRegexes = MarkupPatterns.Select(Rx).ToArray();
    static readonly Regex TextRegex = Rx(TextPattern);
    static readonly Regex P2sRegex = Rx("P2S");

    static double? ExtractPrice(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        try
        {
            // Позиции слова «P2S» (по возрастанию) — для поиска цены рядом с названием модели.
            var p2s = new List<int>();
            foreach (Match m in P2sRegex.Matches(html)) p2s.Add(m.Index);

            foreach (var rx in MarkupRegexes)
            {
                var list = Candidates(html, rx);
                if (list.Count > 0) return PickNear(list, p2s);
            }

            var text = Candidates(html, TextRegex);
            return text.Count > 0 ? PickNear(text, p2s) : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    static double PickNear(List<(int idx, double value)> list, List<int> p2s)
    {
        if (p2s.Count > 0)
            foreach (var c in list)
                if (NearP2s(c.idx, p2s)) return c.value;
        return list[0].value;
    }

    // Двоичный поиск ближайшего «P2S» вместо перебора всех пар.
    static bool NearP2s(int idx, List<int> p2s)
    {
        int pos = p2s.BinarySearch(idx);
        if (pos < 0) pos = ~pos;
        if (pos < p2s.Count && Math.Abs(p2s[pos] - idx) <= 4000) return true;
        if (pos > 0 && Math.Abs(p2s[pos - 1] - idx) <= 4000) return true;
        return false;
    }

    static List<(int idx, double value)> Candidates(string html, Regex rx)
    {
        var list = new List<(int, double)>();
        foreach (Match m in rx.Matches(html))
        {
            double? v = ParseNumber(m.Groups[1].Value);
            if (v != null) list.Add((m.Index, v.Value));
        }
        return list;
    }

    static double? ParseNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string s = raw.Replace("&nbsp;", "").Replace("&thinsp;", "")
                      .Replace(" ", "").Replace(" ", "").Replace(" ", "");
        s = Regex.Replace(s, @"[^\d.,]", "");
        if (s.Length == 0) return null;

        // «109990.00» / «109990,00» — дробная часть из 1-2 цифр отбрасывается,
        // «109.990» / «109,990» — разделитель тысяч.
        int sep = Math.Max(s.LastIndexOf('.'), s.LastIndexOf(','));
        string digits = (sep >= 0 && s.Length - sep - 1 is 1 or 2)
            ? Regex.Replace(s.Substring(0, sep), "[.,]", "")
            : Regex.Replace(s, "[.,]", "");

        if (!double.TryParse(digits, out double v)) return null;
        return v >= MinPlausible && v <= MaxPlausible ? v : null;
    }
}
