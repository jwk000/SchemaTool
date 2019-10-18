using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    public enum NameManglingType
    {
        unknown, aaBb, AaBb, aa_bb
    }
    static class CodeGenTool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static NameManglingType CheckNameManglingType(string name)
        {
            if (name.IndexOf('_') >= 0) return NameManglingType.aa_bb;
            if (name[0] >= 'a' && name[0] <= 'z') return NameManglingType.aaBb;
            if (name[0] >= 'A' && name[0] <= 'Z') return NameManglingType.AaBb;
            return NameManglingType.unknown;
        }
        public static string NameMangling(string name, NameManglingType totype)
        {
            NameManglingType t = CheckNameManglingType(name);
            char[] c = name.ToCharArray();
            List<char> tmp = new List<char>();
            List<char> tar = new List<char>();
            for (int i = 0; i < c.Length; i++)
            {
                if (i > 0 && char.IsUpper(c[i]))
                {
                    tmp.Add('_');
                }
                tmp.Add(c[i]);
            }

            if (totype == NameManglingType.AaBb || totype == NameManglingType.aaBb)
            {
                bool toup = totype == NameManglingType.AaBb;
                for (int i = 0; i < tmp.Count; i++)
                {
                    if (tmp[i] == '_')
                    {
                        toup = true;
                        continue;
                    }
                    if (toup)
                    {
                        tar.Add(char.ToUpper(tmp[i]));
                        toup = false;
                    }
                    else
                    {
                        tar.Add(tmp[i]);
                    }
                }

                return new string(tar.ToArray());

            }
            if (totype == NameManglingType.aa_bb)
            {
                string s = new string(tmp.ToArray());
                return s.ToLower();
            }

            return name;
        }

        public static void DumpCode()
        {
            //读取配置里的schema和template文件，code目录，生成codefile文件
            foreach(DumpCodeConfig cfg in ConfigLoader.appConfig.dump)
            {
                string schema_name = Path.GetFileNameWithoutExtension( cfg.schema_file);
                if(SchemaLoader.metas.TryGetValue(schema_name, out Meta meta))
                {
                    string[] tfs = Directory.EnumerateFiles(cfg.template_dir).ToArray();
                    foreach(string tf in tfs)
                    {
                        string file_name = Path.GetFileName(tf);
                        string code_name = file_name.Replace("template", schema_name);
                        string code_file = Path.Combine(cfg.code_dir, code_name);
                        meta.csv_dir = cfg.csv_dir;
                        DumpCodeFile(meta, tf, code_file);
                    }
                }
            }
        }

        public static void DumpCodeFile(Meta meta, string template_file, string codefile)
        {
            string[] lines=File.ReadAllLines(template_file);
            TemplateRuleParser parser = new TemplateRuleParser();
            List<ITemplateRule> rules = parser.Parse(lines.ToList());
            //展开规则
            List<string> code = new List<string>();
            TemplateData data = new TemplateData();
            data.SetGlobalVariant("Meta", meta);
            //meta变量注入
            foreach (var kv in meta.meta_variant)
            {
                data.SetGlobalVariant(kv.Key, kv.Value);
            }
            foreach (var rule in rules)
            {
                var code_line = rule.Apply(data);
                code.AddRange(code_line);
            }
            //删除旧文件
            if (File.Exists(codefile))
            {
                File.SetAttributes(codefile, FileAttributes.Normal);
                File.Delete(codefile);
            }
            //写入文件
            using (FileStream fs = new FileStream(codefile, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    foreach (var line in code)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
            logger.Trace("DumpCode {0} OK", codefile);
        }
    }
}
