using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace SchemaTool
{
    static class ExcelCreatTool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //创建excel模板
        public static void CreateExcelTemplate(string filepath, string schemaname)
        {
            if(!SchemaLoader.schemas.TryGetValue(schemaname,out Schema schema))
            {
                logger.Error("CreateExcel Error: cannot find schema {0}",schemaname);
                return;
            }
            try
            {
                using (FileStream file = new FileStream(filepath, FileMode.CreateNew, FileAccess.Write))
                {
                    IWorkbook workbook = new XSSFWorkbook(file);
                    ICellStyle cellstyle=workbook.CreateCellStyle();
                    cellstyle.FillForegroundColor= HSSFColor.Green.Index;
                    ISheet sheet = workbook.CreateSheet("#data");
                    //第一行是注释第二行是英文名
                    IRow row0 = sheet.CreateRow(0);
                    IRow row1 = sheet.CreateRow(1);
                    for (int i = 0; i < schema.field_list.Count; i++)
                    {
                        SchemaField field=schema.field_list[i];
                        row0.CreateCell(i).SetCellValue(field.comment);
                        row1.CreateCell(i).SetCellValue(field.name);
                        row1.GetCell(i).CellStyle=cellstyle;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("CreateExcel Error: {0}", e.ToString());
            }

        }

    }
}
