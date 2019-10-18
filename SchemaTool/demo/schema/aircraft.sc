
//空间状态
SpaceState : enum
{
    SpaceStateLocked : 0, //未开放
    SpaceStateNeedClean : 1,//未清理
    SpaceStateEmpty : 2,//可建造
    SpaceStateFull : 3, //已建造
    SpaceStateAisle : 4,//已建造走道
}

//房间类型
AirRoomType:enum
{
    AisleRoom : 0,//过道
    CentralRoom : 1,//主控室
    PowerRoom : 2,//能源室
    EntertainRoom : 3,//娱乐室
    EvailRoom : 4, //恶鬼抓捕室
    PurifyRoom : 5,//恶鬼研究所
}

//房间状态
AirRoomStatus : enum
{
    Building : 1,//建造升级
    Idle : 2, //停止工作
}

//空间表
cfg_aircraft_space:schema @dump
{
    ID: int @key @range(7100000,7199999), //空间id
    Status:SpaceState, //空间初始状态
    AdjancentID:[int], //邻接空间ID
    BuildType:[AirRoomType], //可建造建筑类型
    CleanCost:[{RoleAsset}|], //清理消耗资源
    AddFireFly:int, //增加萤火
}

//房间表
cfg_aircraft_room:schema @dump
{
    ID: int @key @range(7200000,7299999), //房间ID
    RoomType:AirRoomType, //房间类型
    Level:int, //等级
    PrevLevelID:int, //上一级ID
    NextLevelID:int, //下一级ID
    Need:[{RoleAsset}|], //升级消耗
    Recycle:[{RoleAsset}|], //降级返还
    NeedPower:int, //星能需求
    LevelUpTime:int, //升级时间
    PetNum:int, //入驻星灵数量
    Name:string, //名字
    Picture:string,//截图
    Prefab:string, //模型
}

//通用配置结构：整数范围
IntRange:schema
{
    min:int,
    max:int,
}

RoomLimit:schema
{
    CountLimit:int, //数量限制
    LevelLimit:int, //等级限制
}
//主控室
cfg_aircraft_central_room:schema @dump
{
    ID:int @key @ref(cfg_aircraft_room.ID, RoomType==1), //主控室ID
    AddFirefly:int, //增加萤火
    MoodCost:int, //心情消耗速度
    AisleLimit:{RoomLimit}, //走道限制
    PowerRoomLimit:{RoomLimit}, //能源室限制
    EntertainRoomLimit:{RoomLimit}, //娱乐室限制
    EvilRoomLimit:{RoomLimit}, //恶鬼抓捕室限制
    PurifyRoomLimit:{RoomLimit}, //研究所限制
}

//能源室
cfg_aircraft_power_room:schema @dump
{
    ID:int, @key @ref(cfg_aircraft_room.ID, RoomType==2),
    FireflyRecover:float, //萤火恢复速度（分钟）
    AddPower:int, //提供星能
    MoodCost:int, //心情消耗速度（小时）
}

//娱乐室
cfg_aircraft_entertain_room:schema @dump
{
    ID:int @key @ref(cfg_aircraft_room.ID, RoomType==3),
    MoodRecover:float, //心情恢复速度（小时）
}


//恶鬼抓捕室
cfg_aircraft_evil_room:schema @dump
{
    ID:int @key @ref(cfg_aircraft_room.ID, RoomType==4),
    RefreshEvilCount:{IntRange}, //恶鬼刷新数量
    SearchEvilStar:{IntRange}, //恶鬼搜索星级
    SearchCount:int, //搜索恶鬼数量
    CellCount:int, //监牢数量
    QuickCaptureEvilStar:int,//可快速抓捕恶鬼星级
    TraceEvilCount:int, //可追踪恶鬼数量
}

//净化室
cfg_aircraft_purify_room:schema @dump
{
    ID:int @key @ref(cfg_aircraft_room.ID, RoomType==5),
    DecrTime:int, //净化减少时间（分钟）
}

//恶鬼表
cfg_evil:schema @dump
{
    ID:int @key @range(8000000,8999999),
    Name:string,
    Body:string,
    Spine:string,
    Prefab:string,
    Star:int,
    Element:int,
    HP:int,
    Tags:string,
    CaptureUseFirefly:int,
    MoodCost:int, //抓捕心情消耗
    PurifyTime:int, //净化时间
    ShowDrops:[{RoleAsset}|], //显示奖励
    DropID:int, //掉落
    LevelID:int, //关卡
}
