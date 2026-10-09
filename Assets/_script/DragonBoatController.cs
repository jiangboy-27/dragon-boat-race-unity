using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// 定义每个速度里程碑 
[System.Serializable]
public class SpeedMilestone
{
    [Tooltip("当航行距离达到这个值时，触发此里程碑")]
    public float triggerDistance;

    [Tooltip("触发时，基础速度瞬间增加这个值")]
    public float speedBoost;

    [Tooltip("触发后，新的目标速度（软上限）")]
    public float newTargetSpeed;

    [Tooltip("触发后，新的速度增长系数（可以保持不变）")]
    public float newGrowthFactor;
}

public class DragonBoatController : MonoBehaviour
{
    [Header("航道设置")]
    [Tooltip("所有航道的Z轴坐标")]
    public float[] lanePositions = { -2f, 0f, 2f };
    [Tooltip("切换航道所需的时间")]
    public float laneChangeDuration = 0.2f;

    [Header("速度设置")]
    [Tooltip("初始前进速度")]
    public float initialForwardSpeed = 10f;
    [Tooltip("速度里程碑配置列表，请按triggerDistance从小到大排序")]
    public List<SpeedMilestone> speedMilestones;

    [Header("龙舟状态")]
    [Tooltip("初始/最大生命值")]
    public int maxHealth = 100;

    [Header("UI 界面引用")]
    public Slider healthSlider;
    public GameObject invincibleUIContainer; // 整个无敌UI的容器
    public TextMeshProUGUI invincibleTimerText;

    [Header("实时信息UI引用")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText_Live;
    public TextMeshProUGUI timeText_Live;
    public TextMeshProUGUI speedText_Live;

    // 存储从龙舟升级中获得的最终属性
    private float finalLaneChangeSpeedBonus = 0f;  // 龙头
    private float bonusInvincibilityTime_Boat = 0f;  // 龙头
    private int bonusMaxHealth = 0;              // 船身
    private float woodDamageReduction_Boat = 0f;   // 船身 (0-1的小数)
    private int finalBlessingSlots = 1;          // 龙尾
    private float silkFindChanceBonus = 0f;      // 龙尾 (0-1的小数)

    // --- 存储已激活的祝福效果 ---
    private float blessing_woodDamageReduction = 0f;  // 艾草之韧
    private float blessing_invincibilitySpeedBoost = 0f; // 雄黄之怒 
    private float blessing_doubleScoreChance = 0f;   // 香囊之运 
    private bool blessing_whirlpoolProtection = false; // 沐兰之洁 
    private float blessing_initialInvincibility = 0f;
    private float blessing_periodicHealValue = 0f; // (为龙魂庇护预留) 
    private float blessing_periodicHealInterval = 500f; // (为龙魂庇护预留)

    // --- 私有变量 ---
    private int currentScore = 0;
    private int currentHealth;
    private int currentLaneIndex;
    private bool isChangingLane = false;
    private bool isInvincible = false;
    private float startTime;
    private float distanceTraveled;
    private Coroutine invincibilityCoroutine;
    private float baseForwardSpeed; // 基础速度
    private int currentMilestoneIndex = 0; // 当前所处的里程碑阶段索引
    private float currentTargetSpeed;     // 当前阶段的目标速度
    private float currentGrowthFactor;    // 当前阶段的增长系数

    public float CurrentForwardSpeed
    {
        get
        {
            if (isInvincible)
            {
                // 如果无敌，速度 = 基础速度 *  雄黄之怒祝福倍率
                float totalBoost = 1.25f+blessing_invincibilitySpeedBoost;
                return baseForwardSpeed * totalBoost;
            }
            else
            {
                // 如果不无敌，就返回基础速度
                return baseForwardSpeed;
            }
        }
    }
    // --- 游戏状态控制 ---
    private bool isGameActive = true;


    void Awake()
    {
        // 在游戏开始前，加载并计算所有养成属性
        LoadAndApplyUpgrades();
    }
    private void LoadAndApplyUpgrades()
    {
        PlayerSaveData data = PlayerDataManager.GetData();
        // --- 1. 加载并应用龙舟部件升级 ---
        var allPartData = new Dictionary<string, BoatPartData>
        {
            { "head", Resources.Load<BoatPartData>("BoatPartData/Part_Head") },
            { "body", Resources.Load<BoatPartData>("BoatPartData/Part_Body") },
            { "tail", Resources.Load<BoatPartData>("BoatPartData/Part_Tail") }
        };

        ApplyPartUpgrades(allPartData["head"], data.boatHeadLevel);
        ApplyPartUpgrades(allPartData["body"], data.boatBodyLevel);
        ApplyPartUpgrades(allPartData["tail"], data.boatTailLevel);


        // 1. 重置所有祝福加成属性
        blessing_woodDamageReduction = 0f;
        blessing_invincibilitySpeedBoost = 0f; 
        blessing_doubleScoreChance = 0f;   
        blessing_whirlpoolProtection = false; 
        blessing_initialInvincibility = 0f;
        blessing_periodicHealValue = 0f;

        // 2. 遍历已装备的祝福
        foreach (string blessingId in data.equippedBlessings)
        {
            BlessingData blessingData = Resources.Load<BlessingData>($"BlessingData/Blessing_{blessingId.Replace("_", "")}");
            if (blessingData == null)
            {
                Debug.LogWarning($"找不到祝福数据，请检查文件命名: Blessing_{blessingId}");
                continue;
            }

            int level = data.ownedBlessings[blessingId];
            float value = blessingData.GetValue(level);

            // 5. 根据祝福类型，应用效果
            switch (blessingData.effectType)
            {
                case BlessingEffectType.WoodDamageReduction: // 艾草之韧
                    blessing_woodDamageReduction += value / 100f; // 转换为0-1的小数
                    break;

                case BlessingEffectType.InvincibilitySpeedBoost:
                    blessing_invincibilitySpeedBoost += value / 100f; // 转为小数
                    break;

                case BlessingEffectType.DoubleScoreChance:
                    blessing_doubleScoreChance += value; // 直接用百分比数值
                    break;

                case BlessingEffectType.WhirlpoolProtection:
                    // 这是一个开关型技能，只要装备了，就设为true
                    if (value > 0) blessing_whirlpoolProtection = true;
                    break;

                case BlessingEffectType.InitialInvincibility:
                    blessing_initialInvincibility += value;
                    break;

                case BlessingEffectType.PeriodicHeal:
                    blessing_periodicHealValue += value;
                    break;
            }
        }
        Debug.Log("龙舟部件升级加载完毕！");
    }
    private void ApplyPartUpgrades(BoatPartData partData, int level)
    {
        // --- 安全检查  ---
        // 1. 检查 level 是否在有效范围内 (大于0)
        if (level <= 0)
        {
            Debug.LogError($"加载了无效的等级: {level}，对于部件: {partData.name}。将使用1级作为默认值。");
            level = 1; // 强制修正为1级
        }

        // 2. 检查 partData.levels 列表是否为空
        if (partData.levels == null || partData.levels.Count == 0)
        {
            Debug.LogError($"部件 {partData.name} 的 'levels' 列表为空！请在对应的ScriptableObject中配置等级数据。");
            return; // 直接返回，不执行后续逻辑，防止崩溃
        }

        // 3. 修正索引，防止超出最大范围
        int index = level - 1;
        if (index >= partData.levels.Count)
        {
            // 如果存档等级超过了配置的最大等级，则使用配置的最高级
            Debug.LogWarning($"存档等级 {level} 超出了部件 {partData.name} 的最大配置等级。将使用最高级 {partData.levels.Count}。");
            index = partData.levels.Count - 1; // 强制修正为最大有效索引
        }

        UpgradeLevel currentLevelData = partData.levels[index];

        foreach (var effect in currentLevelData.effects)
        {
            switch (effect.type)
            {
                // --- 龙头 ---
                case BoatEffectType.LaneChangeSpeed:
                    finalLaneChangeSpeedBonus = effect.value; 
                    break;
                case BoatEffectType.InvincibilityDuration:
                    bonusInvincibilityTime_Boat = effect.value;
                    break;

                // --- 船身 ---
                case BoatEffectType.MaxHealth:
                    bonusMaxHealth = (int)effect.value;
                    break;
                case BoatEffectType.WoodDamageReduction:
                    woodDamageReduction_Boat = effect.value / 100f;
                    break;

                // --- 龙尾 ---
                case BoatEffectType.BlessingSlot:
                    finalBlessingSlots = (int)effect.value;
                    break;
                case BoatEffectType.SilkFindChance:
                    silkFindChanceBonus = effect.value / 100f;
                    break;
            }
        }
    }
    void Start()
    {
        // 在游戏场景开始时，切换到游戏BGM
        AudioManager.Instance.PlayMusic(AudioManager.Instance.gameplayBGM);
        // 初始化第一个里程碑阶段 
        if (speedMilestones != null && speedMilestones.Count > 0)
        {
            // 游戏开始时，使用第一个里程碑（索引为0）的参数
            currentTargetSpeed = speedMilestones[0].newTargetSpeed;
            currentGrowthFactor = speedMilestones[0].newGrowthFactor;
        }
        else
        {
            // 如果没有配置任何里程碑，提供一个默认值以防崩溃
            Debug.LogWarning("没有配置速度里程碑！将使用默认值。");
            currentTargetSpeed = 30f; // 默认目标速度
            currentGrowthFactor = 0.01f; // 默认增长系数
        }

        // 游戏开始时的初始化
        currentHealth = maxHealth + bonusMaxHealth; // 基础生命值 + 加成
        // 初始切换速度 * (1 - 速度加成百分比)
        laneChangeDuration = laneChangeDuration * (1 - finalLaneChangeSpeedBonus); 
        baseForwardSpeed = initialForwardSpeed; // 初始化基础速度
        // 默认出生在中间航道
        currentLaneIndex = lanePositions.Length / 2;
        transform.position = new Vector3(0, 0, lanePositions[currentLaneIndex]);

        // --- UI 初始化 ---
        UpdateHealthUI();
        UpdateLiveUI();
        invincibleUIContainer.SetActive(false); // 确保无敌UI在开始时是隐藏的

        startTime = Time.time;
        distanceTraveled = 0f;

        Debug.Log("游戏开始！生命值: " + currentHealth);
        if (blessing_initialInvincibility > 0)
        {
            StartCoroutine(ActivateInvincibility(blessing_initialInvincibility));
        }
    }

    void Update()
    {
        // 检查游戏是否激活
        if (!isGameActive)
        {
            return; // 如果游戏未激活，直接跳出，不执行任何移动和输入逻辑
        }
        // --- 实时更新距离 ---
        distanceTraveled = transform.position.x;
        // 检查是否达到下一个里程碑 
        CheckForMilestone();
        HandleInput();
        MoveForward();
        // 更新UI
        UpdateLiveUI();
    }
    // 用于检查和触发里程碑 
    private void CheckForMilestone()
    {
        // 1. 检查是否还有未触发的里程碑
        if (speedMilestones == null || currentMilestoneIndex >= speedMilestones.Count)
        {
            // 所有里程碑都已触发，直接返回
            return;
        }

        // 2. 获取下一个里程碑的数据
        SpeedMilestone nextMilestone = speedMilestones[currentMilestoneIndex];

        // 3. 判断当前距离是否已经超过了下一个里程碑的触发距离
        if (distanceTraveled >= nextMilestone.triggerDistance)
        {
            Debug.Log($"<color=yellow>--- 里程碑达成: {nextMilestone.triggerDistance}米！水流加速！ ---</color>");

            // a. 瞬间提升基础速度
            baseForwardSpeed += nextMilestone.speedBoost;
            Debug.Log($"基础速度提升 {nextMilestone.speedBoost}，当前为: {baseForwardSpeed}");

            // b. 更新当前阶段的目标速度和增长系数
            currentTargetSpeed = nextMilestone.newTargetSpeed;
            currentGrowthFactor = nextMilestone.newGrowthFactor;
            Debug.Log($"新的目标速度: {currentTargetSpeed}, 新的增长系数: {currentGrowthFactor}");

            // c. 将索引指向下一个里程碑，防止重复触发
            currentMilestoneIndex++;

        }
    }
    // 核心逻辑：处理玩家输入
    private void HandleInput()
    {
        // 正在切换航道时，不允许再次输入
        if (isChangingLane) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            ChangeLane(1); // 移动到更“上”的航道，索引增加
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            ChangeLane(-1);  // 移动到更“下”的航道，索引减小
        }
    }
    // 改变航道
    private void ChangeLane(int direction)
    {
        int targetLane = currentLaneIndex + direction;

        // 边界检查，防止移出航道
        if (targetLane < 0 || targetLane >= lanePositions.Length)
        {
            return;
        }

        currentLaneIndex = targetLane;
        StartCoroutine(SmoothLaneChange(lanePositions[currentLaneIndex]));
    }

    // 平滑地移动到目标航道
    private IEnumerator SmoothLaneChange(float targetZ) 
    {
        isChangingLane = true;
        float elapsedTime = 0f;
        float startZ = transform.position.z; // 获取 Z 轴的起始位置

        while (elapsedTime < laneChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            // 使用 Z 轴进行插值
            float newZ = Mathf.Lerp(startZ, targetZ, elapsedTime / laneChangeDuration);
            // 更新位置，保持 X 和 Y 不变
            transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
            yield return null;
        }

        // 确保最终位置精确
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
        isChangingLane = false;
    }
    // 核心逻辑：龙舟前进
    private void MoveForward()
    {
        transform.Translate(Vector3.right * CurrentForwardSpeed * Time.deltaTime, Space.World);

        // 使用当前里程碑阶段的参数进行平滑增长
        if (baseForwardSpeed < currentTargetSpeed)
        {
            float speedDifference = currentTargetSpeed - baseForwardSpeed;
            float growthAmount = speedDifference * currentGrowthFactor * Time.deltaTime;
            baseForwardSpeed += growthAmount;
        }
    }
    // 核心逻辑：处理碰撞/触发事件
    private void OnTriggerEnter(Collider other)
    {
        // --- 障碍物 ---
        if (other.CompareTag("Obstacle_Wood"))
        {
            HandleCollision_Obstacle("浮木", 10);
            AudioManager.Instance.PlayHitWoodSound(); // 播放撞木头音效
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Obstacle_Reef"))
        {
            HandleCollision_Obstacle("礁石", 30);
            AudioManager.Instance.PlayHitReefSound(); // 播放撞石头音效
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Silk") || other.CompareTag("Zongzi") || other.CompareTag("Zongzi_Heal") || other.CompareTag("Zongzi_Slowdown"))
        {
            // 将所有拾取类道具的音效统一处理
            AudioManager.Instance.PlayPickupSound();

            if (other.CompareTag("Silk"))
            {
                SilkItem silk = other.GetComponent<SilkItem>();
                if (silk != null)
                {
                    // 1. 根据颜色增加存档中的丝线数量
                    switch (silk.color)
                    {
                        case SilkColor.Green: PlayerDataManager.AddSilks(1, 0, 0, 0, 0); break;
                        case SilkColor.Red: PlayerDataManager.AddSilks(0, 1, 0, 0, 0); break;
                        case SilkColor.Yellow: PlayerDataManager.AddSilks(0, 0, 1, 0, 0); break;
                        case SilkColor.White: PlayerDataManager.AddSilks(0, 0, 0, 1, 0); break;
                        case SilkColor.Black: PlayerDataManager.AddSilks(0, 0, 0, 0, 1); break;
                    }

                    // 2. 给予少量得分和瞬时加速作为即时反馈
                    AddScore(20);
                    baseForwardSpeed += 0.5f; // 临时增加一点基础速度
                    Debug.Log($"拾取了 {silk.color} 丝线!");
                }
                other.gameObject.SetActive(false); // 回收丝线
            }
            // 处理回血粽子
            else if (other.CompareTag("Zongzi_Heal"))
            {
                int healAmount = 5; // 每次恢复5点生命值
                TakeDamage(-healAmount); // 传入负数即为治疗
                AddScore(50); 
                Debug.Log($"<color=lime>吃到红枣粽！恢复了 {healAmount} 点生命值！</color>");
                other.gameObject.SetActive(false);
            }
            // 处理减速粽子 
            else if (other.CompareTag("Zongzi_Slowdown"))
            {
                float speedReduction = 0.9f;
                baseForwardSpeed *= speedReduction;

                // 防止玩家通过连续吃减速粽子让速度降为0甚至负数
                if (baseForwardSpeed < initialForwardSpeed) // 不允许速度低于初始速度
                {
                    baseForwardSpeed = initialForwardSpeed;
                }

                AddScore(50);
                Debug.Log($"<color=cyan>吃到碱水粽！速度减缓！</color>");
                other.gameObject.SetActive(false);
            }
            else if (other.CompareTag("Zongzi"))
            {
                Debug.Log("拾取了 [粽子]，得分+100");

                // 香囊之运技能判定
                int scoreToAdd = 100; // 基础得分
                if (Random.Range(0f, 100f) < blessing_doubleScoreChance)
                {
                    // 技能触发！
                    scoreToAdd *= 2; // 得分翻倍
                    Debug.Log("<color=yellow>香囊之运触发！得分翻倍！</color>");
                }
                AddScore(scoreToAdd); // 添加最终计算出的分数
                other.gameObject.SetActive(false);
            }
        }

        // --- 强化/特殊事件 ---
        else if (other.CompareTag("Powerup_Invincible"))
        {
            Debug.Log("拾取了 [黄金龙首]，进入无敌冲刺！");
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
            }
            invincibilityCoroutine = StartCoroutine(ActivateInvincibility(5f)); // 基础时间5s
            other.gameObject.SetActive(false);
        }
        
        else if (other.CompareTag("Event_Whirlpool"))
        {
            if (isInvincible) // 如果无敌，直接忽略漩涡
            {
                Debug.Log("<color=yellow>无敌状态！免疫了漩涡！</color>");
                return; // 直接跳出方法，不执行后续逻辑
            }
                if (blessing_whirlpoolProtection) // 检查是否解锁了漩涡保护
                {
                    int damage = (int)((100 + bonusMaxHealth) * 0.5f); // 扣除50%最大生命
                    TakeDamage(damage);
                    Debug.Log("漩涡保护触发！扣除50%最大生命值！");
                }
                else
                {
                    Debug.Log("被漩涡吞噬，游戏结束！");
                    currentHealth = 0;
                    GameOver();
                }
            return;
            // 漩涡不销毁，会持续存在一段时间
        }
    }

    // 得分逻辑
    private void AddScore(int amount)
    {
        currentScore += amount;
        UpdateLiveUI();
    }
    // 统一处理障碍物碰撞
    private void HandleCollision_Obstacle(string name, int damage)
    {
        if (isInvincible) { /* ... */ return; }

        float finalDamage = damage;
        // 如果是浮木，应用船身减伤
        if (name == "浮木")
        {
            // 应用船身减伤 和 祝福减伤
            finalDamage *= (1f - woodDamageReduction_Boat);
            finalDamage *= (1f - blessing_woodDamageReduction);
        }

        Debug.Log($"撞到了 [{name}]，受到 {(int)finalDamage} 点伤害!");
        TakeDamage((int)finalDamage);
    }
    // 统一处理受伤和治疗
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // 更新UI
        UpdateHealthUI();
        Debug.Log("当前生命值: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            GameOver();
        }
    }
    // 激活无敌状态的协程
    private IEnumerator ActivateInvincibility(float duration)
    {
        isInvincible = true;
        invincibleUIContainer.SetActive(true);

        float totalDuration = duration + bonusInvincibilityTime_Boat ;
        float timer = totalDuration;

        while (timer > 0)
        {
            invincibleTimerText.text = timer.ToString("F1");
            timer -= Time.deltaTime;
            yield return null;
        }

        invincibleTimerText.text = "0.0";

        isInvincible = false;
        invincibleUIContainer.SetActive(false);
        Debug.Log("无敌效果结束。");

        invincibilityCoroutine = null;
    }

    // UI更新方法
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            // Slider的值是0-1的小数，所以需要用当前生命值除以最大生命值
            healthSlider.value = (float)currentHealth / (maxHealth + bonusMaxHealth);
        }
    }
    // 一个统一的实时UI更新方法
    private void UpdateLiveUI()
    {
        // 更新得分
        if (scoreText != null)
        {        
            scoreText.text = "得分: " + GetCurrentScoreForUI();
        }

        // 更新实时里程
        if (distanceText_Live != null)
        {
            distanceText_Live.text = $"里程: {distanceTraveled:F0} 米";
        }

        // 更新实时时长
        if (timeText_Live != null)
        {
            timeText_Live.text = $"时长: {GetSurvivalTime():F1} 秒";
        }

        // 更新实时速度
        if (speedText_Live != null)
        {
            speedText_Live.text = $"速度: {CurrentForwardSpeed:F1} m/s";
        }
    }

    #region 公共Getter方法 (给GameManager调用)

    // 返回当前游戏是否在进行中
    public bool IsGameActive()
    {
        return isGameActive;
    }

    // 返回用于UI显示的完整分数
    public int GetCurrentScoreForUI()
    {
        if (!isGameActive && Time.timeScale == 0f) // 如果游戏已结束
        {

        }

        int timeScore = (int)((Time.time - startTime) * 10);
        int baseDistanceScore = (int)distanceTraveled;
        return currentScore + timeScore + baseDistanceScore;
    }

    // 返回已航行的距离
    public float GetDistanceTraveled()
    {
        return distanceTraveled;
    }

    // 返回已坚持的时间
    public float GetSurvivalTime()
    {
        if (!isGameActive)
        {
        }
        return Time.time - startTime;
    }
    // 一个被GameManager调用的强制结束游戏的方法
    public void ForceEndGame()
    {
        isGameActive = false;
    }
    #endregion
    // 游戏结束逻辑
    private void GameOver()
    {
        Debug.Log("生命值为0，游戏结束！通知GameManager...");

        // 1. 设置游戏状态为不激活
        isGameActive = false;

        // 2. 停止龙舟的当前速度 
        baseForwardSpeed = 0f; // 停止基础速度

        float survivalTime = Time.time - startTime;
        int timeScore = (int)(survivalTime * 10);

        int baseDistanceScore = (int)distanceTraveled;
        int finalTotalScore = currentScore + timeScore + baseDistanceScore;

        GameManager.Instance.EndGame(finalTotalScore, distanceTraveled, survivalTime);
    }
}