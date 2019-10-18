# SchemaTool 工具说明 V0.1

## 目的

1. 检查Excel配置文件。通过语法约束字段的类型、值、引用关系，SchemaTool可以检查并指出配置错误。
2. 生成Excel表头。这样程序只需要关心schema配置文件了。
3. 生成导出文件。通过配置支持导出格式，在excel和csv、lua之间加了一个转换层。
4. 生成解析代码。解析代码比较固定，可用模板生成。


## 设计

检查excel配置数据，需要根据一个类型描述文件，称为schema配置文件。这个文件描述了配置中使用的枚举、字段、字段类型、字段格式等内容。
schema配置整体上采用js对象语法，有些魔改。配置文件以.sc结尾。
配置的原则是一切数据皆有约束，所以格式都是 `变量：约束` 的形式。

### 枚举

配置格式 `枚举变量 : enum @约束 { 枚举项:枚举值 }`
在Excel配置中，枚举可以使用数字也可以使用枚举项的字符串。

### 对象

配置格式 `对象名字 : schema @约束 { 字段: 字段类型 @字段约束}`

### 类型

int uint float double string
这些基础类型略过不提了

*bool* 布尔值
在excel中取值 true false TRUE FALSE 0 1 都可以解析。

*object* 以schema定义的对象，用 `{对象 分隔符}` 括起来就是对象
对象以字符串的形式配置在Excel中
默认用逗号`,`分隔，不可以指定，直接`{对象}`就可以了。
可指定除了`,`之外的任意字符为分隔符，比如`{object|}`用竖线分隔。

*array* 任意类型用 `[类型 分隔符]` 括起来就表示数组
数组以字符串的形式配置在Excel中，
默认用逗号`,`分隔，不可以指定，直接`[类型]`就可以了。
可指定除了`,`之外的任意字符为分隔符，比如`[int|]`用竖线分隔。
对象数组的格式为 `[{object}|]` 数组和对象要用不同的分隔符号，否则无法解析。

### 约束

- @flag 枚举约束，表示枚举是可以`|`连接起来的值，工具会检查配置枚举是否可以通过或操作计算出来。
- @key 字段约束，表示主键，用于生成解析代码，作为key的字段。主键不能为空，不能重复。
- @range(min,max) 字段约束，表示范围。min不填表示无下限，max不填表示无上限。 **值得注意的是：range可以约束数值大小，也可以约束字符串长度，也可以约束数组长度。**
- @nullable(field==value) 字段约束，表示可空。指定可空条件为field=value。可空字段在excel里可以空着不填，默认不可空。
- @nullable(field!=value) 可空。指定可空条件为field!=value。
- @default(value) 字段约束，表示默认值。可空字段会有一个默认值，不填则使用系统默认值。
- @ref(schema.field, limit==value) 字段约束，表示外部引用一个值。比如ID引用。
schema表示引用了哪个表，field表示引用了哪个字段，limit表示对field的约束条件，也是schema里的一个字段，value是limit字段的约束值。limit和value可以不配置。
如果被检测的字段值为data，其检查逻辑用sql表示就是`select field=data in schema where limit=value;`
- @bind(field1==value1, field2>=value2) 对象约束，绑定字段关系，field1等于value1时，field2>=value2；
- @map(myfield,schema.field) 映射约束，表示myfield字段是schema.field字段的一一映射，取值不能超出field值范围也不能漏值；
## 流程

1. 解析schema配置文件
2. 遍历excel目录，加载所有excel配置文件
3. 检查每个excel表的所有数据格式、类型、约束合法

## 示例

这是物品表的schema配置

```js

// 物品主分类
 ItemType:enum
{
    ItemType_None : 0, //无分类
    ItemType_Use : 1, //消耗品
    ItemType_Material : 2, //材料
}

//物品子类型
 ItemSubType:enum
{
    ItemSubType_Base : 1, //基础
    ItemSubType_Gift : 2, //礼包
    ItemSubType_PetExp : 3, //星灵经验材料
}

//礼包类型
 ItemGiftType:enum
{
    ItemGiftType_General : 1, //普通
    ItemGiftType_Choose : 2, //自选
    ItemGiftType_Random : 3, // 抽奖
}

//物品颜色品质
 ItemColor:enum
{
    ItemColor_White : 1,
    ItemColor_Green : 2,
    ItemColor_Blue : 3,
    ItemColor_Purple : 4,
    ItemColor_Golden : 5,
}

//使用类型
 ItemUseType: enum
{
    ItemUseType_CanotUse : 0, //不能使用
    ItemUseType_ManualUse : 1, //手动使用
    ItemUseType_AutoUse : 2, //自动使用
}

// 通用标志位
ItemDataFlags :enum @flag
{
    Item_Flag_Is_New_Obtain : 1,// 是否新获得
}

//使用效果
ItemUseEffect:enum
{
    AddExp : 1,
    OpenGift : 2,
}

//玩家资产
RoleAsset :schema
{
    assetid:int @range(3000000,3999999) @ref(cfg_item:ID),
    count:int,
}

//使用效果
UseEffect :schema
{
    effectId:ItemUseEffect,
    params:[int],
}

//物品表
 cfg_item:schema
 {
    ID:int               @key  @range(3000000,3999999) ,     //ID
    Name:string,    //名字
    Icon:string,
    Intro:string,
    RpIntro:string,
    ItemType:ItemType ,	//类型
    ItemSubType:ItemSubType,	//子类型
    Color:ItemColor,	//颜色品质
    InBag:bool,
    BagSortIndex:int,
    Overlay:bool ,	//是否堆叠
    CanSale:bool,
    SaleGold:int,
    UseType:ItemUseType ,             //使用类型
    UseEffect:{UseEffect}  @nullable, //使用效果
}

GiftOpenCondition:enum{
    RoleLevel:1
}

OpenCondition:schema{
    cond:GiftOpenCondition,
    param:int,
}

//礼包
 cfg_item_gift:schema
 {
    ID:int    @ref(cfg_item:ID,ItemSubType:2) @key ,  //ID
    ItemGiftType:ItemGiftType ,                       //礼包类型
    ItemList:[{RoleAsset}|]  ,                       //物品列表
    ItemWeight:[int|] @nullable ,                     //物品权重
    DropCount:int, //掉落数量
    OpenCondition:{OpenCondition},     //礼包打开条件
    OpenConditionDesc:string,           //打开条件描述
}


//元素属性
ElementType:enum
{
    ElementType_None : 0,//无
    ElementType_Blue :1, //水
    ElementType_Red : 2,  //火
    ElementType_Green : 3, //森
    ElementType_Yellow :4, //雷
    ElementType_Any : 5,//任意
}

//宠物经验材料
cfg_item_pet_exp:schema{
    ID:int @ref(cfg_item:ID,ItemSubType:3) @key ,
    Exp:int, //转换经验
    Element: ElementType, //元素属性
    Gold:int, //转换需要金币
}

```

## xls2lua 规则说明
用于支持excel导出lua和csv

SchemaTool xls2lua excel_path export_path format

format 选项是lua,csv或任选其一

功能说明：

1. 用//开头的行视为注释，此行不解析，不导出。
2. 第一个非注释行是列名行，列的数量=从第一列一直读到第一个空列为止的数量

列名的规则：

1. 格式必须为：字母数字和_组成的列名:列属性标识
2. 所有字母均会转化为小写
3. 列属性标识
 p  ~ 主键
 u  ~ 值唯一
 i  ~ 整数
 f  ~ 浮点数
 s  ~ 字符串(需要翻译的字符串字段加-标识，比如name:s-<>)
 b  ~ 布尔值（TRUE 或true或1 ，FALSE或false或0）
 t  ~ 时间
 a  ~ 数组
 e  ~ 允许为空
 d  ~ 用默认值代替空（数组：{}，数字：0，布尔：false，字符串：''）
 ,  ~ 指定数组的分隔符为,
 |  ~ 指定数组的分隔符为|
 \  ~ 指定数组的分隔符为回车\n
 <  ~ 只在客户端有用
 >  ~ 只在服务端有用
 h  ~ 和a完全一样，为了兼容已有的配置

string类型，如果string包含换行符，比如 道具描述为：

[审核服]测试

    [审核服]测试

        [审核服]测试

            [审核服]测试

        [审核服]测试

    [审核服]测试

[审核服]测试           

可以直接把这段文件在excel里配置即可，不需要加特殊标记。也可以把换行用\n标识，配成一行，比如：[审核服]测试\n    [审核服]测试\n        [审核服]测试\n            [审核服]测试\n        [审核服]测试\n    [审核服]测试

t 时间，支持三种格式，如下

%d%d:%d%d:%d%d

%d%d/%d%d/%d%d

%d%d/%d%d/%d%d d%d:%d%d:%d%d

比如：

01:01:01 

01/01/01 

01/01/01 01:01:01