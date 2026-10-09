using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DragonBoatPanelController : MonoBehaviour
{
    // --- UI 引用 (保持不变) ---
    [Header("资源UI")]
    public TextMeshProUGUI normalWoodText;
    public TextMeshProUGUI rareWoodText;
    public TextMeshProUGUI legendaryWoodText;
    public TextMeshProUGUI dragonXpText;

    [Header("龙首 UI")]
    public TextMeshProUGUI text_Info_Dragonhead;
    public Button button_Upgrade_Dragonhead;
    public TextMeshProUGUI text_Cost_Dragonhead;

    [Header("船身 UI")]
    public TextMeshProUGUI text_Info_Body;
    public Button button_Upgrade_Body;
    public TextMeshProUGUI text_Cost_Body;

    [Header("龙尾 UI")]
    public TextMeshProUGUI text_Info_Tail;
    public Button button_Upgrade_Tail;
    public TextMeshProUGUI text_Cost_Tail;


    // --- 私有变量 ---
    private int dragonheadLevel;
    private int bodyLevel;
    private int tailLevel;

    // 用于缓存部件数据的字典 
    private Dictionary<string, BoatPartData> boatPartDataCache;

    void Awake()
    {
        // 在Awake时加载并缓存所有部件数据
        boatPartDataCache = new Dictionary<string, BoatPartData>();
        var allParts = Resources.LoadAll<BoatPartData>("BoatPartData");
        foreach (var part in allParts)
        {
            boatPartDataCache[part.partId] = part;
        }
    }

    void OnEnable()
    {
        RefreshAllUI();
    }

    void Start()
    {
        if (button_Upgrade_Dragonhead != null) button_Upgrade_Dragonhead.onClick.AddListener(OnUpgradeDragonheadClicked);
        if (button_Upgrade_Body != null) button_Upgrade_Body.onClick.AddListener(OnUpgradeBodyClicked);
        if (button_Upgrade_Tail != null) button_Upgrade_Tail.onClick.AddListener(OnUpgradeTailClicked);
    }

    private void RefreshAllUI()
    {
        UpdatePartLevelUI();
        UpdateResourceUI();
        CheckButtonInteractable();
    }

    //更新UI显示，从BoatPartData读取消耗
    private void UpdatePartLevelUI()
    {
        dragonheadLevel = PlayerDataManager.GetBoatHeadLevel();
        bodyLevel = PlayerDataManager.GetBoatBodyLevel();
        tailLevel = PlayerDataManager.GetBoatTailLevel();

        // 更新龙首UI
        if (text_Info_Dragonhead != null) text_Info_Dragonhead.text = $"龙首 Lv.{dragonheadLevel}";
        if (text_Cost_Dragonhead != null) text_Cost_Dragonhead.text = GetCostText("head", dragonheadLevel);

        // 更新船身UI
        if (text_Info_Body != null) text_Info_Body.text = $"船身 Lv.{bodyLevel}";
        if (text_Cost_Body != null) text_Cost_Body.text = GetCostText("body", bodyLevel);

        // 更新龙尾UI
        if (text_Info_Tail != null) text_Info_Tail.text = $"龙尾 Lv.{tailLevel}";
        if (text_Cost_Tail != null) text_Cost_Tail.text = GetCostText("tail", tailLevel);
    }
    // 一个辅助方法，用于生成消耗文本
    private string GetCostText(string partId, int currentLevel)
    {
        if (!boatPartDataCache.ContainsKey(partId)) return "数据错误";

        BoatPartData partData = boatPartDataCache[partId];
        // 检查是否已满级
        if (currentLevel >= partData.levels.Count)
        {
            return "已满级";
        }

        // 获取升到下一级的消耗
        UpgradeCost cost = partData.levels[currentLevel - 1].costToNextLevel;
        if (cost == null) return "数据错误";

        // 动态生成文本，只显示不为0的消耗
        string costString = "升级 (";
        if (cost.normalWood > 0) costString += $"普木:{cost.normalWood} ";
        if (cost.rareWood > 0) costString += $"稀木:{cost.rareWood} ";
        if (cost.legendaryWood > 0) costString += $"传木:{cost.legendaryWood}";
        costString += ")";

        return costString;
    }

    // 更新资源UI
    private void UpdateResourceUI()
    {
        if (normalWoodText != null) normalWoodText.text = $"普通木料: {PlayerDataManager.GetNormalWood()}";
        if (rareWoodText != null) rareWoodText.text = $"稀有木料: {PlayerDataManager.GetRareWood()}";
        if (legendaryWoodText != null) legendaryWoodText.text = $"传说木料: {PlayerDataManager.GetLegendaryWood()}";
        if (dragonXpText != null) dragonXpText.text = $"龙舟经验: {PlayerDataManager.GetDragonXp()}";
    }

    // 检查按钮是否可点击 
    private void CheckButtonInteractable()
    {
        if (button_Upgrade_Dragonhead != null) button_Upgrade_Dragonhead.interactable = CanUpgradePart("head", dragonheadLevel);
        if (button_Upgrade_Body != null) button_Upgrade_Body.interactable = CanUpgradePart("body", bodyLevel);
        if (button_Upgrade_Tail != null) button_Upgrade_Tail.interactable = CanUpgradePart("tail", tailLevel);
    }

    //一个统一的检查能否升级的辅助方法
    private bool CanUpgradePart(string partId, int currentLevel)
    {
        if (!boatPartDataCache.ContainsKey(partId)) return false;

        BoatPartData partData = boatPartDataCache[partId];
        // 如果已满级，不能升级
        if (currentLevel >= partData.levels.Count) return false;

        // 获取消耗
        UpgradeCost cost = partData.levels[currentLevel - 1].costToNextLevel;
        if (cost == null) return false;

        // 检查所有资源是否足够
        return PlayerDataManager.GetNormalWood() >= cost.normalWood &&
               PlayerDataManager.GetRareWood() >= cost.rareWood &&
               PlayerDataManager.GetLegendaryWood() >= cost.legendaryWood;
    }

    #region 按钮点击事件
    private void OnUpgradeDragonheadClicked()
    {
        ProcessUpgrade("head", dragonheadLevel);
    }

    private void OnUpgradeBodyClicked()
    {
        ProcessUpgrade("body", bodyLevel);
    }

    private void OnUpgradeTailClicked()
    {
        ProcessUpgrade("tail", tailLevel);
    }

    //通用的升级处理方法
    private void ProcessUpgrade(string partId, int currentLevel)
    {
        // 1. 再次确认能否升级
        if (!CanUpgradePart(partId, currentLevel))
        {
            Debug.Log($"资源不足或已满级！无法升级 {partId}。");
            return;
        }

        // 2. 获取消耗并扣除资源
        UpgradeCost cost = boatPartDataCache[partId].levels[currentLevel - 1].costToNextLevel;
        PlayerDataManager.AddCurrency(-cost.normalWood, -cost.rareWood, -cost.legendaryWood, 0);

        // 3. 提升等级并保存
        int newLevel = currentLevel + 1;
        switch (partId)
        {
            case "head": PlayerDataManager.SetBoatHeadLevel(newLevel); break;
            case "body": PlayerDataManager.SetBoatBodyLevel(newLevel); break;
            case "tail": PlayerDataManager.SetBoatTailLevel(newLevel); break;
        }

        Debug.Log($"{partId} 升级成功！");

        // 4. 升级后刷新所有UI
        RefreshAllUI();
    }
    #endregion

    void OnDestroy()
    {
        if (button_Upgrade_Dragonhead != null) button_Upgrade_Dragonhead.onClick.RemoveAllListeners();
        if (button_Upgrade_Body != null) button_Upgrade_Body.onClick.RemoveAllListeners();
        if (button_Upgrade_Tail != null) button_Upgrade_Tail.onClick.RemoveAllListeners();
    }
}