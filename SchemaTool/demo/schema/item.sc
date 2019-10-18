
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
    assetid:int @range(3000000,3999999) @ref(cfg_item.ID),
    count:int,
}

//使用效果
UseEffect :schema
{
    effectId:ItemUseEffect,
    params:[int],
}

//礼包打开条件类型
GiftOpenCondition:enum
{
    RoleLevel:1
}

//礼包打开条件
OpenCondition:schema
{
    cond:GiftOpenCondition,
    param:int,
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

//物品表
cfg_item:schema @dump
{
    ID:int	@key  @range(3000000,3999999) , //ID
    Name:string	@target(lua) ,    //名字
    Icon:string,				//图标
    Intro:string,				//简介
    RpIntro:string,				//详情
    ItemType:ItemType ,         //类型
    ItemSubType:ItemSubType  ,  //子类型
    Color:ItemColor,			//颜色品质
    InBag:bool,					//是否放背包
    BagSortIndex:int,			//背包排序
    Overlay:bool ,              //是否堆叠
    CanSale:bool,				//是否可出售
    SaleGold:int,				//出售金币
    UseType:ItemUseType ,             //使用类型
    UseEffect:{UseEffect}  @nullable(UseType==0), //使用效果
}

//礼包
cfg_item_gift:schema @dump
{
    ID:int    @ref(cfg_item.ID,ItemSubType==2) @key ,  //ID
    ItemGiftType:ItemGiftType ,         //礼包类型
    ItemList:[{RoleAsset}|]  ,          //物品列表
    ItemWeight:[int|] @nullable(ItemGiftType!=3) ,       //物品权重
    DropCount:int,						//掉落数量
    OpenCondition:{OpenCondition},      //礼包打开条件
    OpenConditionDesc:string,           //打开条件描述
}

//宠物经验材料
cfg_item_pet_exp:schema @dump
{
    ID:int @ref(cfg_item.ID,ItemSubType==3) @key , //ID
    Exp:int,				//转换经验
    Element: ElementType,	//元素属性
    Gold:int,				//转换需要金币
}

