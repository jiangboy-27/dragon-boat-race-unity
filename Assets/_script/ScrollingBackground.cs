using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [Header("背景图片引用")]
    public RectTransform bgImage1;
    public RectTransform bgImage2;

    [Header("滚动速度")]
    [Tooltip("背景滚动的速度，会受到龙舟当前速度的影响")]
    public float scrollSpeedMultiplier = 0.5f; // 背景滚动的速度是龙舟速度的50%

    // --- 私有变量 ---
    private float imageWidth; // 单张背景图的宽度
    private DragonBoatController playerController; // 对玩家控制器的引用

    void Start()
    {
        // 1. 获取图片的宽度
        if (bgImage1 != null)
        {
            imageWidth = bgImage1.rect.width;
        }
        else
        {
            Debug.LogError("背景图片1 (bgImage1) 未被引用！");
            this.enabled = false; // 禁用此脚本以防报错
            return;
        }

        // 2. 找到玩家控制器以获取实时速度
        playerController = FindObjectOfType<DragonBoatController>();
        if (playerController == null)
        {
            Debug.LogError("场景中找不到 DragonBoatController！");
            this.enabled = false;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // 1. 计算当前帧背景应该滚动的速度
        float currentScrollSpeed = playerController.CurrentForwardSpeed * scrollSpeedMultiplier;

        // 2. 移动两张背景图片
        bgImage1.anchoredPosition += Vector2.left * currentScrollSpeed * Time.deltaTime;
        bgImage2.anchoredPosition += Vector2.left * currentScrollSpeed * Time.deltaTime;

        // 3. 检查并处理循环
        // 检查第一张图片
        if (bgImage1.anchoredPosition.x <= -imageWidth)
        {
            // 如果 bgImage1 完全移出左侧屏幕，将它传送到 bgImage2 的右侧
            bgImage1.anchoredPosition = new Vector2(bgImage2.anchoredPosition.x + imageWidth, bgImage1.anchoredPosition.y);
        }

        // 检查第二张图片
        if (bgImage2.anchoredPosition.x <= -imageWidth)
        {
            // 如果 bgImage2 完全移出左侧屏幕，将它传送到 bgImage1 的右侧
            bgImage2.anchoredPosition = new Vector2(bgImage1.anchoredPosition.x + imageWidth, bgImage2.anchoredPosition.y);
        }
    }
}