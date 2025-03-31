namespace XerifeTv.CMS.Models.Abstractions.Interfaces;

public interface ISpreadsheetReaderService
{
  string[][] Read(string[] colluns, MemoryStream fileStream);
}