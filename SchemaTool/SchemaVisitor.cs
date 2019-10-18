using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime;

namespace SchemaTool
{

    class SchemaVisitor : schemaParserBaseVisitor<object>
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        BufferedTokenStream m_tokens;
        public SchemaVisitor(BufferedTokenStream tokens)
        {
            m_tokens = tokens;
        }
        public override object VisitEnum([NotNull] schemaParser.EnumContext context)
        {
            Enum e = new Enum();
            e.name = context.ID().GetText();
            e.enum_type = eEnumType.Normal;
            foreach (var d in context.desc())
            {
                IDesc desc = VisitDesc(d) as IDesc;
                e.desc.Add(desc);
                if (desc.Name() == "flag")
                {
                    e.enum_type = eEnumType.Flag;
                }
            }
            foreach (var f in context.enum_field())
            {
                EnumField field = VisitEnum_field(f) as EnumField;
                e.fields.Add(field);
            }
            SchemaLoader.enums.Add(e.name, e);
            e.Meta = SchemaLoader.Current;
            SchemaLoader.Current.meta_enum.Add(e.name, e);
            return e;
        }

        public override object VisitEnum_field([NotNull] schemaParser.Enum_fieldContext context)
        {
            EnumField ef = new EnumField();
            ef.name = context.ID().GetText();
            ef.value = int.Parse(context.NUM().GetText());

            //提取注释
            int cmtIdx = context.Stop.TokenIndex + 1;
            IList<IToken> tokens = m_tokens.GetHiddenTokensToRight(cmtIdx);
            if (tokens != null)
            {
                ef.comment = tokens[0].Text;
            }
            return ef;
        }


        public override object VisitSchema([NotNull] schemaParser.SchemaContext context)
        {
            Schema sc = new Schema();
            sc.name_aa_bb = context.ID().GetText();
            foreach (var d in context.desc())
            {
                IDesc desc = VisitDesc(d) as IDesc;
                sc.desc.Add(desc);
            }
            foreach (var f in context.schema_field())
            {
                SchemaField field = VisitSchema_field(f) as SchemaField;
                sc.fields.Add(field.name, field);
                sc.field_list.Add(field);
            }
            if (SchemaLoader.schemas.ContainsKey(sc.name_aa_bb))
            {
                logger.Error($"{sc.name_aa_bb}重定义");
            }
            SchemaLoader.schemas.Add(sc.name_aa_bb, sc);
            sc.Meta=SchemaLoader.Current;
            SchemaLoader.Current.meta_class.Add(sc.name_aa_bb, sc);
            return sc;
        }

        public override object VisitSchema_field([NotNull] schemaParser.Schema_fieldContext context)
        {
            SchemaField sf = new SchemaField();
            sf.name = context.ID().GetText();
            sf.type = VisitField_type(context.field_type()) as FieldType;
            foreach (var d in context.desc())
            {
                IDesc desc = VisitDesc(d) as IDesc;
                sf.desc.Add(desc);
            }
            return sf;
        }


        public override object VisitPrime_type([NotNull] schemaParser.Prime_typeContext context)
        {
            FieldType ft = new FieldType();
            ft.type_name = context.GetText();
            string[] typestring = { "int", "uint", "string", "bool", "float", "double" };
            for (int i = 0; i < typestring.Length; i++)
            {
                if (ft.type_name == typestring[i])
                {
                    ft.type = (eSchemaFieldType)i;
                }
            }

            return ft;
        }

        public override object VisitObject_type([NotNull] schemaParser.Object_typeContext context)
        {
            FieldType ft = new FieldType();
            ft.type_name = context.GetText().Replace("{","").Replace("}","");
            ft.type = eSchemaFieldType.OBJECT;
            if(null == context.DELIMITER())
            {
                ft.delimiter=',';
            }
            else
            {
                ft.delimiter = context.DELIMITER().GetText()[0];
            }
            ft.inner_type = VisitField_type(context.field_type()) as FieldType;
            return ft;
        }

        public override object VisitArray_type([NotNull] schemaParser.Array_typeContext context)
        {
            FieldType ft = new FieldType();
            ft.type_name = context.GetText();
            ft.type = eSchemaFieldType.ARRAY;
            if(null == context.DELIMITER())
            {
                ft.delimiter=',';
            }
            else
            {
                ft.delimiter = context.DELIMITER().GetText()[0];
            }
            ft.inner_type = VisitField_type(context.field_type()) as FieldType;
            return ft;
        }

        public override object VisitEnum_type([NotNull] schemaParser.Enum_typeContext context)
        {
            FieldType ft = new FieldType();
            ft.type_name = context.GetText();
            ft.type = eSchemaFieldType.ENUM;
            return ft;
        }

        public override object VisitRange_desc([NotNull] schemaParser.Range_descContext context)
        {
            RangeDesc rd = new RangeDesc();
            if (context.MIN == null)
            {
                rd.min = int.MinValue;
            }
            else
            {
                rd.min = int.Parse(context.MIN.Text);
            }
            if (context.MAX == null)
            {
                rd.max = int.MaxValue;
            }
            else
            {
                rd.max = int.Parse(context.MAX.Text);
            }
            return rd;
        }

        public override object VisitKey_desc([NotNull] schemaParser.Key_descContext context)
        {
            return new KeyDesc();
        }
        public override object VisitFlag_desc([NotNull] schemaParser.Flag_descContext context)
        {
            FlagDesc fd = new FlagDesc();
            return fd;

        }
        public override object VisitNullable_desc([NotNull] schemaParser.Nullable_descContext context)
        {
            NullableDesc nd = new NullableDesc();
            nd.field = context.COND.Text;
            nd.value=context.VALUE.Text;
            nd.op=context.OP().GetText();
            return nd;
        }

        public override object VisitDefault_desc([NotNull] schemaParser.Default_descContext context)
        {
            DefaultDesc dd = new DefaultDesc();
            dd.value = context.VALUE.Text;
            return dd;
        }

        public override object VisitTarget_desc([NotNull] schemaParser.Target_descContext context)
        {
            TargetDesc td = new TargetDesc();
            foreach (var arg in context.ID())
            {
                td.targets.Add(arg.GetText());
            }
            return td;

        }

        public override object VisitRef_desc([NotNull] schemaParser.Ref_descContext context)
        {
            RefDesc desc = new RefDesc();
            desc.refSchemaName = context.TABLE.Text;
            desc.refSchemaFieldName = context.FIELD.Text;
            if (context.ID().Count() > 2)
            {
                desc.refLimitFieldName = context.COND.Text;
                desc.refLimitFieldValue = context.VALUE.Text;
                desc.refLimitFieldOption=context.OP().GetText();
            }
            return desc;
        }

        public override object VisitBind_desc([NotNull] schemaParser.Bind_descContext context)
        {
            BindDesc desc=new BindDesc();
            desc.field1=context.FA.Text;
            desc.field2=context.FB.Text;
            desc.value1=context.VA.Text;
            desc.value2=context.VB.Text;
            desc.op1=context.OP(0).GetText();
            desc.op2=context.OP(1).GetText();
            return desc;
        }

        public override object VisitMap_desc([NotNull] schemaParser.Map_descContext context)
        {
            MapDesc desc = new MapDesc();
            desc.myfield=context.FA.Text;
            desc.schema = context.TABLE.Text;
            desc.field = context.FIELD.Text;
            return desc;
        }

        public override object VisitDump_desc([NotNull] schemaParser.Dump_descContext context)
        {
            return new DumpDesc();
        }
    }
}
