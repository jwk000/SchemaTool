using System;
using System.Collections.Generic;
using CommandLine;
using Monitor.Core.Utilities;

namespace SchemaTool
{
    class Program
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('c', "check", Required = false, HelpText = "检查配置")]
            public bool Check { get; set; }

            [Option('d', "dump", Required = false, HelpText = "生成代码")]
            public bool Dump { get; set; }

            [Option('e', "export", Required = false, HelpText = "导出配置")]
            public bool Export { get; set; }

            [Value(0, Required = true, HelpText = "配置路径")]
            public string ConfigPath { get; set; }
        }
        //-c check -d dump -e export
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                              .WithParsed<Options>(o =>
                              {
                                  string config_path = o.ConfigPath;
                                  if (!ConfigLoader.LoadConfig(config_path))
                                  {
                                      logger.Error("找不到配置：{0}", config_path);
                                      return;
                                  }
                                  P4Tool.ReadP4Info(ConfigLoader.appConfig.p4_path);
                                  SchemaLoader.LoadSchemaDir(ConfigLoader.appConfig.schema_path);

                                  if (o.Export)
                                  {
                                      foreach (var cfg in ConfigLoader.appConfig.export)
                                      {
                                          ExcelLoader.LoadExcel(cfg.excel_path);
                                          Excel2LuaTool.ExportExcel(cfg.excel_path, cfg.export_path, cfg.format);
                                          P4Tool.PerforceReconcile(cfg.excel_path);
                                          P4Tool.PerforceReconcile(cfg.export_path, cfg.link_path);
                                      }

                                  }
                                  if (o.Check)
                                  {
                                      foreach(var excels in ConfigLoader.appConfig.check.excel_path)
                                      {
                                          ExcelLoader.LoadExcel(excels);
                                      }
                                      ExcelCheckTool.CheckExcel();
                                  }
                                  if (o.Dump)
                                  {
                                      CodeGenTool.DumpCode();
                                      foreach(var cfg in ConfigLoader.appConfig.dump)
                                      {
                                          P4Tool.PerforceReconcile(cfg.code_dir);
                                      }
                                  }
                              });

        }


    }
}
