using OfficeOpenXml;
using XerifeTv.CMS.Models.Abstractions.Exceptions;
using XerifeTv.CMS.Models.Abstractions.Interfaces;

namespace XerifeTv.CMS.Models.Abstractions.Services;

public class SpreadsheetReaderService : ISpreadsheetReaderService
{
	public string[][] Read(string[] colluns, MemoryStream fileStream)
	{
		try
		{
			using var package = new ExcelPackage(fileStream);
			var worksheet = package.Workbook.Worksheets.FirstOrDefault();
			
			if (worksheet is null)
				throw new SpreadsheetInvalidException("empty spreadsheet or not found");
			
			var spreadsheetColumns = new List<string>();
			for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
				spreadsheetColumns.Add(worksheet.Cells[1, col].Text);
			
			if (!colluns.SequenceEqual(spreadsheetColumns))
				throw new SpreadsheetInvalidException("spreadsheet in incorrect format");

			ICollection<string> rowItemValues = [];
			ICollection<string[]> result = [];

			for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
			{
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