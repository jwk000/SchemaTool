using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    enum eFlagType
    {
        INVALID, BOOL, INT, FLOAT, STRING, TIME, ARRAY
    }
    class FlagType
    {
        public eFlagType m_type = eFlagType.INVALID;
        public char m_delimiter = ',';
        public FlagType m_inner_type = null;
    }
    class FlagInfo
    {
        public FlagType m_field_type = new FlagType();
        public bool m_unique = false;
        public bool m_default = false;
        public bool m_ignore = false;
        public bool m_nullable = false;
        public bool m_primary = false;
        public bool m_client = false;
        public bool m_server = false;
    }
    static class Excel2LuaTool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void ExportExcel(string excelpath, string exportpath, string format)
        {
            Stopwatch watcher = new Stopwatch();
            watcher.Start();

            foreach (var info in ExcelLoader.tables.Values)
            {
                //跳过非本目录下的excel
                if (!info.ExcelPath.StartsWith(excelpath))
                {
                    continue;
                }
                string filedir = Path.GetDirectoryName(info.ExcelPath.Replace(excelpath, exportpath));
                if (!Directory.Exists(filedir))
                {
                    Directory.CreateDirectory(filedir);
                }

                if (format.Contains("lua"))
                {
                    string filepath = Path.Combine(filedir, info.ExcelName + ".lua");
                    ExportLua(info, filepath);
                }
                if (format.Contains("csv"))
                {
                    string filepath = Path.Combine(filedir, info.ExcelName + ".csv");
                    ExportCsv(info, filepath);
                }
            }
            watcher.Stop();

            logger.Info($"=============Excel导出完毕，用时{watcher.ElapsedMilliseconds}毫秒===========");
        }

        //导出lua
        public static void ExportLua(ExcelInfo info, string filepath)
        {
            if (File.Exists(filepath))
            {
                File.SetAttributes(filepath, FileAttributes.Normal);
            }
            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("return {");
                var itor = info.ExcelRows.GetEnumerator();

                while (itor.MoveNext())
                {
                    var key = itor.Current.Key;
                    var row = itor.Current.Value;
                    FlagInfo flag = info.FieldFlags[0];
                    key = FlagTypeToLua(key, flag.m_field_type);

                    string affix = null;
                    sw.Write($"[{key}]={{");
                    for (int i = 0; i < row.Count; i++)
                    {
                        flag = info.FieldFlags[i];
                        if (flag.m_ignore)
                        {
                            continue;
                        }
                        string v = row[i];
                        if (string.IsNullOrEmpty(v))
                        {
                            if (flag.m_default)
                            {
                                v = FlagTypeDefaultLuaValue(flag.m_field_type);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            v = FlagTypeToLua(v, flag.m_field_type);
                        }
                        //多级字段
                        if (info.FieldNames[i].Contains('.'))
                        {
                            string[] ss = info.FieldNames[i].Split('.');
                            if (affix == null)
                            {
                                affix = ss[0];
                                sw.Write($"{affix}={{");
                            }
                            else if (affix != ss[0])
                            {
                                sw.Write("}, ");
                                affix = ss[0];
                                sw.Write($"{affix}={{");
                            }
                            sw.Write($"{ss[1]}={v}, ");
                        }
                        else
                        {
                            if (affix != null)
                            {
                                sw.Write("}, ");
                                affix = null;
                            }
                            sw.Write($"{info.FieldNames[i]}={v}, ");
                        }

                    }
                    if (affix != null)
                    {
                        sw.Write("}");
                        affix = null;
                    }
                    sw.WriteLine("},");
                }
                sw.WriteLine($"}},'{info.KeyFieldName}'");

                sw.Close();
            }

        }


        public static void ExportCsv(ExcelInfo info, string filepath)
        {
            if (File.Exists(filepath))
            {
                File.SetAttributes(filepath, FileAttributes.Normal);
            }

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                //表头
                for (int i = 0; i < info.FieldNames.Count; i++)
                {
                    sw.Write(info.FieldNames[i]);
                    if (i < info.FieldNames.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.WriteLine();
                var itor = info.ExcelRows.GetEnumerator();
                while (itor.MoveNext())
                {
                    var row = itor.Current.Value;
                    for (int i = 0; i < row.Count; i++)
                    {
                        var flag = info.FieldFlags[i];
                        if (flag.m_ignore)
                        {
                            continue;
                        }
                        string v = row[i];
                        if (string.IsNullOrEmpty(v))
                        {
                            if (flag.m_default)
                            {
                                v = Excel2LuaTool.FlagTypeDefaultCsvValue(flag.m_field_type);
                            }
                            else
                            {
                                if (i < row.Count - 1)
                                {
                                    sw.Write(',');
                                }
                                continue;
                            }
                        }
                        if (v.Contains(","))
                        {
                            v = $"\"{v}\"";
                        }
                        v = Excel2LuaTool.FlagTypeToCsv(v, flag.m_field_type);
                        sw.Write(v);
                        if (i < row.Count - 1)
                        {
                            sw.Write(',');
                        }
                    }
                    sw.WriteLine();
                }
                sw.Close();
            }

        }


        public static FlagInfo ParseFlag(string flag)
        {
            FlagInfo info = new FlagInfo();
            FlagType ftype = info.m_field_type;
            for (int i = 0; i < flag.Length; i++)
            {
                char f = flag[i];
                if (f == 'a' || f == 'h')
                {
                    ftype.m_type = eFlagType.ARRAY;
                    ftype.m_inner_type = new FlagType();
                    if(ftype != info.m_field_type)
                    {
                        ftype.m_delimiter=',';
                    }
                    else if (flag.Contains(','))
                    {
                        ftype.m_delimiter = ',';
                    }
                    else if (flag.Contains('|'))
                    {
                        ftype.m_delimiter = '|';
                    }
                    else
                    {
                        return null;
                    }
                    ftype = ftype.m_inner_type;
                    flag.Remove(i, 1);
                }
            }
            foreach (var f in flag)
            {
                if (f == 'i')
                {
                    ftype.m_type = eFlagType.INT;
                }
                else if (f == 'f')
                {
                    ftype.m_type = eFlagType.FLOAT;
                }
                else if (f == 'b')
                {
                    ftype.m_type = eFlagType.BOOL;
                }
                else if (f == 's')
                {
                    ftype.m_type = eFlagType.STRING;
                }
                else if (f == 't')
                {
                    ftype.m_type = eFlagType.TIME;
                }

                else if (f == '<')
                {
                    info.m_client = true;
                }
                else if (f == '>')
                {
                    info.m_server = true;
                }
                else if (f == '*')//新增忽略功能
                {
                    info.m_ignore = true;
                }
                else if (f == 'p')
                {
                    info.m_primary = true;
                }
                else if (f == 'u')
                {
                    info.m_unique = true;
                }
                else if (f == 'e')
                {
                    info.m_nullable = true;
                }
                else if (f == 'd')
                {
                    info.m_default = true;
                }
            }

            if (info.m_client == false && info.m_server == false)
            {
                info.m_client = true;
                info.m_server = true;
            }

            return info;
        }

        public static bool CheckFlag(string data, FlagInfo flag)
        {
            if (flag.m_nullable || flag.m_ignore)
            {
                return true;
            }
            if (!CheckFlagType(data, flag.m_field_type))
            {
                return false;
            }
            return true;
        }

        public static bool CheckFlagType(string data, FlagType type)
        {
            if (type.m_type == eFlagType.BOOL)
            {
                return data.ToLower() == "true" || data.ToLower() == "false" || data == "0" || data == "1";
            }
            if (type.m_type == eFlagType.FLOAT)
            {
                return float.TryParse(data, out float f);
            }
            if (type.m_type == eFlagType.INT)
            {
                return int.TryParse(data, out int i);
            }
            if (type.m_type == eFlagType.TIME)
            {
                return DateTime.TryParse(data, out DateTime dt);
            }
            if (type.m_type == eFlagType.STRING)
            {
                return true;
            }
            if (type.m_type == eFlagType.ARRAY)
            {
                string[] ss = data.Split(type.m_delimiter);
                foreach (string s in ss)
                {
                    if (!CheckFlagType(s, type.m_inner_type))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        public static string ToLua(string data, FlagInfo flag)
        {
            return FlagTypeToLua(data, flag.m_field_type);
        }

        public static string FlagTypeToLua(string data, FlagType type)
        {
            if (type.m_type == eFlagType.ARRAY)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                int idx = 1;
                string[] ss = data.Split(type.m_delimiter);
                foreach (string s in ss)
                {
                    string x = FlagTypeToLua(s, type.m_inner_type);
                    sb.Append($"[{idx++}]={x}, ");
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (type.m_type == eFlagType.BOOL)
            {
                if (data.ToLower() == "true" || data == "1")
                {
                    return "true";
                }
                return "false";
            }
            if (type.m_type == eFlagType.STRING)
            {
                if (data.Contains("\n"))
                {
                    return $"[[{data}]]";
                }
                return $"\"{data}\"";
            }
            if (type.m_type == eFlagType.TIME)
            {
                return $"\"{data}\"";
            }

            return data;
        }

        public static string FlagTypeDefaultLuaValue(FlagType ftype)
        {
            eFlagType type = ftype.m_type;
            if (type == eFlagType.BOOL)
            {
                return "false";
            }
            if (type == eFlagType.STRING)
            {
                return "\"\"";
            }
            if (type == eFlagType.INT || type == eFlagType.FLOAT)
            {
                return "0";
            }
            return "";
        }

        public static string FlagTypeToCsv(string data, FlagType type)
        {
            if (type.m_type == eFlagType.BOOL)
            {
                if (data.ToLower() == "true" || data == "1")
                {
                    return "1";
                }
                return "0";
            }
            return data;
        }
        public static string FlagTypeDefaultCsvValue(FlagType type)
        {
            if (type.m_type == eFlagType.BOOL)
            {
                return "0";
            }
            if (type.m_type == eFlagType.STRING)
            {
                return "";
            }
            if (type.m_type == eFlagType.INT || type.m_type == eFlagType.FLOAT)
            {
                return "0";
            }
            return "";
        }

    }
}
