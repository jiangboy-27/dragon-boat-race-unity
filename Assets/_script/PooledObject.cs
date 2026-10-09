using UnityEngine;

// 辅助脚本，挂在所有需要被池化的预制体上
// 帮助对象池识别这个对象的“原始类型”
public class PooledObject : MonoBehaviour
{
    // 这个引用将在对象池初始化时被设置
    public GameObject originalPrefab;
}