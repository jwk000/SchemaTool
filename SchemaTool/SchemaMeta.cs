using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    enum eSchemaFieldType
    {
        INT, UINT, STRING, BOOL, FLOAT, DOUBLE, ARRAY, OBJECT, ENUM
    }


    class FieldType
    {
        public string type_name;//名字
        public eSchemaFieldType type;//字段类型
        public FieldType inner_type;//元素类型
        public char delimiter;//数组、对象分隔符
    }
    class SchemaField
    {
        public string comment;
        public string name;
        public FieldType type;
        public List<IDesc> desc = new List<IDesc>();

        public string Comment { get { return comment; } }
        public string Name { get { return name; } }
        public string MetaType
        {
            get
            {
                return GetMetaTypeName(type);
            }
        }
        string GetMetaTypeName(FieldType type)
        {
            switch (type.type)
            {
                case eSchemaFieldType.BOOL:
                    return "bool";
                case eSchemaFieldType.STRING:
                    return "string";
                case eSchemaFieldType.INT:
                    return "int";
                case eSchemaFieldType.UINT:
                    return "uint";
                case eSchemaFieldType.FLOAT:
                    return "float";
                case eSchemaFieldType.DOUBLE:
                    return "double";
                case eSchemaFieldType.ENUM:
                    return "enum";
                case eSchemaFieldType.OBJECT:
                    return "object";
                case eSchemaFieldType.ARRAY:
                    return "array";
            }
            return "error";
        }
        public string CppType
        {
            get
            {
                return GetCppTypeName(type);
            }
        }

        string GetCppTypeName(FieldType type)
        {
            switch (type.type)
            {
                case eSchemaFieldType.BOOL:
                    return "bool";
                case eSchemaFieldType.STRING:
                    return "std::string";
                case eSchemaFieldType.INT:
                    return "int";
                case eSchemaFieldType.UINT:
                    return "unsigned int";
                case eSchemaFieldType.FLOAT:
                    return "float";
                case eSchemaFieldType.DOUBLE:
                    return "double";
                case eSchemaFieldType.ENUM:
                    return type.type_name;
                case eSchemaFieldType.OBJECT:
                    return type.type_name;
                case eSchemaFieldType.ARRAY:
                    return "std::vector<" + GetCppTypeName(type.inner_type) + ">";
            }
            return "error";
        }

        public Schema Object
        {
            get
            {
                if (eSchemaFieldType.OBJECT == type.type)
                {
                    if (SchemaLoader.schemas.TryGetValue(type.type_name, out Schema sc))
                    {
                        return sc;
                    }
                }
                if (eSchemaFieldType.ARRAY == type.type && eSchemaFieldType.OBJECT == type.inner_type.type)
                {
                    if (SchemaLoader.schemas.TryGetValue(type.inner_type.type_name, out Schema sc))
                    {
                        return sc;
                    }
                }

                return null;
            }
        }

        public string ArrayDelimiter
        {
            get
            {
                if (type.type == eSchemaFieldType.ARRAY)
                {
                    return type.delimiter.ToString();
                }
                return null;
            }
        }

        public string InnerMetaType
        {
            get { return GetMetaTypeName(type.inner_type); }
        }

        public string InnerCppType
        {
            get { return GetCppTypeName(type.inner_type); }
        }
    }
    class Schema
    {
        public string comment;
        public string name_aa_bb;
        public List<IDesc> desc = new List<IDesc>();
        public Dictionary<string, SchemaField> fields = new Dictionary<string, SchemaField>();
        public List<SchemaField> field_list = new List<SchemaField>();//按字段顺序排列

        public Meta Meta { get; set; }
        public string Comment { get { return comment; } }
        public string Name { get { return CodeGenTool.NameMangling(name_aa_bb, NameManglingType.AaBb); } }
        public string name { get { return name_aa_bb; } }
        public object[] FieldList { get { return field_list.ToArray(); } }
        public string KeyFieldName
        {
            get
            {
                foreach (SchemaField sf in field_list)
                {
                    if (ExcelCheckTool.GetDesc(sf, "key") != null)
                    {
                        return sf.name;
                    }
                }
                return null;
            }
        }
        public string KeyFieldType
        {
            get
            {
                foreach (SchemaField sf in field_list)
                {
                    if (ExcelCheckTool.GetDesc(sf, "key") != null)
                    {
                        return sf.CppType;
                    }
                }
                return null;
            }
        }
        public string IsDump
        {
            get
            {
                if (ExcelCheckTool.GetDesc(this, "dump") != null)
                    return "true";
                return "false";
            }
        }
        public string CsvPath
        {
            get
            {
                string path = Path.Combine(Meta.csv_dir, name_aa_bb+".csv");
                return path;
            }
        }
    }

    enum eEnumType
    {
        Normal,
        Flag
    }
    class EnumField
    {
        public string comment;
        public string name;
        public int value;
    }
    class Enum
    {
        public string comment;
        public eEnumType enum_type;
        public string name;
        public List<IDesc> desc = new List<IDesc>();
        public List<EnumField> fields = new List<EnumField>();

        public Meta Meta { get; set; }
        public string Comment { get { return comment; } }
        public string Name { get { return name; } }
        public object[] FieldList { get { return fields.ToArray(); } }

    }
    class Meta
    {
        public Dictionary<string, Schema> meta_class = new Dictionary<string, Schema>();
        public Dictionary<string, Enum> meta_enum = new Dictionary<string, Enum>();
        public Dictionary<string, string> meta_variant = new Dictionary<string, string>();
        public string meta_file_path;
        public string meta_name_AaBb;
        public string meta_name_aa_bb;
        public string csv_dir;

        int _auto_id = 0;
        public string AutoIncID { get { return (++_auto_id).ToString(); } }
        public string Name { get { return meta_name_AaBb; } }
        public string name { get { return meta_name_aa_bb; } }
        public object[] ClassList { get { return meta_class.Values.ToArray(); } }
        public object[] EnumList { get { return meta_enum.Values.ToArray(); } }
        public string GetVar(string key) { return meta_variant.TryGetValue(key, out string val) ? val : null; }
        public string DATETIME { get { return DateTime.Now.ToString(); } }

    }
}
