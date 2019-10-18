using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SchemaTool
{
    class ExcelInfo
    {
        //路径
        public string ExcelPath;
        //名字
        public string ExcelName;
        //字段名-下标
        public Dictionary<string, int> FieldIndex = new Dictionary<string, int>();
        //下标-字段
        public List<string> FieldNames = new List<string>();
        //标记
        public List<FlagInfo> FieldFlags = new List<FlagInfo>();
        //所有行，必有key
        public Dictionary<string, List<string>> ExcelRows = new Dictionary<string, List<string>>();
        //key字段
        public string KeyFieldName;
    }



    static class ExcelLoader
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static Dictionary<string, ExcelInfo> tables = new Dictionary<string, ExcelInfo>();
        public static Dictionary<string, string> changes = new Dictionary<string, string>();
        static MD5 md5 = new MD5CryptoServiceProvider();
        public static void LoadExcel(string dir)
        {
            string md5file=ConfigLoader.appConfig.md5_path;
            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            LoadMd5(md5file);
            int file_count = LoadExcelDir(dir);
            SaveMd5(md5file);
            watcher.Stop();
            logger.Info($"加载{file_count}个表格，用时{watcher.ElapsedMilliseconds}毫秒");
        }

        public static int LoadExcelDir(string dir)
        {
            int file_count = 0;
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                if (Path.GetExtension(file) == ".xlsx")
                {
                    ImportExcelFileNoSchema(file);
                    file_count++;
                }
            }
            foreach (string subdir in Directory.EnumerateDirectories(dir))
            {
                file_count += LoadExcelDir(subdir);
            }
            return file_count;
        }

        public static void LoadMd5(string md5file)
        {
            if (!ConfigLoader.appConfig.use_md5)
            {
                return;
            }
            if (!File.Exists(md5file))
            {
                return;
            }
            string json = File.ReadAllText(md5file);
            changes = JsonConvert.DeserializeObject<Dictionary<string,string>>(json);
        }

        public static void SaveMd5(string md5file)
        {
            if (!ConfigLoader.appConfig.use_md5)
            {
                return;
            }

            using (FileStream fs = new FileStream(md5file, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string json = JsonConvert.SerializeObject(changes);
                    sw.WriteLine(json);
                }
            }
        }


        //导入excel数据，兼容xls2lua
        public static bool ImportExcelFileNoSchema(string filePath)
        {
            logger.Trace("LoadExcel {0}...", filePath);
            string excelName = Path.GetFileNameWithoutExtension(filePath);
            if (tables.ContainsKey(excelName))
            {
                return true;
            }
            //md5
            if (ConfigLoader.appConfig.use_md5)
            {
                string excelMd5;
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] retval = md5.ComputeHash(fs);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in retval)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    excelMd5 = sb.ToString();
                }

                if (!changes.ContainsKey(excelName))
                {
                    changes.Add(excelName, excelMd5);
                }
                else
                {
                    if (changes[excelName] == excelMd5)
                    {
                        return true;
                    }
                    changes[excelName] = excelMd5;
                }
            }

            IWorkbook hssfworkbook;
            try
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    hssfworkbook = new XSSFWorkbook(file);
                }
            }
            catch (Exception e)
            {
                logger.Error("LoadExcel Error: {0}", e.ToString());
                return false;
            }

            ExcelInfo info = new ExcelInfo();
            info.ExcelName = Path.GetFileNameWithoutExtension(filePath);
            info.ExcelPath = filePath;
            //合并所有sheet到一个datatable
            for (int idx = 0; idx < hssfworkbook.NumberOfSheets; idx++)
            {
                ISheet sheet = hssfworkbook.GetSheetAt(idx);
                //#开头的才解析
                if (!sheet.SheetName.StartsWith("#"))
                {
                    continue;
                }
                int keyRowNum = 0;
                //跳过注释
                IEnumerator rows = sheet.GetRowEnumerator();
                while (rows.MoveNext())
                {
                    IRow row = rows.Current as IRow;
                    if (row.GetCell(0).ToString().StartsWith("//"))
                    {
                        keyRowNum++;
                    }
                    else
                    {
                        break;
                    }
                }

                bool inited = false;
                if (info.FieldIndex.Count > 0)
                {
                    inited = true;
                }
                //解析表头
                IRow title = sheet.GetRow(keyRowNum);
                for (int i = 0; i < title.LastCellNum; i++)
                {
                    ICell cell = title.Cells[i];
                    if(cell == null)
                    {
                        logger.Error($"Excel {filePath} title cannot be empty");
                        return false;
                    }
                    string cellstr = title.Cells[i].StringCellValue;
                    if (string.IsNullOrEmpty(cellstr))
                    {
                        break;
                    }
                    string[] fieldstr = cellstr.Split(':');
                    if (fieldstr.Length < 2)
                    {
                        logger.Error($"Excel {filePath} sheet {sheet.SheetName} field {cellstr} miss ':' !!");
                        return false;
                    }
                    string field_name = fieldstr[0];
                    string field_flag = fieldstr[1];
                    if (!inited)
                    {
                        info.FieldIndex.Add(field_name, i);
                        info.FieldNames.Add(field_name);
                        FlagInfo fi = Excel2LuaTool.ParseFlag(field_flag);
                        if (fi == null)
                        {
                            logger.Error($"Excel {filePath} sheet {sheet.SheetName} field {cellstr} flag error {field_flag}");
                            return false;
                        }
                        info.FieldFlags.Add(fi);
                        if (fi.m_primary)
                        {
                            info.KeyFieldName = field_name;
                        }
                    }

                }

                Dictionary<int, HashSet<string>> UniqueValueDict = new Dictionary<int, HashSet<string>>();
                int lineNO = keyRowNum;
                //实际的配置内容
                while (rows.MoveNext())
                {
                    lineNO++;
                    IRow row = (XSSFRow)rows.Current;
                    //判定最后一行
                    var cell0 = row.GetCell(0).ToString();
                    if (cell0 == "///END")
                    {
                        break;
                    }
                    //跳过注释行
                    if (cell0.StartsWith("//"))
                    {
                        continue;
                    }

                    List<string> dr = new List<string>();
                    string key = null;
                    for (int i = 0; i < info.FieldNames.Count; i++)
                    {
                        ICell cell = row.GetCell(i);
                        if (cell == null)
                        {
                            //判断可空
                            if (!info.FieldFlags[i].m_nullable)
                            {
                                logger.Error("{0}第{1}行 {2}配置为空，请指定 'e' 标记", filePath, lineNO, info.FieldNames[i]);
                            }
                            dr.Add(null);
                        }
                        else
                        {
                            //公式转换
                            string cellValue = cell.ToString();
                            if (cell.CellType == CellType.Formula)
                            {
                                switch (info.FieldFlags[i].m_field_type.m_type)
                                {
                                    case eFlagType.INT:
                                        cellValue = ((int)cell.NumericCellValue).ToString();
                                        break;
                                    case eFlagType.FLOAT:
                                        cellValue = ((float)cell.NumericCellValue).ToString();
                                        break;
                                    case eFlagType.STRING:
                                        cellValue = cell.StringCellValue;
                                        break;
                                }
                            }

                            //flag检查
                            if (!Excel2LuaTool.CheckFlag(cellValue, info.FieldFlags[i]))
                            {
                                logger.Error("{0}第{1}行 {2}配置格式错误", filePath, lineNO, info.FieldNames[i]);
                            }

                            if (info.FieldFlags[i].m_primary)
                            {
                                key = cellValue;
                                if (info.ExcelRows.ContainsKey(key))
                                {
                                    logger.Error("{0}第{1}行 {2}主键键值{3}重复", filePath, lineNO, info.FieldNames[i], key);
                                }
                            }
                            if (info.FieldFlags[i].m_unique)
                            {
                                if (!UniqueValueDict.ContainsKey(i))
                                {
                                    UniqueValueDict.Add(i, new HashSet<string>());
                                }
                                var hset = UniqueValueDict[i];
                                if (hset.Contains(cell.ToString()))
                                {
                                    logger.Error("{0}第{1}行 {2}值{3}不允许重复", filePath, lineNO, info.FieldNames[i], key);
                                }
                            }
                            dr.Add(cellValue);
                        }
                    }
                    if (key != null&&!info.ExcelRows.ContainsKey(key))
                    {
                        info.ExcelRows.Add(key, dr);
                    }
                }

            }

            tables.Add(info.ExcelName, info);
            logger.Trace("LoadExcel {0} OK", filePath);

            return true;
        }


    }
}
