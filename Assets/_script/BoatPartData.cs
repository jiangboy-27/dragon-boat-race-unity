using UnityEngine;
using System.Collections.Generic;

public enum BoatEffectType
{
    // --- 龙头属性 ---
    LaneChangeSpeed,        // 效果1: 航道切换速度
    InvincibilityDuration,  // 效果2: 黄金龙首无敌时间

    // --- 船身属性 ---
    MaxHealth,              // 效果1: 最大生命值
    WoodDamageReduction,    // 效果2: 浮木伤害减免 (百分比)

    // --- 龙尾属性 ---
    BlessingSlot,           // 效果1: 祝福装备槽数量
    SilkFindChance          // 效果2: 稀有丝线出现概率 (百分比)
}
// 定义一个具体的效果
[System.Serializable]
public class BoatUpgradeEffect
{
    public BoatEffectType type;
    public float value;
}
// 定义升级所需的消耗
[System.Serializable]
public class UpgradeCost
{
    public int normalWood;
    public int rareWood;
    public int legendaryWood;
}

// 定义每个等级的效果和消耗
[System.Serializable]
public class UpgradeLevel
{
    // 这个等级提供的所有效果
    public List<BoatUpgradeEffect> effects;
    // 升到下一级所需的消耗
    public UpgradeCost costToNextLevel;
}

[CreateAssetMenu(fileName = "New Boat Part", menuName = "DragonBoat/Boat Part Data")]
public class BoatPartData : ScriptableObject
{
    public string partId; // "head", "body", "tail"
    public List<UpgradeLevel> levels;
}