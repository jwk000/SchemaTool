using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;

namespace SchemaTool
{
    class ExportInfo : Drop
    {
        ExcelInfo _info;
        int _key_index;
        List<ExportInfoField> _fields = new List<ExportInfoField>();
        public ExportInfo(ExcelInfo info)
        {
            _info = info;
            _key_index = info.FieldIndex[info.KeyFieldName];
            for (int i = 0; i < info.FieldNames.Count; i++)
            {
                string name = info.FieldNames[i];
                FlagInfo flag = info.FieldFlags[i];

                ExportInfoField field = new ExportInfoField(name, flag.m_field_type.m_type == eFlagType.STRING, i);
                _fields.Add(field);
            }
        }
        public int KeyIndex
        {
            get { return _key_index; }
        }
        public List<ExportInfoField> Fields
        {
            get { return _fields; }
        }
        public List<List<string>> Rows
        {
            get { return _info.ExcelRows.Values.ToList(); }
        }
    }

    class ExportInfoField : Drop
    {
        string _name;
        bool _is_string;
        int _index;
        public ExportInfoField(string name, bool isstring, int index)
        {
            _name = name;
            _is_string = isstring;
            _index = index;
        }
        public string Name
        {
            get { return _name; }
        }

        public int Index
        {
            get { return _index; }
        }
        public bool IsString
        {
            get { return _is_string; }
        }
    }

    class ExcelExportTool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static void ExportExcel(string excelpath, string exportpath, string template_file)
        {
            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            string text = File.ReadAllText(template_file);
            Template template = Template.Parse(text);

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
                string ext = Path.GetExtension(template_file);
                string filepath = Path.Combine(filedir, info.ExcelName + ext);
                ExportFile(info, template, filepath);
            }
            watcher.Stop();

            logger.Info($"=============Excel导出完毕，用时{watcher.ElapsedMilliseconds}毫秒===========");
        }


        //导出数据文件
        public static void ExportFile(ExcelInfo info, Template template, string filepath)
        {
            if (File.Exists(filepath))
            {
                File.SetAttributes(filepath, FileAttributes.Normal);
            }

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                string outs = template.Render(Hash.FromAnonymousObject(new { rows = info.ExcelRows.Values.ToArray() }));
                sw.Write(outs);
                sw.Close();
            }
        }
    }
}
