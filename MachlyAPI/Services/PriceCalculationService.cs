using MachlyAPI.Models;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.Services;

public class PriceCalculationService
{
    public decimal CalculatePrice(Machine machine, DateTime start, DateTime end, double? quantity = null)
    {
        var days = (end - start).Days;
        if (days < 1) days = 1;

        var basePrice = machine.CategoryData.TarifaBase;
        var operatorPrice = machine.CategoryData.TarifaOperador ?? 0;

        decimal totalPrice = 0;

        switch (machine.Category)
        {
            case MachineCategory.Servicios:
                // Precio por día
                totalPrice = basePrice * days;
                if (machine.CategoryData.WithOperator)
                {
                    totalPrice += operatorPrice * days;
                }
                break;

            case MachineCategory.Semillas:
                // Precio por hectárea
                var hectareas = quantity ?? machine.CategoryData.Hectareas ?? 0;
                totalPrice = basePrice * (decimal)hectareas;
                if (machine.CategoryData.WithOperator)
                {
                    totalPrice += operatorPrice * (decimal)hectareas;
                }
                break;

            case MachineCategory.Caña:
                // Precio por tonelada o kilómetro
                var units = quantity ?? machine.CategoryData.Toneladas ?? machine.CategoryData.Kilometros ?? 0;
                totalPrice = basePrice * (decimal)units;
                if (machine.CategoryData.WithOperator)
                {
                    totalPrice += operatorPrice * (decimal)units;
                }
                break;
        }

        return totalPrice;
    }
}
