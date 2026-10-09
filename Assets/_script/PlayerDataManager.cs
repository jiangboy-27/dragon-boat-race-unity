using UnityEngine;
using System.IO;

public static class PlayerDataManager
{
    private static PlayerSaveData _playerData;
    private static string _savePath;

    static PlayerDataManager()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        LoadData();
    }
    // --- 新增：恢复 GetData() 方法 ---
    /// <summary>
    /// 获取完整的玩家存档数据对象。
    /// 主要用于核心系统进行批量数据读取和初始化。
    /// UI等简单脚本应优先使用下方的具体Get/Set方法。
    /// </summary>
    /// <returns>当前的PlayerSaveData实例</returns>
    public static PlayerSaveData GetData()
    {
        return _playerData;
    }

    private static void LoadData()
    {
        if (File.Exists(_savePath))
        {
            try 
            {
                string json = File.ReadAllText(_savePath);
                _playerData = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log("玩家存档已加载！");
            }
            catch (System.Exception e)
            {
                // 如果旧存档结构与新的PlayerSaveData类不匹配，JsonUtility会抛出异常
                Debug.LogError($"加载存档失败，可能是存档版本不兼容: {e.Message}");
                Debug.Log("正在创建一份新的干净存档...");
                CreateNewSaveData();
            }
        }
        else
        {
            Debug.Log("未找到存档，已创建新存档。");
            CreateNewSaveData();
        }
    }
    // 创建新存档
    private static void CreateNewSaveData()
    {
        _playerData = new PlayerSaveData();
        SaveData();
    }
    public static void SaveData()
    {
        string json = JsonUtility.ToJson(_playerData, true);
        File.WriteAllText(_savePath, json);
        Debug.Log("玩家存档已保存。");
    }

    // (调试用)
    public static void ResetData()
    {
        _playerData = new PlayerSaveData();
        SaveData();
    }

    // --- 公共接口 ---

    #region 资源管理
    public static int GetNormalWood() { return _playerData.normalWood; }
    public static int GetRareWood() { return _playerData.rareWood; }
    public static int GetLegendaryWood() { return _playerData.legendaryWood; }
    public static int GetDragonXp() { return _playerData.dragonXp; }

    public static void AddCurrency(int normal, int rare, int legendary, int xp)
    {
        _playerData.normalWood += normal;
        _playerData.rareWood += rare;
        _playerData.legendaryWood += legendary;
        _playerData.dragonXp += xp;
        SaveData(); 
    }
    #endregion

    #region 龙舟部件等级管理
    public static int GetBoatHeadLevel() { return _playerData.boatHeadLevel; }
    public static void SetBoatHeadLevel(int level)
    {
        _playerData.boatHeadLevel = level;
        SaveData();
    }

    public static int GetBoatBodyLevel() { return _playerData.boatBodyLevel; }
    public static void SetBoatBodyLevel(int level)
    {
        _playerData.boatBodyLevel = level;
        SaveData();
    }

    public static int GetBoatTailLevel() { return _playerData.boatTailLevel; }
    public static void SetBoatTailLevel(int level)
    {
        _playerData.boatTailLevel = level;
        SaveData();
    }
    #endregion

    #region 五彩丝线管理
    public static void AddSilks(int green, int red, int yellow, int white, int black)
    {
        _playerData.greenSilk += green;
        _playerData.redSilk += red;
        _playerData.yellowSilk += yellow;
        _playerData.whiteSilk += white;
        _playerData.blackSilk += black;
        SaveData();
    }
    public static int GetGreenSilk() { return _playerData.greenSilk; }
    public static int GetRedSilk() { return _playerData.redSilk; }
    public static int GetYellowSilk() { return _playerData.yellowSilk; }
    public static int GetWhiteSilk() { return _playerData.whiteSilk; }
    public static int GetBlackSilk() { return _playerData.blackSilk; }

    #endregion

    #region 祝福管理
    public static void UnlockBlessing(string blessingId)
    {
        if (!_playerData.ownedBlessings.ContainsKey(blessingId))
        {
            _playerData.ownedBlessings.Add(blessingId, 1);
            SaveData();
        }
    }

    public static void UpgradeBlessing(string blessingId)
    {
        if (_playerData.ownedBlessings.ContainsKey(blessingId))
        {
            _playerData.ownedBlessings[blessingId]++;
            SaveData();
        }
    }

    public static void EquipBlessing(string blessingId)
    {
        if (!_playerData.equippedBlessings.Contains(blessingId))
        {
            _playerData.equippedBlessings.Add(blessingId);
            SaveData();
        }
    }
    public static void UnequipBlessing(string blessingId)
    {
        if (_playerData.equippedBlessings.Contains(blessingId))
        {
            _playerData.equippedBlessings.Remove(blessingId);
            SaveData();
        }
    }
    #endregion
}
