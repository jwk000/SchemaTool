
//技能类型
SkillType:enum
{
    SkillType_Normal : 1, //普通攻击
    SkillType_ChainSkill : 2, //连锁技
    SkillType_Active : 3, //主动技能
    SkillType_Captain : 4, //队长技能
    SkillType_Work : 5, //工作技能
}

//星灵表
cfg_pet:schema @dump
{
    ID:int	@key  @range(1000000,1999999) , //ID
    Name:string	 ,    //名字
    NickName:string,  //英文标志
    ChinaTag:string,  //中文标记
    Desc:string,      //详情
    Head:string ,     //头像
    Body:string  ,    //立绘
    Prefabs:string,     //模型
    Spine:string,       //动画
    Star:int,			//星级
    FirstElement:ElementType ,      //主元素
    SecondElement:ElementType ,     //副元素
    Element2NeedGrade:int,          //副元素解锁阶段
    Tags:[int] ,             //标签
    NormalSkill:int, //普攻技能
    ActiveSkill:int, //主动技能
    CaptainSkill:int, //队长技能
    ChainSkill1:int, //连锁技能1
    ChainSkill2:int, //连锁技能2
    ChainSkill3:int, //连锁技能3
    WorkSkill1:int, //工作技能1
    WorkSkill2:int, //工作技能2
    WorkSkill3:int, //工作技能3
    ExchangeItem:[{RoleAsset}|] //重复星灵转化资源
}

//星灵亲密度
cfg_pet_affinity:schema @dump @map(PetID,cfg_pet.ID)
{
    ID:int @key , 
    PetID:int @ref(cfg_pet.ID),
    AffinityLevel:int , //亲密度等级
    NeedAffintyExp:int, //亲密度经验
    Attack:int, //攻击
    Defence:int, //防御
    Health:int, //血量
    Hit:int, //命中
    Doge:int, //闪避
    Crit:int, //暴击
    CritHurt:int //暴伤
}

//星灵觉醒
cfg_pet_awakening:schema @dump @map(PetID,cfg_pet.ID)
{
    ID:int @key , 
    PetID:int @ref(cfg_pet.ID),
    Awakening:int , //亲密度等级
    NeedStar:int,
    NeedGrade:int,
    NeedItem:[{RoleAsset}|],
    Attack:int, //攻击
    Defence:int, //防御
    Health:int, //血量
    Hit:int, //命中
    Doge:int, //闪避
    Crit:int, //暴击
    CritHurt:int, //暴伤
    NormalSkill:int, //普攻技能
    ActiveSkill:int, //主动技能
    CaptainSkill:int, //队长技能
    ChainSkill1:int, //连锁技能1
    ChainSkill2:int, //连锁技能2
    ChainSkill3:int, //连锁技能3
    WorkSkill1:int, //工作技能1
    WorkSkill2:int, //工作技能2
    WorkSkill3:int, //工作技能3
}

//星灵进阶
cfg_pet_grade:schema @dump @map(PetID,cfg_pet.ID)
{
    ID:int @key , 
    PetID:int @ref(cfg_pet.ID),
    Grade:int , //亲密度等级
    NeedStar:int,
    NeedLevel:int,
    NeedItem:[{RoleAsset}|],
    Head:string ,  //头像
    Body:string  ,  //立绘
    Prefabs:string, //模型
    Spine:string,  //动画
    Shape:string,
    Attack:int, //攻击
    Defence:int, //防御
    Health:int, //血量
    Hit:int, //命中
    Doge:int, //闪避
    Crit:int, //暴击
    CritHurt:int, //暴伤
    NormalSkill:int, //普攻技能
    ActiveSkill:int, //主动技能
    CaptainSkill:int, //队长技能
    ChainSkill1:int, //连锁技能1
    ChainSkill2:int, //连锁技能2
    ChainSkill3:int, //连锁技能3
    WorkSkill1:int, //工作技能1
    WorkSkill2:int, //工作技能2
    WorkSkill3:int, //工作技能3
}

//星灵等级
cfg_pet_level:schema @dump @map(PetID,cfg_pet.ID)
{
    ID:int @key , 
    PetID:int @ref(cfg_pet.ID),
    Grade:int , //阶段
    Level:int, //等级
    NeedExp:int, //所需经验
    Attack:int, //攻击
    Defence:int, //防御
    Health:int, //血量
    Hit:int, //命中
    Doge:int, //闪避
    Crit:int, //暴击
    CritHurt:int //暴伤
}
