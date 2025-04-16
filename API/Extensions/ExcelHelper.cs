using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace CMS.API.Extensions;

public static class ExcelHelper
{
    public static byte[] CreateFile<T>(List<T> source, string Title = "")
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Sheet1");

        #region Style
        var font = workbook.CreateFont();
        font.FontHeightInPoints = 12;
        font.FontName = "Calibri";
        font.IsBold = true;


        var style = workbook.CreateCellStyle();
        style.WrapText = true;
        style.Alignment = HorizontalAlignment.Center;
        style.VerticalAlignment = VerticalAlignment.Center;
        style.SetFont(font);


        var sum_style = workbook.CreateCellStyle();
        sum_style.WrapText = true;
        sum_style.Alignment = HorizontalAlignment.Right;
        sum_style.VerticalAlignment = VerticalAlignment.Center;
        sum_style.SetFont(font);
        #endregion



        IRow row = sheet.CreateRow(0);
        row.Height = (short)(row.Height * 3);
        ICell cell = row.CreateCell(0);
        cell.SetCellValue(Title);



        var properties = typeof(T).GetProperties();


        var rowHeader = sheet.CreateRow(1);
        var colIndex = 0;

        foreach (var property in properties)
        {
            cell = rowHeader.CreateCell(colIndex);
            cell.SetCellValue(property.Name);
            cell.CellStyle = style;

            int colValueLength = source.Select(x => $"{property.GetValue(x) ?? ""}".Length).ToList().Max(x => x);
            colValueLength = colValueLength > property.Name.Length ? colValueLength : property.Name.Length;
            colValueLength = colValueLength > 100 ? 100 : colValueLength + 8;
            sheet.SetColumnWidth(colIndex, colValueLength * 255);

            colIndex++;
        }
        //end header

        sheet.GetRow(0).GetCell(0).CellStyle = style;
        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, properties.Length - 1));
        sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, properties.Length - 1));
        sheet.CreateFreezePane(0, 2);

        //content
        var rowNum = 2;
        foreach (var item in source)
        {
            var rowContent = sheet.CreateRow(rowNum);

            var colContentIndex = 0;
            foreach (var property in properties)
            {
                var cellContent = rowContent.CreateCell(colContentIndex);
                var value = property.GetValue(item, null);

                if (value == null)
                {
                    cellContent.SetCellValue("");
                }
                else if (property.PropertyType == typeof(string))
                {
                    cellContent.SetCellValue(value.ToString());
                }
                else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    cellContent.SetCellValue(Convert.ToInt32(value));
                }
                else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?) || property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
                {
                    cellContent.SetCellValue(Convert.ToDouble(value));
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    var dateValue = (DateTime)value;
                    cellContent.SetCellValue(dateValue.ToString("yyyy-MM-dd hh:mm:ss tt"));
                }
                else cellContent.SetCellValue(value.ToString());

                colContentIndex++;
            }

            rowNum++;
        }

        //end content


        var colList = properties.Select(x => x.Name.ToLower()).ToList();
        int amtIndex = colList.IndexOf("amount");

        if (amtIndex >= 0)
        {
            if (rowNum > 4)
            {
                row = sheet.CreateRow(rowNum++);
                row.Height = (short)(row.Height * 1.5);

                var prop = properties[amtIndex];
                var total = source.Select(x => (double?)prop.GetValue(x)).ToList().Sum();
                cell = row.CreateCell(amtIndex);
                cell.SetCellValue(total ?? 0);
                cell.CellStyle = sum_style;
            }
        }


        var stream = new MemoryStream();
        workbook.Write(stream);
        var content = stream.ToArray();

        return content;
    }
}