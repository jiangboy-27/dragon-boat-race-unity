using UnityEngine;
using System.Collections.Generic;

// 定义祝福效果的类型
public enum BlessingEffectType
{
    WoodDamageReduction,   // 艾草之韧 - 减少浮木伤害
    InvincibilitySpeedBoost, // 雄黄之怒 - 增加无敌时速度
    DoubleScoreChance,     // 香囊之运 - 拾取粽子时几率得分翻倍
    WhirlpoolProtection,   // 沐兰之洁 - 漩涡免死
    InitialInvincibility,  // 天问之光 - 开局无敌
    PeriodicHeal           // 龙魂庇护 - 周期性回血
}

// 定义解锁或升级一次所需的消耗
[System.Serializable]
public class WeaveCost
{
    public int greenSilk;
    public int redSilk;
    public int yellowSilk;
    public int whiteSilk;
    public int blackSilk;
    public int dragonXp;
}

[CreateAssetMenu(fileName = "Blessing_New", menuName = "DragonBoat/Blessing Data")]
public class BlessingData : ScriptableObject
{
    [Header("基本信息")]
    public string blessingId; // 唯一的ID,
    public string blessingName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("养成与效果")]
    public BlessingEffectType effectType;

    // 专门用于解锁的消耗 
    public WeaveCost unlockCost;

    public List<float> levelValues; // 每一级的效果数值
    public List<WeaveCost> upgradeCosts; // 每一级的升级消耗（索引0是升到2级的消耗）

    // 获取特定等级的效果值
    public float GetValue(int level)
    {
        if (level > 0 && level <= levelValues.Count)
        {
            return levelValues[level - 1]; // level 1 对应索引 0
        }
        return 0;
    }

    public WeaveCost GetCostToNextLevel(int currentLevel)
    {
        // currentLevel 1, 想升到2级, 需要 upgradeCosts 列表的索引 0
        int costIndex = currentLevel - 1;

        if (costIndex >= 0 && costIndex < upgradeCosts.Count)
        {
            return upgradeCosts[costIndex];
        }

        // 如果索引无效（比如已满级），则返回null
        return null;
    }
}