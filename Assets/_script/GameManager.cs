using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // --- 单例模式实现 ---
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // 如果场景中已经存在一个实例，则销毁这个新的，保证唯一性
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    // --- 单例模式结束 ---

    [Header("UI引用")] 
    public GameObject settlementPanel; // 结算/暂停面板
    public TextMeshProUGUI panelTitleText; //面板的标题文本
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI rewardText;
    public GameObject restartButton; // 需要单独控制“再来一局”按钮的显隐
    public GameObject returnMainButton; // 需要单独控制“返回主菜单”按钮的显隐
    public GameObject settleNowButton; //结算按钮的引用

    [Header("游戏逻辑引用")]
    public DragonBoatController dragonBoatController; // 需要引用龙舟控制器

    // --- 暂停状态变量 ---
    private bool isPaused = false;

    // 游戏开始时，确保结算面板是关闭的
    void Start()
    {
        settlementPanel.SetActive(false);
        Time.timeScale = 1f; // 确保游戏时间是正常流逝的
    }

    // --- Update方法用于监听输入 ---
    void Update()
    {
        // 游戏结束时，不能再暂停
        if (dragonBoatController != null && !dragonBoatController.IsGameActive())
        {
            return;
        }

        // 监听空格键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    // --- 暂停游戏的方法 ---
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        settlementPanel.SetActive(true);
        panelTitleText.text = "游戏暂停";

        // 获取实时数据
        int currentScore = dragonBoatController.GetCurrentScoreForUI();
        float currentDistance = dragonBoatController.GetDistanceTraveled();
        float currentTime = dragonBoatController.GetSurvivalTime();

        // 更新实时数据文本
        scoreText.text = "当前得分: " + currentScore;
        distanceText.text = "航行距离: " + currentDistance.ToString("F0") + " 米";
        timeText.text = "坚持时长: " + currentTime.ToString("F1") + " 秒";

        // --- 调用方法显示奖励预览 ---
        rewardText.gameObject.SetActive(true); // 确保奖励文本框是显示的
        CalculateAndShowRewards(currentScore, currentDistance, currentTime);

        // 配置按钮显隐
        restartButton.SetActive(false);
        returnMainButton.SetActive(false);
        settleNowButton.gameObject.SetActive(true); // 显示“结算奖励”按钮
    }
    // --- 继续游戏的方法 ---
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 恢复游戏时间
        settlementPanel.SetActive(false); // 隐藏面板
        settleNowButton.gameObject.SetActive(false);
    }
    // --- 奖励计算和显示逻辑 ---
    private void CalculateAndShowRewards(int score, float distance, float time)
    {
        // --- 计算奖励 ---
        int normalWood = score / 100;
        int rareWood = (int)(distance / 200);
        int legendaryWood = (int)(time / 20);
        int dragonXp = score / 10;

        // 显示奖励文本
        rewardText.text = $"<color=yellow>可获奖励预览</color>\n" + 
                          $"<color=green>普通木料 x{normalWood}</color>\n" +
                          $"<color=blue>稀有木料 x{rareWood}</color>\n" +
                          $"<color=purple>传说木料 x{legendaryWood}</color>\n" +
                          $"龙舟经验 x{dragonXp}\n";
    }
    // --- “直接结算”按钮调用的方法 ---
    public void SettleGameAndExit()
    {
        Debug.Log("玩家选择主动结算...");
        // 1. 获取最终数据
        int finalScore = dragonBoatController.GetCurrentScoreForUI();
        float finalDistance = dragonBoatController.GetDistanceTraveled();
        float finalTime = dragonBoatController.GetSurvivalTime();

        // 2. 保存奖励
        SaveRewards(finalScore, finalDistance, finalTime);

        // 3. 禁用龙舟控制器，正式结束游戏
        if (dragonBoatController != null) dragonBoatController.ForceEndGame();

        // 4. 返回主菜单
        ReturnMain();
    }
    // --- 保存奖励 ---
    private void SaveRewards(int score, float distance, float time)
    {
        int normalWood = score / 100;
        int rareWood = (int)(distance / 200);
        int legendaryWood = (int)(time /30);
        int dragonXp = score / 10;

        // 调用PlayerDataManager来保存奖励
        PlayerDataManager.AddCurrency(normalWood, rareWood, legendaryWood, dragonXp);
        Debug.Log("奖励已保存！");
    }
    /// <summary>
    /// 结束游戏的公共方法，由DragonBoatController调用
    /// </summary>
    public void EndGame(int finalScore, float finalDistance, float survivalTime)
    {
        // 只处理“死亡结算”的情况
        Time.timeScale = 0f;
        settlementPanel.SetActive(true);
        panelTitleText.text = "本局结算";

        // 更新最终数据文本
        scoreText.text = "最终得分: " + finalScore;
        distanceText.text = "航行距离: " + finalDistance.ToString("F0") + " 米";
        timeText.text = "坚持时长: " + survivalTime.ToString("F1") + " 秒";

        // 保存并显示奖励
        SaveRewards(finalScore, finalDistance, survivalTime);
        CalculateAndShowRewards(finalScore, finalDistance, survivalTime);

        // 配置按钮显隐
        rewardText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        returnMainButton.SetActive(true);
        settleNowButton.gameObject.SetActive(false); // 死亡结算时不显示“直接结算”
    }
    /// <summary>
    /// “再来一局”按钮调用的方法
    /// </summary>
    public void RestartGame()
    {
        // 恢复时间流逝
        Time.timeScale = 1f;

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ReturnMain()
    {
        // 恢复时间流逝
        Time.timeScale = 1f;
        // 返回主菜单场景
        SceneManager.LoadScene("MainScene");
    }
}