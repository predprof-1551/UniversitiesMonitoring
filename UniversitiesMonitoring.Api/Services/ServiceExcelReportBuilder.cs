using OfficeOpenXml;
using OfficeOpenXml.Style;
using UniversityMonitoring.Data;
using UniversityMonitoring.Data.Models;

namespace UniversitiesMonitoring.Api.Services;

public static class ServiceExcelReportBuilder
{
    public static byte[] BuildExcel(UniversityService service)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Отчет");

        #region Настройка базовых данных и стилей

        sheet.Cells["A1:A4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["D1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells["F2:F3"].Style.Fill.PatternType = ExcelFillStyle.Solid;

        // Устанавливаем базовый текст, который одинаковый для всех отчетов
        sheet.Cells["A1"].Value = "Название сервиса:";
        sheet.Cells["A2"].Value = "Название ВУЗа:";
        
        sheet.Cells["A3"].Value = "Ссылка страницу сервиса";
        sheet.Cells["A3"].Hyperlink = new Uri(service.GenerateUrl());
        sheet.Cells["A3"].Style.Font.UnderLine = true;
        
        sheet.Cells["A4"].Value = "Ссылка на сервис";
        sheet.Cells["A4"].Hyperlink = new Uri(service.Url);
        sheet.Cells["A4"].Style.Font.UnderLine = true;
        
        sheet.Cells["D1"].Value = "Изменения состояний";
        // Индикаторы офлайн и онлайн, чтобы пользователю было понятно
        sheet.Cells["F3"].Value = "Онлайн";
        sheet.Cells["F2"].Value = "Офлайн";
        
        sheet.Cells["A1:A4"].Style.Fill.BackgroundColor.SetColor(1, 7, 152, 234);
        sheet.Cells["A1:A4"].Style.Font.Color.SetColor(1, 255, 255, 255);
        sheet.Cells["A1:A4"].Style.Font.Bold = true;
        
        sheet.Cells["D1"].Style.Fill.BackgroundColor.SetColor(1, 7, 152, 234);
        sheet.Cells["D1"].Style.Font.Color.SetColor(1, 255, 255, 255);
        sheet.Cells["D1"].Style.Font.Bold = true;
        
        sheet.Cells["A:A"].AutoFitColumns();
        // Офлайн
        SetOfflineStyle(sheet.Cells["F2"]);
        SetOnlineStyle(sheet.Cells["F3"]);

        #endregion

        sheet.Cells["B1"].Value = service.Name;
        sheet.Cells["B2"].Value = service.University.Name;

        sheet.Cells["B:B"].AutoFitColumns();
        
        var first20StateChanges = service.UniversityServiceStateChanges.Take(20)
            .OrderByDescending(x => x.ChangedAt).ToArray();

        sheet.Cells[$"D2:D{first20StateChanges.Length + 1}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        
        for (var i = 0; i < first20StateChanges.Length; i++)
        {
            var ctxCell = sheet.Cells[$"D{i + 2}"];
            var ctxChange = first20StateChanges[i];
            
            ctxCell.Value = ctxChange.ChangedAt.ToString("dd.MM.yyyy hh:mm");
            
            if (ctxChange.IsOnline) SetOnlineStyle(ctxCell);
            else SetOfflineStyle(ctxCell);
        }
        
        sheet.Cells["D:D"].AutoFitColumns();

        GC.Collect(0);
        
        return package.GetAsByteArray();
    }
    
    private static void SetOfflineStyle(ExcelRange range)
    {
        range.Style.Font.Color.SetColor(1, 153, 0, 0);
        range.Style.Fill.BackgroundColor.SetColor(1, 255, 205, 205);
    }

    private static void SetOnlineStyle(ExcelRange range)
    {
        range.Style.Font.Color.SetColor(1, 0, 153, 0);
        range.Style.Fill.BackgroundColor.SetColor(1, 205, 255, 205);
    }
}