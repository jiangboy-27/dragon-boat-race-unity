using UnityEngine;
using System.Collections.Generic;

public enum ObjectType
{
    Buff,      // 增益类 (粽子, 丝线, 黄金龙首)
    Debuff,    // 减益类 (浮木, 礁石, 漩涡)
    Event      // 特殊事件 
}
// 用于在Inspector中方便地设置带权重的预制体
[System.Serializable]
public class SpawnableObject
{
    public GameObject prefab;
    public ObjectType type;
    [Range(1, 100)]
    public int weight = 50; // 权重越高，出现的概率越大
}

public class ObjectSpawner : MonoBehaviour
{
    [Header("核心设置")]
    public Transform playerTransform;
    [Tooltip("所有航道的Z轴坐标，必须和玩家控制器中的一致")]
    public float[] lanePositions;

    [Header("生成逻辑")]
    [Tooltip("在玩家前方多远的位置(X轴)生成对象")]
    public float spawnDistanceX = 30f;
    // 随机偏移距离
    [Tooltip("在spawnDistanceX基础上，前后随机偏移的最大距离")]
    public float spawnDistanceJitter = 5f;

    [Tooltip("玩家每前进多少米(X轴)，进行一次生成尝试")]
    public float spawnInterval = 10f;
    // 生成概率控制 
    [Tooltip("在每个生成点，实际生成物体的基础概率 (0-1)")]
    [Range(0f, 1f)]
    public float baseSpawnChance = 0.8f; // 80%的概率会生成东西

    [Tooltip("生成一行物体时，生成2个物体的概率 (0-1)")]
    [Range(0f, 1f)]
    public float spawnTwoObjectsChance = 0.6f; // 60%概率生成2个

    [Tooltip("在生成2个物体的情况下，再生成第3个物体的概率 (0-1)")]
    [Range(0f, 1f)]
    public float spawnThreeObjectsChance = 0.2f; // 20%概率生成3个
    [Tooltip("对象落后玩家多远时(X轴)，被回收")]
    public float despawnDistanceX = 10f;

    [Header("对象池与权重")]
    [Tooltip("所有可能生成的对象及其权重列表")]
    public List<SpawnableObject> spawnableObjects;
    [Tooltip("每个对象在池中的初始数量")]
    public int initialPoolSize = 10;

    // --- 私有变量 ---
    private Dictionary<GameObject, Queue<GameObject>> objectPool = new Dictionary<GameObject, Queue<GameObject>>();
    private List<GameObject> activeObjects = new List<GameObject>();
    private float lastSpawnX;
    private int lastSpawnedLane = -1; // 记录上次生成的航道，用于避免重复
    // 用于存储龙尾技能加成的变量
    private float silkChanceBonus = 0f; // 丝线出现概率的加成值

    // 缓存龙尾数据以提高性能
    private BoatPartData tailPartData;
    // 用于分类存储物品的列表
    private List<SpawnableObject> buffObjects = new List<SpawnableObject>();
    private List<SpawnableObject> debuffObjects = new List<SpawnableObject>();

    void Start()
    {
        // 加载数据并计算加成
        LoadTailUpgrades();
        InitializeObjectPool();
        // 在初始化时对物品进行分类 
        CategorizeSpawnableObjects();

        if (playerTransform != null)
        {
            lastSpawnX = playerTransform.position.x;
        }
    }
    // 将总列表分类 
    private void CategorizeSpawnableObjects()
    {
        foreach (var spawnable in spawnableObjects)
        {
            switch (spawnable.type)
            {
                case ObjectType.Buff:
                    buffObjects.Add(spawnable);
                    break;
                case ObjectType.Debuff:
                    debuffObjects.Add(spawnable);
                    break;
                    // case ObjectType.Event:
                    // eventObjects.Add(spawnable);
                    // break;
            }
        }
    }
    // 加载龙尾升级数据
    private void LoadTailUpgrades()
    {
        // 1. 加载龙尾数据文件
        tailPartData = Resources.Load<BoatPartData>("BoatPartData/Part_Tail");
        if (tailPartData == null)
        {
            Debug.LogError("无法加载龙尾数据 (BoatPartData/Part_Tail)！");
            return;
        }

        // 2. 获取龙尾等级
        int tailLevel = PlayerDataManager.GetBoatTailLevel();
        if (tailLevel <= 0) tailLevel = 1;

        // 3. 安全检查并获取对应等级的数据
        int index = tailLevel - 1;
        if (index >= tailPartData.levels.Count) index = tailPartData.levels.Count - 1;
        UpgradeLevel levelData = tailPartData.levels[index];

        // 4. 查找并应用丝线概率加成效果
        var silkEffect = levelData.effects.Find(e => e.type == BoatEffectType.SilkFindChance);
        if (silkEffect != null)
        {
            // 将百分比转换为小数，例如 10 -> 0.1
            silkChanceBonus = silkEffect.value / 100f;
            Debug.Log($"龙尾技能加载成功！丝线出现概率提升: {silkChanceBonus * 100}%");
        }
    }
    void Update()
    {
        if (playerTransform == null) return;

        // 检查是否需要生成新的对象
        if (playerTransform.position.x > lastSpawnX + spawnInterval)
        {
            SpawnNewObject();
            lastSpawnX = playerTransform.position.x;
        }

        // 检查并回收远处的对象
        RecycleDistantObjects();
    }

    // 1. 初始化对象池
    void InitializeObjectPool()
    {
        foreach (var spawnable in spawnableObjects)
        {
            GameObject prefab = spawnable.prefab;
            // 为每个预制体创建一个队列
            objectPool[prefab] = new Queue<GameObject>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject obj = Instantiate(prefab);
                // 在PooledObject脚本中记录它的“身份”
                obj.GetComponent<PooledObject>().originalPrefab = prefab;
                obj.SetActive(false);
                objectPool[prefab].Enqueue(obj);
            }
        }
        Debug.Log("对象池初始化完成！");
    }
    // 3. 生成新对象的主逻辑
    void SpawnNewObject()
    {
        // --- 1. 判定本次是否要生成 ---
        if (Random.Range(0f, 1f) > baseSpawnChance)
        {
            // 本次轮空，直接返回，创造出“安全区”
            return;
        }

        // --- 2. 决定本次要生成几个物体 ---
        int objectCountToSpawn = 1;
        if (Random.Range(0f, 1f) < spawnTwoObjectsChance)
        {
            objectCountToSpawn = 2;
            if (Random.Range(0f, 1f) < spawnThreeObjectsChance)
            {
                objectCountToSpawn = 3;
            }
        }

        // 如果航道数小于要生成的物体数，则修正
        if (objectCountToSpawn > lanePositions.Length)
        {
            objectCountToSpawn = lanePositions.Length;
        }

        // --- 3. 获取所有可用航道的索引列表，并打乱它 ---
        List<int> availableLaneIndices = new List<int>();
        for (int i = 0; i < lanePositions.Length; i++)
        {
            availableLaneIndices.Add(i);
        }
        // Fisher-Yates shuffle algorithm to randomize the list
        for (int i = 0; i < availableLaneIndices.Count; i++)
        {
            int temp = availableLaneIndices[i];
            int randomIndex = Random.Range(i, availableLaneIndices.Count);
            availableLaneIndices[i] = availableLaneIndices[randomIndex];
            availableLaneIndices[randomIndex] = temp;
        }

        // --- 4. 循环生成指定数量的物体 ---
        for (int i = 0; i < objectCountToSpawn; i++)
        {
            // a. 从打乱后的航道列表中，顺序取出一个，确保不重复
            int laneIndex = availableLaneIndices[i];

            // b. 决定这个位置是放增益还是减益
            ObjectType typeToSpawn;
            if (i == 0) typeToSpawn = ObjectType.Buff;
            else if (i == 1) typeToSpawn = ObjectType.Debuff;
            else typeToSpawn = (Random.Range(0, 2) == 0) ? ObjectType.Buff : ObjectType.Debuff;

            // c. 根据类型，从对应的池子里随机一个预制体
            GameObject prefabToSpawn = null;
            if (typeToSpawn == ObjectType.Buff && buffObjects.Count > 0)
            {
                prefabToSpawn = GetRandomWeightedPrefabFromList(buffObjects);
            }
            else if (typeToSpawn == ObjectType.Debuff && debuffObjects.Count > 0)
            {
                prefabToSpawn = GetRandomWeightedPrefabFromList(debuffObjects);
            }

            // d. 在指定航道上生成它
            if (prefabToSpawn != null)
            {
                // 加入随机位置偏移 
                float randomJitter = Random.Range(-spawnDistanceJitter, spawnDistanceJitter);
                SpawnPrefabAt(prefabToSpawn, laneIndex, randomJitter);
            }
        }
    }

    // 一个X轴偏移参数 
    private void SpawnPrefabAt(GameObject prefab, int laneIndex, float xOffset = 0f)
    {
        // 计算生成位置
        float spawnZ = lanePositions[laneIndex];
        // 在基础生成距离上，加入随机偏移
        float spawnX = playerTransform.position.x + spawnDistanceX + xOffset;
        Vector3 spawnPosition = new Vector3(spawnX, 0, spawnZ);

        GameObject spawnedObject = GetFromPool(prefab);
        spawnedObject.transform.position = spawnPosition;
        spawnedObject.transform.rotation = Quaternion.identity;
        spawnedObject.SetActive(true);
        activeObjects.Add(spawnedObject);
    }

    // 4. 回收远处的对象
    void RecycleDistantObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];

            if (!obj.activeInHierarchy || obj.transform.position.x < playerTransform.position.x - despawnDistanceX)
            {
                ReturnToPool(obj);
                activeObjects.RemoveAt(i);
            }
        }
    }
    // --- 对象池核心方法 ---

    private GameObject GetFromPool(GameObject prefab)
    {
        // 如果池中有可用的对象，则取出
        if (objectPool.ContainsKey(prefab) && objectPool[prefab].Count > 0)
        {
            return objectPool[prefab].Dequeue();
        }

        // 如果池已空，动态创建一个新的
        Debug.LogWarning($"对象池中没有足够的 {prefab.name}，正在动态创建一个。考虑增加Initial Pool Size。");
        GameObject obj = Instantiate(prefab);
        obj.GetComponent<PooledObject>().originalPrefab = prefab;
        return obj;
    }

    private void ReturnToPool(GameObject obj)
    {
        // 通过辅助脚本找到它的原始类型
        GameObject originalPrefab = obj.GetComponent<PooledObject>().originalPrefab;

        if (originalPrefab != null && objectPool.ContainsKey(originalPrefab))
        {
            obj.SetActive(false);
            objectPool[originalPrefab].Enqueue(obj);
        }
        else
        {
            // 如果找不到池，直接销毁
            Debug.LogError($"无法将对象 {obj.name} 返回对象池！");
            Destroy(obj);
        }
    }

    private GameObject GetRandomWeightedPrefabFromList(List<SpawnableObject> objectList)
    {
        if (objectList == null || objectList.Count == 0) return null;

        // --- 1. 动态计算传入列表的总权重 ---
        float dynamicTotalWeight = 0;
        foreach (var spawnable in objectList)
        {
            if (spawnable.prefab.CompareTag("Silk"))
            {
                dynamicTotalWeight += spawnable.weight * (1f + silkChanceBonus);
            }
            else
            {
                dynamicTotalWeight += spawnable.weight;
            }
        }

        // --- 2. 在新的动态总权重范围内进行随机 ---
        float randomValue = Random.Range(0, dynamicTotalWeight);
        float cumulativeWeight = 0;

        // --- 3. 遍历并根据动态权重选择物品 ---
        foreach (var spawnable in objectList)
        {
            float currentWeight = spawnable.weight;
            if (spawnable.prefab.CompareTag("Silk"))
            {
                currentWeight *= (1f + silkChanceBonus);
            }

            cumulativeWeight += currentWeight;
            if (randomValue <= cumulativeWeight)
            {
                return spawnable.prefab;
            }
        }

        return null;
    }
}