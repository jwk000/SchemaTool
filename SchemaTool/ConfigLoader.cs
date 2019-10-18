using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SchemaTool
{
    class CheckConfig
    {
        public string[] excel_path;
    }
    class ExportConfig
    {
        public string excel_path;
        public string export_path;
        public string format;
        public string link_path;
    }

    class DumpCodeConfig
    {
        public string schema_file;
        public string template_dir;
        public string code_dir;
        public string csv_dir;
    }

    class AppConfig
    {
        public string p4_path;
        public bool use_md5;
        public string md5_path;
        public string schema_path;
        public CheckConfig check;
        public ExportConfig[] export;
        public DumpCodeConfig[] dump;
    }

    static class ConfigLoader
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static AppConfig appConfig;
        public static bool LoadConfig(string config)
        {
            try
            {
                string json = File.ReadAllText(config);
                appConfig = JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch (Exception e)
            {
                logger.Error("LoadConfig {0} error: {1}", config, e.Message);
                return false;
            }
            logger.Trace("LoadConfig {0} OK ", config);
            return true;
        }
    }
}
