namespace GameLogic
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public enum EntityType
    {
        None = 0,
        Player = 1,    // 玩家
        Enemy = 2,     // 敌人
        NPC = 3,       // NPC
        Projectile = 4 // 投射物
    }
    
    /// <summary>
    /// 实体状态类型
    /// </summary>
    public enum EntityStateType
    {
        None = 0,
        Idle = 1,      // 空闲
        Combat = 2,    // 战斗
        Dead = 3,      // 死亡
        Stunned = 4,   // 眩晕
        Attacking = 5, // 攻击中
        Casting = 6    // 施法中
    }
    
    /// <summary>
    /// 战斗状态类型
    /// </summary>
    public enum CombatStateType
    {
        Idle = 0,      // 非战斗状态
        Preparing = 1,  // 准备战斗
        Combat = 2,     // 战斗中
        Ending = 3      // 战斗结束
    }
    
    /// <summary>
    /// 属性类型
    /// </summary>
    public enum AttributeType
    {
        Health = 0,      // 生命值
        MaxHealth = 1,   // 最大生命值
        Attack = 2,      // 攻击力
        Defense = 3,     // 防御力
        Speed = 4,       // 速度
        Critical = 5,    // 暴击率
        CritDamage = 6,  // 暴击伤害
        Dodge = 7,       // 闪避率
        AttackSpeed = 8  // 攻击速度
    }
    
    /// <summary>
    /// 属性修饰符类型
    /// </summary>
    public enum ModifierType
    {
        Add = 0,        // 加法修饰
        Multiply = 1,    // 乘法修饰
        Override = 2,     // 覆盖修饰
        Additive = 3
    }
    
    /// <summary>
    /// VR输入类型
    /// </summary>
    public enum VRInputType
    {
        LeftGrip = 0,      // 左手抓取
        RightGrip = 1,      // 右手抓取
        LeftTrigger = 2,    // 左手扳机
        RightTrigger = 3,   // 右手扳机
        LeftPrimary = 4,    // 左手主按钮
        RightPrimary = 5,   // 右手主按钮
        LeftSecondary = 6,  // 左手次按钮
        RightSecondary = 7,  // 右手次按钮
        TriggerButton = 8,
        GripButton = 9,
        MenuButton = 10
    }
}
