using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace SchemaTool
{
    class SchemaLoader
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static Dictionary<string, Schema> schemas = new Dictionary<string, Schema>();
        public static Dictionary<string, Enum> enums = new Dictionary<string, Enum>();
        public static Dictionary<string, Meta> metas = new Dictionary<string, Meta>();

        public static Meta Current;
        public static void LoadSchemaDir(string dir)
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                if (File.GetAttributes(file) == FileAttributes.Directory)
                {
                    LoadSchemaDir(file);
                }
                else if (Path.GetExtension(file) == ".sc")
                {
                    LoadSchema(file);
                }
            }

        }
        public static void LoadSchema(string filepath)
        {
            logger.Trace("LoadSchema {0}...",filepath);
            try
            {
                AntlrFileStream fs = new AntlrFileStream(filepath);
                schemaLexer lexer = new schemaLexer(fs);
                CommonTokenStream token = new CommonTokenStream(lexer);
                schemaParser parser = new schemaParser(token);
                SchemaVisitor visitor = new SchemaVisitor(token);
                Current=new Meta();
                Current.meta_file_path = filepath;
                Current.meta_name_aa_bb = Path.GetFileNameWithoutExtension(filepath);
                Current.meta_name_AaBb= CodeGenTool.NameMangling(Current.meta_name_aa_bb, NameManglingType.AaBb);
                metas.Add(Current.name, Current);
                visitor.Visit(parser.prog());
            }
            catch (Exception e)
            {
                logger.Error("LoadSchema {0} error: {1}", filepath, e.ToString());
                return;
            }
            logger.Trace("LoadSchema {0} OK", filepath);

        }
    }


}
