namespace XerifeTv.CMS.Modules.Abstractions.Interfaces;

public interface ISpreadsheetReaderService
{
    string[][] Read(string[] colluns, MemoryStream fileStream, int worksheetIndex = 0);
}