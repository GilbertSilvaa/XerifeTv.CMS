using MongoDB.Driver.Linq;
using OfficeOpenXml;
using XerifeTv.CMS.Modules.Abstractions.Exceptions;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;

namespace XerifeTv.CMS.Modules.Abstractions.Services;

public class SpreadsheetReaderService : ISpreadsheetReaderService
{
    public string[][] Read(string[] colluns, MemoryStream fileStream)
    {
        try
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
                throw new SpreadsheetInvalidException("Planilha vazia ou nao encontrada");

            var spreadsheetColumns = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                spreadsheetColumns.Add(worksheet.Cells[1, col].Text);

            if (!colluns.SequenceEqual(spreadsheetColumns.Where(c => !string.IsNullOrWhiteSpace(c))))
                throw new SpreadsheetInvalidException("Planilha em formato incorreto");

            ICollection<string> rowItemValues = [];
            ICollection<string[]> result = [];

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                if (string.IsNullOrEmpty(worksheet.Cells[row, 1].Text)) continue;

                for (int col = 1; col <= colluns.Length; col++)
                    rowItemValues.Add(worksheet.Cells[row, col].Text);

                result.Add(rowItemValues.ToArray());
                rowItemValues.Clear();
            }

            return result.ToArray();
        }
        catch (Exception)
        {
            throw;
        }
    }
}