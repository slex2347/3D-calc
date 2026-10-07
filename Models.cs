using System;
namespace PrintCalc3D {
    public class OrderInputs {
        public double WeightGrams { get; set; }
        public double Hours { get; set; }
        public int PartsCount { get; set; } = 1;
        public int BedsCount { get; set; } = 1;
    }
    public class Calculator {
        public static CalculationResult Calculate(OrderInputs order, AppSettings settings, double printerPrice) {
            double workDays = 21.3;
            double hoursPerMonth = workDays * settings.HoursPerDay;
            double electricityPerHour = settings.PowerKw * settings.ElectricityTariff;
            double salaryPerHour = settings.UseSalary ? (settings.MonthlySalary / hoursPerMonth) : 0;
            double amortizationPerHour = printerPrice / 12 / hoursPerMonth;
            double overheadPerHour = (printerPrice * 0.10) / 6 / hoursPerMonth;
            double totalRatePerHour = electricityPerHour + salaryPerHour + amortizationPerHour + overheadPerHour;
            double totalHours = order.Hours * order.BedsCount;
            double totalWeightKg = (order.WeightGrams * order.PartsCount) / 1000.0;
            double plasticCost = totalWeightKg * settings.FilamentPricePerKg;
            double operationCost = totalHours * totalRatePerHour;
            double costPrice = operationCost + plasticCost;
            double clientPrice = costPrice * (1 + settings.MarkupPercent / 100.0);
            return new CalculationResult {
                CostPrice = Math.Round(costPrice, 2),
                ClientPrice = Math.Round(clientPrice, 2),
                TotalHours = totalHours,
                TotalPlasticCost = Math.Round(plasticCost, 2)
            };
        }
    }
    public class CalculationResult {
        public double CostPrice { get; set; }
        public double ClientPrice { get; set; }
        public double TotalHours { get; set; }
        public double TotalPlasticCost { get; set; }
    }
}
