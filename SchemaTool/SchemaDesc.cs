using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    interface IDesc
    {
        string Name();
    }
    class BindDesc : IDesc
    {
        public string Name() { return "bind"; }
        public string field1;
        public string op1;
        public string value1;
        public string field2;
        public string op2;
        public string value2;
        public override string ToString()
        {
            return $"如果{field1} {op1} {value1}则{field2} {op2} {value2}";
        }
    }
    class MapDesc : IDesc
    {
        public string Name() { return "map"; }
        public string myfield;
        public string schema;
        public string field;
    }
    //引用，根据需求修正
    class RefDesc : IDesc
    {
        public string Name() { return "ref"; }
        public string refSchemaName;
        public string refSchemaFieldName;//引用字段
        public string refLimitFieldName;//限制字段
        public string refLimitFieldValue;//限制值
        public string refLimitFieldOption;
    }
    class RangeDesc : IDesc
    {
        public string Name() { return "range"; }
        public int min;
        public int max;
    }

    class FlagDesc : IDesc
    {
        public string Name() { return "flag"; }
    }

    class KeyDesc : IDesc
    {
        public string Name() { return "key"; }
    }

    class NullableDesc : IDesc
    {
        public string Name() { return "nullable"; }
        public string field;
        public string value;
        public string op;
    }

    class DefaultDesc : IDesc
    {
        public string Name() { return "default"; }
        public string value;
    }

    class TargetDesc : IDesc
    {
        public string Name() { return "target"; }
        public List<string> targets = new List<string>();
    }

    class DumpDesc:IDesc
    {
        public string Name(){return "dump";}

    }
}
