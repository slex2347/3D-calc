namespace PrintCalc3D;

/// <summary>
/// ВСЕ размеры, отступы и шрифты интерфейса в одном месте.
/// Если что-то обрезается или слишком тесно — правьте числа здесь, остальной код трогать не нужно.
/// </summary>
public static class UiConfig
{
    // ---------------------------------------------------------------- Шрифты
    public static readonly Font Body        = new("Segoe UI", 10f);            // основной текст
    public static readonly Font Small       = new("Segoe UI", 9f);             // мелкие подсказки
    public static readonly Font Input       = new("Segoe UI", 11f);            // текст в полях ввода
    public static readonly Font Button      = new("Segoe UI Semibold", 10f);   // текст на кнопках
    public static readonly Font Title       = new("Segoe UI Semibold", 15f);   // заголовок в шапке
    public static readonly Font CardTitle   = new("Segoe UI Semibold", 13f);   // заголовок карточки «Параметры заказа»
    public static readonly Font Section     = new("Segoe UI Semibold", 11f);   // заголовки разделов
    public static readonly Font Strong      = new("Segoe UI Semibold", 10f);   // выделенные подписи
    public static readonly Font StrongValue = new("Segoe UI Semibold", 10.5f); // выделенные значения
    public static readonly Font Big         = new("Segoe UI Semibold", 30f);   // крупная цена для клиента

    public static readonly Font CostBig     = new("Segoe UI Semibold", 22f);   // себестоимость рядом с ценой
    public const float FitMinFont = 8f;                                         // минимальный размер шрифта при автоподгоне текста

    // ---------------------------------------------------------------- Окно
    public static readonly Size WindowSize    = new(1100, 800);
    public static readonly Size WindowMinSize = new(960, 740);
    public const int WindowPadding = 16;

    // ---------------------------------------------------------------- Шапка
    public const int HeaderMinHeight = 96;                                      // минимальная высота (растёт сама по тексту)
    public static readonly Padding HeaderPadding = new(20, 12, 16, 12);
    public const int ButtonRefreshWidth  = 160;
    public const int ButtonSettingsWidth = 140;

    // ---------------------------------------------------------------- Тело окна
    public const int BodyTopGap = 12;            // отступ между шапкой и карточками
    public const int CardGap = 8;                // половина промежутка между двумя карточками
    public const int LeftColumnPercent = 46;     // ширина карточки ввода
    public const int RightColumnPercent = 54;    // ширина карточки результатов
    public static readonly Padding CardPadding = new(22, 18, 22, 18);

    // ---------------------------------------------------------------- Элементы
    public const int ButtonHeight = 40;
    public const int InputHeight = 38;
}

/// <summary>Все надписи главного окна.</summary>
public static class UiText
{
    public const string WindowTitle = "PrintCalc 3D — Bambu Lab P2S Combo + AMS 2 Pro";
    public const string AppTitle    = "Калькулятор стоимости 3D-печати";
    public const string BtnRefresh  = "Обновить цену";
    public const string BtnSettings = "Настройки";

    public const string BtnCalculate = "Рассчитать";
    public const string HintInitial  = "Введите время и массу, затем нажмите «Рассчитать» (или Enter)";
    public const string HintStale    = "Параметры изменены — нажмите «Рассчитать»";
    public const string HintFresh    = "Расчёт актуален";
    public const string CostTile     = "Себестоимость";
    public const string RowMarkup    = "Наценка (цена − себестоимость)";

    // Левая карточка
    public const string InputsTitle    = "Параметры заказа";
    public const string LblMaterial    = "Материал из базы";
    public const string CustomFilament = "— Свой пластик (ввести цену вручную) —";
    public const string LblPricePerKg  = "Цена пластика, ₽ за кг";
    public const string BtnSaveToDb    = "Сохранить в базу";
    public const string BtnFilamentDb  = "База пластика";
    public const string LblTime        = "Время печати по слайсеру";
    public const string UnitHours      = "ч";
    public const string UnitMinutes    = "мин";
    public const string LblGrams       = "Масса пластика по слайсеру, г";
    public const string ChkSalary      = "Учитывать ЗП оператора";
    public const string LblOrders      = "Заказов в месяц (для окупаемости)";

    // Правая карточка
    public const string ClientPrice    = "Цена для клиента";
    public const string SecRate        = "Ставка печати, ₽ за час";
    public const string RowHoursMonth  = "Часов работы в месяц";
    public const string RowElec        = "Электроэнергия";
    public const string RowSalary      = "ЗП оператора";
    public const string RowHourTotal   = "Итого за час печати";
    public const string SecOrder       = "Заказ";
    public const string RowMass        = "Пластик с запасом";
    public const string RowPlastic     = "Стоимость пластика";
    public const string RowMachine     = "Стоимость времени печати";
    public const string RowCost        = "Себестоимость";
    public const string SecPayback     = "Окупаемость принтера";
    public const string RowProfit      = "Чистая прибыль с заказа";
    public const string RowNeed        = "Заказов в месяц нужно";
    public const string RowPayback     = "Окупится при вашем потоке";
    public const string PaybackNote    =
        "Чистая прибыль считается без амортизации: это реальные деньги, " +
        "которые остаются после пластика, электричества, ЗП и сопутствующих расходов.";
}
