using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    class ExcelCheckTool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static void CheckExcel()
        {
            foreach (var kv in ExcelLoader.tables)
            {
                ExcelInfo excelInfo = kv.Value;
                if(!SchemaLoader.schemas.TryGetValue(excelInfo.ExcelName, out Schema excelSchema))
                {
                    logger.Info($"Not find schema for Excel {excelInfo.ExcelName} ... skip check!");
                    continue;
                }
                int errorCount = 0;
                logger.Trace($"CheckExcel ${excelInfo.ExcelPath} ...");
                //表级约束
                MapDesc mapdesc=GetDesc(excelSchema,"map")as MapDesc;
                if(mapdesc != null)
                {
                    ExcelLoader.tables.TryGetValue(mapdesc.schema, out ExcelInfo info);
                    if(!CheckMap(excelInfo, mapdesc.myfield, info, mapdesc.field))
                    {
                        logger.Error($"CheckExcel {excelInfo.ExcelName} map error");
                    }
                }
                //行级约束
                BindDesc binddesc = GetDesc(excelSchema, "bind") as BindDesc;
                var itor=excelInfo.ExcelRows.GetEnumerator();
                for (int i = 0; i < excelInfo.ExcelRows.Count; i++)
                {
                    itor.MoveNext();
                    var row = itor.Current;
                    foreach (var fi in excelInfo.FieldIndex)
                    {
                        string fieldName = fi.Key;
                        int idx = fi.Value;
                        try
                        {
                            if(!excelSchema.fields.TryGetValue(fieldName, out SchemaField field))
                            {
                                continue;
                            }
                            string data=row.Value[idx];
                            //可空判断
                            if (string.IsNullOrEmpty(data)||string.IsNullOrWhiteSpace(data))
                            {
                                //NullableDesc nd = GetDesc(field, "nullable") as NullableDesc;
                                //if (nd == null || !CheckNullable(excelInfo, row.Value, nd))
                                //{
                                //    throw new Exception("字段不能为空");
                                //}
                            }
                            else
                            {
                                CheckData(field, data);
                            }
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            logger.Error("数据检查错误：{0} 第{1}行 {2} {3}", excelInfo.ExcelName, i + 1, fieldName, e.Message);
                        }
                    }
                    if (binddesc != null)
                    {
                        if (!CheckBind(excelInfo, row.Value, binddesc))
                        {
                            logger.Error("检查固定关系错误：{0}第{1}行 约定{2}",excelInfo.ExcelName,i+1,binddesc.ToString());
                        }
                    }
                }
                logger.Info("检查{0}表完成，共{1}个配置错误！", excelInfo.ExcelName, errorCount);
            }

            logger.Trace("----------------------检查完毕！---------------------------");

        }

        public static IDesc GetDesc(SchemaField field, string desc)
        {
            foreach (var d in field.desc)
            {
                if (d.Name() == desc)
                {
                    return d;
                }
            }
            return null;
        }
        public static IDesc GetDesc(Schema sc, string desc)
        {
            foreach (var d in sc.desc)
            {
                if (d.Name() == desc)
                {
                    return d;
                }
            }
            return null;
        }

        public static void CheckData(SchemaField field, string data)
        {
            //类型检查
            if (!CheckDataType(field.type, data))
            {
                throw new Exception($"数据类型不是{field.type.type}");
            }
            //字段级约束
            foreach (var desc in field.desc)
            {

                if (desc.Name() == "range")
                {
                    RangeDesc rd = desc as RangeDesc;

                    if (field.type.type == eSchemaFieldType.INT ||
                    field.type.type == eSchemaFieldType.UINT)
                    {
                        if (!CheckIntRange(data, rd))
                        {
                            throw new Exception($"整数{data}不在指定范围内[{rd.min}-{rd.max}]");
                        }
                    }

                    if (field.type.type == eSchemaFieldType.STRING)
                    {
                        if (!CheckStringLength(data, rd))
                        {
                            throw new Exception($"字符串长度不在范围内[{rd.min}-{rd.max}]");
                        }
                    }
                    if (field.type.type == eSchemaFieldType.ARRAY)
                    {
                        var ss = data.Split(field.type.delimiter);
                        if (!CheckArrayLength(ss, rd))
                        {
                            throw new Exception($"数组长度越界[{rd.min}-{rd.max}]");
                        }
                    }
                }

                if (desc.Name() == "ref")
                {
                    RefDesc rd = desc as RefDesc;
                    if (!CheckReference(data, rd))
                    {
                        throw new Exception($"引用的外部配置值{data}不存在，请检查{rd.refSchemaName}.{rd.refSchemaFieldName}={data}");
                    }
                }

            }
        }

        public static bool CheckNullable(ExcelInfo info, List<string> row, NullableDesc desc)
        {
            int idx = info.FieldIndex[desc.field];
            return CheckOption(row[idx],desc.value,desc.op);
        }
        public static bool CheckDataType(FieldType fieldtype, string data)
        {

            switch (fieldtype.type)
            {
                case eSchemaFieldType.STRING:
                    return true;
                case eSchemaFieldType.DOUBLE:
                    return double.TryParse(data, out double d);
                case eSchemaFieldType.FLOAT:
                    return float.TryParse(data, out float f);
                case eSchemaFieldType.INT:
                    return int.TryParse(data, out int i32);
                case eSchemaFieldType.UINT:
                    return uint.TryParse(data, out uint ui32);
                case eSchemaFieldType.BOOL:
                    if (string.Compare(data, "true", true) == 0 ||
                        string.Compare(data, "false", true) == 0 ||
                        string.Compare(data, "0") == 0 ||
                        string.Compare(data, "1") == 0)
                    {
                        return true;
                    }
                    return false;

                case eSchemaFieldType.ARRAY:
                    //检查每个元素的类型
                    foreach (string s in data.Split(fieldtype.delimiter))
                    {
                        if (!CheckDataType(fieldtype.inner_type, s))
                        {
                            return false;
                        }
                    }
                    return true;
                case eSchemaFieldType.OBJECT:
                    //找到引用的object，匹配每个字段的类型
                    string schemaName = fieldtype.inner_type.type_name;
                    if (!SchemaLoader.schemas.TryGetValue(schemaName, out Schema sc))
                    {
                        return false;
                    }
                    string[] ss = data.Split(fieldtype.delimiter);
                    if (ss.Length != sc.field_list.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < sc.field_list.Count; i++)
                    {
                        SchemaField field = sc.field_list[i];
                        if (!CheckDataType(field.type, ss[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                case eSchemaFieldType.ENUM:
                    if (!SchemaLoader.enums.TryGetValue(fieldtype.type_name, out Enum en))
                    {
                        return false;
                    }
                    return CheckEnum(data, en);
            }

            return false;
        }
        public static bool CheckEnum(string data, Enum e)
        {
            if (e.enum_type == eEnumType.Normal)
            {
                //数字版本
                if (int.TryParse(data, out int val))
                {
                    return e.fields.First(f => f.value == val) != null;
                }
                else //字符串版本
                {
                    return e.fields.First(f => f.name == data) != null;
                }
            }
            if (e.enum_type == eEnumType.Flag)
            {
                //数字版本
                if (uint.TryParse(data, out uint val))
                {
                    //取出每个1
                    for (int i = 0; i < 31; i++)
                    {
                        long res = val & (1 << i);
                        if (res > 0 && e.fields.First(f => f.value == res) == null)
                        {
                            return false;
                        }
                    }
                }
                else //字符串版本
                {
                    string[] ss = data.Split('|');
                    foreach (var s in ss)
                    {
                        if (e.fields.First(f => f.name == s) == null)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            //其他情况，不存在的
            return false;
        }
        public static bool CheckIntRange(string data, RangeDesc desc)
        {
            int val = int.Parse(data);
            return val >= desc.min && val <= desc.max;
        }
        public static bool CheckStringLength(string data, RangeDesc desc)
        {
            return data.Length >= desc.min && data.Length <= desc.max;
        }
        public static bool CheckArrayLength(string[] ss, RangeDesc desc)
        {
            return ss.Length >= desc.min && ss.Length < desc.max;
        }
        public static bool CheckReference(string data, RefDesc desc)
        {
            if (!ExcelLoader.tables.TryGetValue(desc.refSchemaName, out ExcelInfo info))
            {
                logger.Error("找不到引用表{0}", desc.refSchemaName);
                return false;
            }
            if (!info.FieldIndex.TryGetValue(desc.refSchemaFieldName, out int refid))
            {
                logger.Error("找不到引用字段{0}", desc.refSchemaFieldName);
                return false;
            }
            int limitId = -1;
            if (!string.IsNullOrEmpty(desc.refLimitFieldName))
            {
                if (!info.FieldIndex.TryGetValue(desc.refLimitFieldName, out limitId))
                {
                    logger.Error("找不到引用字段{0}", desc.refLimitFieldName);
                    return false;
                }
            }
            var ret = info.ExcelRows.Values.ToList().Find(row =>
            {
                if (limitId == -1)
                {
                    return row[refid] == data;
                }
                else
                {
                    return row[refid] == data && row[limitId] == desc.refLimitFieldValue;
                }
            });

            if (ret == null)
            {
                return false;
            }
            return true;
        }
        public static bool CheckMap(ExcelInfo a, string fa, ExcelInfo b, string fb)
        {
            if (!SchemaLoader.schemas.TryGetValue(b.ExcelName, out Schema sc))
            {
                logger.Error($"CheckExcel {a.ExcelPath} map desc error, not find schema {b.ExcelName}");
                return false;
            }
            //b.fb所有值映射到a.fa的所有值，满射
            if(!a.FieldIndex.TryGetValue(fa,out int aidx))
            {
                return false;
            }
            if(!b.FieldIndex.TryGetValue(fb,out int bidx))
            {
                return false;
            }
            foreach (var row in b.ExcelRows.Values)
            {
                string val = row[bidx];
                if(!findValueInTable(a,aidx,val))
                {
                    logger.Error($"Excel {a.ExcelName}.{fa} 映射{b.ExcelName}.{fb}字段错误，值{val}的映射不存在");
                    return false;
                }
            }
            return true;
        }
        static bool findValueInTable(ExcelInfo info,int idx, string value)
        {
            foreach (var r in info.ExcelRows.Values)
            {
                if (r[idx] == value)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool CheckBind(ExcelInfo info, List<string> row, BindDesc desc)
        {
            int idx1= info.FieldIndex[desc.field1];
            int idx2=info.FieldIndex[desc.field2];
            return CheckOption(row[idx1],desc.value1,desc.op1) &&
                CheckOption(row[idx2],desc.value2,desc.op2);
        }
        public static bool CheckOption(string data, string value, string op)
        {
            if (op == "==")
            {
                return data==value;
            }
            if (op == "!=")
            {
                return data!=value;
            }
            if (op == ">")
            {
                return int.Parse(data)>int.Parse(value);
            }
            if (op == ">=")
            {
                return int.Parse(data) >= int.Parse(value);
            }
            if (op == "<")
            {
                return int.Parse(data) < int.Parse(value);
            }
            if (op == "<=")
            {
                return int.Parse(data) <= int.Parse(value);
            }

            return false;
        }
    }
}
