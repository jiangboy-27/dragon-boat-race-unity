using System.Collections.Generic;
using UnityEngine; // 需要引用UnityEngine来实现ISerializationCallbackReceiver

[System.Serializable]
public class PlayerSaveData : ISerializationCallbackReceiver
{
    // 龙舟各部件的等级 
    public int boatHeadLevel;
    public int boatBodyLevel;
    public int boatTailLevel;

    // 拥有的货币/资源
    public int normalWood;
    public int rareWood;
    public int legendaryWood;
    public int dragonXp;
    // 五彩丝线数量
    public int greenSilk;
    public int redSilk;
    public int yellowSilk;
    public int whiteSilk;
    public int blackSilk;

    // 祝福系统存档 
    // 记录玩家拥有的每个祝福及其等级
    [System.NonSerialized]
    public Dictionary<string, int> ownedBlessings;

    // 用于序列化的List
    [SerializeField] private List<string> blessingKeys = new List<string>();
    [SerializeField] private List<int> blessingValues = new List<int>();

    // 记录玩家当前装备的祝福ID
    public List<string> equippedBlessings;
    // 构造函数，用于创建一份新的、初始化的存档
    public PlayerSaveData()
    {
        boatHeadLevel = 1;
        boatBodyLevel = 1;
        boatTailLevel = 1;
        normalWood = 1000; 
        rareWood = 100;
        legendaryWood = 10;
        boatHeadLevel = 1; boatBodyLevel = 1; boatTailLevel = 1;
        normalWood = 1000; rareWood = 100; legendaryWood = 10;
        dragonXp = 0;
        // 初始化丝线和祝福
        greenSilk = 50; redSilk = 50; yellowSilk = 50; whiteSilk = 50; blackSilk = 50;

        ownedBlessings = new Dictionary<string, int>();
        equippedBlessings = new List<string>();
    }
    public void OnBeforeSerialize()
    {
        blessingKeys.Clear();
        blessingValues.Clear();
        if (ownedBlessings == null) return;
        foreach (var pair in ownedBlessings)
        {
            blessingKeys.Add(pair.Key);
            blessingValues.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        ownedBlessings = new Dictionary<string, int>();
        for (int i = 0; i < blessingKeys.Count && i < blessingValues.Count; i++)
        {
            ownedBlessings.Add(blessingKeys[i], blessingValues[i]);
        }
    }
}