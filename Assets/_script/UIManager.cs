using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject mainUIPanel;
    public GameObject dragonBoatPanel;
    public GameObject blessingPanel;

    [Header("按钮引用")]
    public Button startGameButton;
    public Button openDragonBoatPanelButton;
    public Button openBlessingPanelButton;

    void Start()
    {
        // 在游戏开始时，播放主界面的BGM
        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuBGM);
        // --- 绑定按钮事件 ---
        startGameButton.onClick.AddListener(StartGame);

        // 打开面板
        if (openDragonBoatPanelButton != null)
            openDragonBoatPanelButton.onClick.AddListener(() => OpenPanel(dragonBoatPanel));
        if (openBlessingPanelButton != null)
            openBlessingPanelButton.onClick.AddListener(() => OpenPanel(blessingPanel));


        // --- 初始化UI状态 ---
        mainUIPanel.SetActive(true);
        if (dragonBoatPanel != null) dragonBoatPanel.SetActive(false);
        if (blessingPanel != null) blessingPanel.SetActive(false);

    }
    private void StartGame()
    {
        Debug.Log("开始游戏！");
        SceneManager.LoadScene("GameScene");
    }
    // --- 通用的面板管理方法 ---
    // 处理所有面板的打开
    private void OpenPanel(GameObject panelToOpen)
    {
        if (mainUIPanel != null) mainUIPanel.SetActive(false);
        if (panelToOpen != null) panelToOpen.SetActive(true);
    }

    // 子面板的“关闭”按钮调用
    public void ClosePanelAndReturnToMain(GameObject panelToClose)
    {
        if (panelToClose != null) panelToClose.SetActive(false);
        if (mainUIPanel != null) mainUIPanel.SetActive(true);
    }
    void OnDestroy()
    {
        if (startGameButton != null) startGameButton.onClick.RemoveAllListeners();
        if (openDragonBoatPanelButton != null) openDragonBoatPanelButton.onClick.RemoveAllListeners();
        if (openBlessingPanelButton != null)
            openBlessingPanelButton.onClick.RemoveAllListeners();
    }
}
