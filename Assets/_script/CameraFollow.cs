using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 龙舟对象

    [Header("摄像机位置")]
    [Tooltip("摄像机在Y轴上的固定高度")]
    public float cameraHeight = 10f;
    [Tooltip("摄像机在Z轴上的固定距离 (决定俯视角度)")]
    public float cameraDistance = -4f;
    [Tooltip("摄像机与龙舟在X轴上的固定偏移量")]
    public float xOffset = 10f;

    void LateUpdate()
    {
        // 1. 安全检查
        if (target == null)
        {
            Debug.LogWarning("摄像机没有指定跟随目标！");
            return;
        }

        // 2. 计算摄像机期望的X坐标
        float desiredX = target.position.x + xOffset;

        // 3. 构建摄像机的最终位置
        Vector3 newPosition = new Vector3(desiredX, cameraHeight, cameraDistance);

        // 4. 直接设置摄像机的位置
        transform.position = newPosition;

    }
}