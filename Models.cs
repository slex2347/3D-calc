using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrintCalc3D;

public class Filament
{
    public string Name { get; set; } = "";
    public double PricePerKg { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class AppSettings
{
    // ---- Внешний вид ----
    public bool DarkTheme { get; set; } = true;

    // ---- Электричество ----
    public bool ElectricityConfigured { get; set; } = false;
    public double ElectricityTariff { get; set; } = 6.5;      // ₽ за кВт·ч
    public DateTime ElectricityUpdated { get; set; } = DateTime.Now;
    public double PrinterPowerW { get; set; } = 150;          // средняя мощность принтера + AMS, Вт

    // ---- Зарплата оператора ----
    public bool SalaryEnabled { get; set; } = true;
    public double MonthlySalary { get; set; } = 60000;
    public double WorkDays { get; set; } = 21.3;
    public double PrinterHoursPerDay { get; set; } = 16;      // сколько часов в сутки принтер реально печатает

    // ---- Принтер ----
    public double PrinterPrice { get; set; } = 110000;        // последняя известная цена (автообновление)
    public DateTime? PriceUpdated { get; set; } = null;
    public bool UseManualPrice { get; set; } = false;
    public double ManualPrinterPrice { get; set; } = 110000;
    public List<string> PriceUrls { get; set; } = new();

    // ---- Формула ----
    public double PaybackMonths { get; set; } = 12;           // срок окупаемости и амортизации
    public double RelatedPercent { get; set; } = 10;          // сопутствующие расходы, % от цены оборудования
    public double RelatedMonths { get; set; } = 6;            // ... за этот период, мес.
    public double PlasticExtraPercent { get; set; } = 10;     // добавка к массе из слайсера
    public double MarginPercent { get; set; } = 50;           // наценка

    // ---- База пластика ----
    public List<Filament> Filaments { get; set; } = DefaultFilaments();

    public double EffectivePrinterPrice => UseManualPrice ? ManualPrinterPrice : PrinterPrice;

    /// <summary>Номер версии настроек: растёт при каждом Save(); по нему сбрасывается кэш ставок в Calculator.</summary>
    [JsonIgnore] public int Revision { get; private set; }

    static List<Filament> DefaultFilaments() => new()
    {
        new Filament { Name = "Bambu PLA Basic",   PricePerKg = 2000 },
        new Filament { Name = "Bambu PLA Matte",   PricePerKg = 2300 },
        new Filament { Name = "Bambu PETG HF",     PricePerKg = 2200 },
        new Filament { Name = "Bambu ABS",         PricePerKg = 2200 },
        new Filament { Name = "Bambu ASA",         PricePerKg = 2500 },
        new Filament { Name = "Bambu TPU 95A HF",  PricePerKg = 3300 },
        new Filament { Name = "Bambu PLA-CF",      PricePerKg = 3500 },
        new Filament { Name = "Bambu PA6-CF",      PricePerKg = 7000 },
        new Filament { Name = "Сторонний PLA",     PricePerKg = 1300 },
        new Filament { Name = "Сторонний PETG",    PricePerKg = 1400 },
    };

    // ---- Хранение ----
    static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrintCalc3D");
    static string FilePath => Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (s != null)
                {
                    if (s.Filaments == null || s.Filaments.Count == 0) s.Filaments = DefaultFilaments();
                    if (s.PriceUrls == null) s.PriceUrls = new();
                    return s;
                }
            }
        }
        catch { /* повреждённый файл — начинаем с настроек по умолчанию */ }
        return new AppSettings();
    }

    public void Save()
    {
        Revision++;
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* нет прав на запись — не критично */ }
    }
}

public class CalcResult
{
    public double HoursPerMonth;
    public double ElecPerHour, SalaryPerHour, AmortPerHour, RelatedPerHour, HourRate;
    public double MassWithExtra, PlasticCost, MachineCost, Cost, Price;
    public double Markup;              // наценка: цена для клиента минус себестоимость
    public double ProfitPerOrder;      // чистая прибыль без учёта амортизации (реальные деньги в кассе)
    public double OrdersNeeded;        // заказов в месяц для окупаемости за PaybackMonths
    public double PaybackMonths;       // срок окупаемости при заданном числе заказов
}

/// <summary>
/// Всё, что зависит только от настроек (но не от конкретного заказа), посчитано один раз и закэшировано.
/// На каждый заказ остаётся несколько умножений.
/// </summary>
public sealed class HourRates
{
    public double HoursPerMonth;
    public double Elec, Salary, Amort, Related, Total;   // ₽ за час печати
    public double Printer;                                // цена принтера
    public double PrinterPerPaybackMonth;                 // цена принтера / срок окупаемости (мес.)
    public double MassFactor;                             // 1 + добавка к массе
    public double MarginFactor;                           // 1 + наценка
}

public static class Calculator
{
    static readonly object Lock = new();
    static AppSettings _owner;
    static int _revision = -1;
    static HourRates _rates;

    /// <summary>Ставки за час: пересчитываются только когда настройки реально изменились (Save()).</summary>
    public static HourRates GetRates(AppSettings s)
    {
        lock (Lock)
        {
            if (_rates != null && ReferenceEquals(_owner, s) && _revision == s.Revision)
                return _rates;

            var k = Build(s);
            _owner = s;
            _revision = s.Revision;
            _rates = k;
            return k;
        }
    }

    static HourRates Build(AppSettings s)
    {
        double printer = s.EffectivePrinterPrice;
        double hpm = Math.Max(1, s.WorkDays * s.PrinterHoursPerDay);
        double invHpm = 1.0 / hpm;
        double payback = Math.Max(1, s.PaybackMonths);
        double relMonths = Math.Max(1, s.RelatedMonths);

        var k = new HourRates
        {
            HoursPerMonth = hpm,
            Printer = printer,
            PrinterPerPaybackMonth = printer / payback,
            Elec = s.PrinterPowerW * 0.001 * s.ElectricityTariff,
            Salary = s.SalaryEnabled ? s.MonthlySalary * invHpm : 0,
            Amort = printer * invHpm / payback,
            Related = printer * (s.RelatedPercent * 0.01) * invHpm / relMonths,
            MassFactor = 1 + s.PlasticExtraPercent * 0.01,
            MarginFactor = 1 + s.MarginPercent * 0.01
        };
        k.Total = k.Elec + k.Salary + k.Amort + k.Related;
        return k;
    }

    public static CalcResult Calc(AppSettings s, double hours, double grams, double pricePerKg, double ordersPerMonth)
    {
        var k = GetRates(s);

        var r = new CalcResult
        {
            HoursPerMonth = k.HoursPerMonth,
            ElecPerHour = k.Elec,
            SalaryPerHour = k.Salary,
            AmortPerHour = k.Amort,
            RelatedPerHour = k.Related,
            HourRate = k.Total
        };

        r.MassWithExtra = grams * k.MassFactor;
        r.PlasticCost = r.MassWithExtra * pricePerKg * 0.001;
        r.MachineCost = hours * k.Total;
        r.Cost = r.MachineCost + r.PlasticCost;
        r.Price = r.Cost * k.MarginFactor;
        r.Markup = r.Price - r.Cost;

        // Реальные деньги = цена минус затраты без амортизации = наценка + амортизация за время печати.
        r.ProfitPerOrder = r.Markup + hours * k.Amort;

        bool profitable = r.ProfitPerOrder > 0;
        r.OrdersNeeded = profitable ? k.PrinterPerPaybackMonth / r.ProfitPerOrder : double.NaN;
        r.PaybackMonths = (ordersPerMonth > 0 && profitable)
            ? k.Printer / (ordersPerMonth * r.ProfitPerOrder) : double.NaN;
        return r;
    }
}

public static class Fmt
{
    public static readonly CultureInfo Ru = new("ru-RU");
    public static string Rub0(double v) => double.IsNaN(v) ? "—" : v.ToString("N0", Ru) + " ₽";
    public static string Rub2(double v) => double.IsNaN(v) ? "—" : v.ToString("N2", Ru) + " ₽";

    public static bool TryNum(string text, out double v)
        => double.TryParse((text ?? "").Trim().Replace(',', '.').Replace(" ", ""),
                           NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    public static double Num(string text) => TryNum(text, out var v) ? v : 0;
}
