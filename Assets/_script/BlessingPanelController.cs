using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BlessingPanelController : MonoBehaviour
{
    [Header("资源UI")]
    public TextMeshProUGUI greenSilkText;
    public TextMeshProUGUI redSilkText;
    public TextMeshProUGUI yellowSilkText;
    public TextMeshProUGUI whiteSilkText;
    public TextMeshProUGUI blackSilkText;

    [Header("祝福列表UI")]
    public GameObject blessingItemTemplate;
    public Transform contentParent;

    [Header("装备槽UI")]
    public Button[] equipSlots;
    public Sprite emptySlotSprite;

    // --- 私有变量 ---
    private PlayerSaveData currentData;
    private List<BlessingData> allBlessingData;
    private int currentBlessingSlotCount; //存储当前可用的祝福槽数量
    private BoatPartData tailPartData;

    void Awake()
    {
        allBlessingData = new List<BlessingData>(Resources.LoadAll<BlessingData>("BlessingData"));
        // 加载龙尾的数据
        tailPartData = Resources.Load<BoatPartData>("BoatPartData/Part_Tail");
    }

    void OnEnable()
    {
        RefreshAllUI();
    }

    // 一个总的刷新方法
    public void RefreshAllUI()
    {
        this.currentData = PlayerDataManager.GetData();
        if (this.currentData == null)
        {
            Debug.LogError("无法获取玩家数据！");
            return;
        }
        // 在刷新UI前，先计算当前可用的祝福槽数量
        CalculateCurrentSlotCount();
        UpdateSilkCountUI();
        PopulateBlessingList();
        UpdateEquipSlotsUI();
    }
    private void CalculateCurrentSlotCount()
    {
        // a. 获取龙尾等级
        int tailLevel = PlayerDataManager.GetBoatTailLevel();
        if (tailLevel <= 0) tailLevel = 1; // 安全检查

        // b. 确保龙尾数据已加载
        if (tailPartData == null || tailPartData.levels == null || tailPartData.levels.Count == 0)
        {
            Debug.LogError("龙尾数据未正确加载或配置！");
            currentBlessingSlotCount = 1; // 默认给1个槽
            return;
        }

        // c. 获取对应等级的数据
        int index = tailLevel - 1;
        if (index >= tailPartData.levels.Count) index = tailPartData.levels.Count - 1; // 防止越界
        UpgradeLevel levelData = tailPartData.levels[index];

        // d. 查找祝福槽效果
        var slotEffect = levelData.effects.Find(e => e.type == BoatEffectType.BlessingSlot);
        if (slotEffect != null)
        {
            currentBlessingSlotCount = (int)slotEffect.value;
        }
        else
        {
            // 如果当前等级没有配置槽位效果，向上查找（继承）低等级的配置
            for (int i = index - 1; i >= 0; i--)
            {
                var lowerLevelEffect = tailPartData.levels[i].effects.Find(e => e.type == BoatEffectType.BlessingSlot);
                if (lowerLevelEffect != null)
                {
                    currentBlessingSlotCount = (int)lowerLevelEffect.value;
                    return;
                }
            }
            // 如果一直找不到，给个默认值
            currentBlessingSlotCount = 1;
        }
    }
    // 更新丝线数量显示
    void UpdateSilkCountUI()
    {
        if (greenSilkText != null) greenSilkText.text = $"青丝: {currentData.greenSilk}";
        if (redSilkText != null) redSilkText.text = $"赤丝: {currentData.redSilk}";
        if (yellowSilkText != null) yellowSilkText.text = $"黄丝: {currentData.yellowSilk}";
        if (whiteSilkText != null) whiteSilkText.text = $"白丝: {currentData.whiteSilk}";
        if (blackSilkText != null) blackSilkText.text = $"黑丝: {currentData.blackSilk}";
    }

    // 动态生成祝福列表
    void PopulateBlessingList()
    {
        foreach (Transform child in contentParent)
        {
            if (child.gameObject != blessingItemTemplate) Destroy(child.gameObject);
        }

        foreach (BlessingData data in allBlessingData)
        {
            GameObject newItem = Instantiate(blessingItemTemplate, contentParent);

            Image icon = newItem.transform.Find("Icon_Image").GetComponent<Image>();
            TextMeshProUGUI nameText = newItem.transform.Find("Name_Text").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI levelText = newItem.transform.Find("Level_Text").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = newItem.transform.Find("Desc_Text").GetComponent<TextMeshProUGUI>();
            Button upgradeButton = newItem.transform.Find("Upgrade_Button").GetComponent<Button>();
            TextMeshProUGUI buttonText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();

            // 为列表项的根按钮获取引用
            Button itemButton = newItem.GetComponent<Button>();

            icon.sprite = data.icon;
            nameText.text = data.blessingName;

            // 在添加新监听前，先移除所有旧的监听，防止重复绑定
            upgradeButton.onClick.RemoveAllListeners();
            itemButton.onClick.RemoveAllListeners();

            if (currentData.ownedBlessings.ContainsKey(data.blessingId))
            {
                int currentLevel = currentData.ownedBlessings[data.blessingId];
                levelText.text = $"Lv.{currentLevel}";
                descText.text = $"{data.description}"; // 示例描述

                WeaveCost cost = data.GetCostToNextLevel(currentLevel);
                if (cost != null)
                {
                    buttonText.text = "升级";
                    upgradeButton.onClick.AddListener(() => OnUpgradeButtonClicked(data, cost));
                }
                else
                {
                    buttonText.text = "MAX";
                    upgradeButton.interactable = false;
                }
            }
            else
            {
                // 未拥有
                levelText.text = "未解锁";
                descText.text = $"{data.description}";
                buttonText.text = "解锁";

                WeaveCost cost = data.unlockCost; // 获取独立的解锁消耗

                // 检查策划是否配置了消耗
                if (cost != null)
                {
                    upgradeButton.onClick.AddListener(() => OnUnlockButtonClicked(data, cost));
                }
                else
                {
                    // 如果连解锁消耗都没配，说明数据有问题
                    buttonText.text = "配置错误";
                    upgradeButton.interactable = false;
                    Debug.LogError($"祝福 {data.blessingName} 的解锁消耗 (unlockCost) 未配置！");
                }

            }

            // 为整个列表项的按钮添加“装备”事件
            itemButton.onClick.AddListener(() => OnEquipButtonClicked(data));

            newItem.SetActive(true);
        }
    }
    void UpdateEquipSlotsUI()
    {
        if (currentData == null) return;

        // 遍历所有UI槽位，但只处理“已解锁”的槽位
        for (int i = 0; i < equipSlots.Length; i++)
        {
            Button slotButton = equipSlots[i];

            // 如果当前槽位的索引小于已解锁的槽位数量，则显示它
            if (i < currentBlessingSlotCount)
            {
                slotButton.gameObject.SetActive(true); // 激活这个槽位
                Image slotImage = slotButton.GetComponent<Image>();

                if (i < currentData.equippedBlessings.Count)
                {
                    // --- 该槽位有祝福 ---
                    string blessingId = currentData.equippedBlessings[i];
                    BlessingData data = allBlessingData.Find(b => b.blessingId == blessingId);

                    if (data != null)
                    {
                        slotImage.sprite = data.icon;
                        slotImage.color = Color.white;
                        slotButton.onClick.RemoveAllListeners();
                        slotButton.onClick.AddListener(() => OnUnequipButtonClicked(blessingId));
                    }
                }
                else
                {
                    // --- 该槽位是空的 ---
                    slotImage.sprite = emptySlotSprite;
                    slotImage.color = new Color(1, 1, 1, 0.5f);
                    slotButton.onClick.RemoveAllListeners();
                }
            }
            else
            {
                // --- 该槽位未解锁，直接隐藏它 ---
                slotButton.gameObject.SetActive(false);
            }
        }
    }
    // --- 按钮点击逻辑 ---

    void OnUnlockButtonClicked(BlessingData data, WeaveCost cost)
    {
            // 详细的调试日志
            Debug.Log($"--- 开始解锁检查: {data.blessingName} ---");
            Debug.Log($"需要 青丝: {cost.greenSilk}, 当前拥有: {PlayerDataManager.GetGreenSilk()}");
            Debug.Log($"需要 赤丝: {cost.redSilk}, 当前拥有: {PlayerDataManager.GetRedSilk()}");
            Debug.Log($"需要 黄丝: {cost.yellowSilk}, 当前拥有: {PlayerDataManager.GetYellowSilk()}");
            Debug.Log($"需要 白丝: {cost.whiteSilk}, 当前拥有: {PlayerDataManager.GetWhiteSilk()}");
            Debug.Log($"需要 黑丝: {cost.blackSilk}, 当前拥有: {PlayerDataManager.GetBlackSilk()}");

            if (!HasEnoughResources(cost))
            {
                Debug.Log("资源不足，无法解锁！检查上面的日志以了解详情。");
                return;
            }
            // 扣除资源
            PlayerDataManager.AddSilks(-cost.greenSilk, -cost.redSilk, -cost.yellowSilk, -cost.whiteSilk, -cost.blackSilk);

        PlayerDataManager.UnlockBlessing(data.blessingId);
        Debug.Log($"成功解锁: {data.blessingName}");
        RefreshAllUI();
    }

    void OnUpgradeButtonClicked(BlessingData data, WeaveCost cost)
    {
        if (!HasEnoughResources(cost))
        {
            Debug.Log("资源不足，无法升级！");
            return;
        }

        PlayerDataManager.AddSilks(-cost.greenSilk, -cost.redSilk, -cost.yellowSilk, -cost.whiteSilk, -cost.blackSilk);
        PlayerDataManager.UpgradeBlessing(data.blessingId);
        Debug.Log($"成功升级: {data.blessingName}");
        RefreshAllUI();
    }

    void OnEquipButtonClicked(BlessingData data)
    {
        if (!currentData.ownedBlessings.ContainsKey(data.blessingId))
        {
            Debug.Log("请先解锁该祝福！");
            return;
        }
        if (currentData.equippedBlessings.Contains(data.blessingId))
        {
            Debug.Log("该祝福已装备！");
            return;
        }
        if (currentData.equippedBlessings.Count >= equipSlots.Length)
        {
            Debug.Log("装备槽已满！");
            return;
        }
        //与计算出的可用槽位数比较
        if (currentData.equippedBlessings.Count >= currentBlessingSlotCount)
        {
            Debug.Log("装备槽已满！请升级龙尾以解锁更多槽位。");
            return;
        }

        PlayerDataManager.EquipBlessing(data.blessingId);
        RefreshAllUI();
    }

    void OnUnequipButtonClicked(string blessingId)
    {
        PlayerDataManager.UnequipBlessing(blessingId);
        RefreshAllUI();
    }

    //一个统一的资源检查方法
    private bool HasEnoughResources(WeaveCost cost)
    {
        if (cost == null) return false;

        return PlayerDataManager.GetGreenSilk() >= cost.greenSilk &&
               PlayerDataManager.GetRedSilk() >= cost.redSilk &&
               PlayerDataManager.GetYellowSilk() >= cost.yellowSilk &&
               PlayerDataManager.GetWhiteSilk() >= cost.whiteSilk &&
               PlayerDataManager.GetBlackSilk() >= cost.blackSilk;
    }
}