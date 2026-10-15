using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("地块预制体 (可以是 Cube 或 Sprite)")]
    public GameObject tilePrefab;

    [Header("地块间距")]
    public float tileSize = 1f;

    // 将我们之前的区域颜色配置转换为 Unity 的字典
    private readonly Dictionary<char, string> regionColorsHex = new Dictionary<char, string>()
    {
        {'A', "#C0A9E0"}, // 东北英格兰
        {'B', "#F9E076"}, // 西北英格兰
        {'C', "#A0C4E2"}, // 约克郡-亨伯
        {'D', "#B5E3A4"}, // 东米德兰兹
        {'E', "#FFC8A2"}, // 西米德兰兹
        {'F', "#FFB7A1"}, // 东英格兰
        {'G', "#FF4D6D"}, // 大伦敦
        {'H', "#F5D061"}, // 东南英格兰
        {'I', "#8AB6F9"}  // 西南英格兰
    };

    // 区域名称映射，用于整洁地命名各个分类父物体
    private readonly Dictionary<char, string> regionNames = new Dictionary<char, string>()
    {
        {'A', "North_East"},
        {'B', "North_West"},
        {'C', "Yorkshire_Humber"},
        {'D', "East_Midlands"},
        {'E', "West_Midlands"},
        {'F', "East_of_England"},
        {'G', "London"},
        {'H', "South_East"},
        {'I', "South_West"}
    };

    // 56 x 56 的地图数据 (与 React 版本保持一致)
    private readonly string[] gridData = new string[]
    {
        "..........................AAAAAA........................",
        "..........................AAAAAA........................",
        "........................AAAAAAAAAA......................",
        "........................AAAAAAAAAA......................",
        "......................AAAAAAAAAAAA......................",
        "......................AAAAAAAAAAAA......................",
        "....................BBAAAAAAAAAA........................",
        "....................BBAAAAAAAAAA........................",
        "..................BBBBBBAAAAAA..........................",
        "..................BBBBBBAAAAAA..........................",
        "..................BBBBBBCCCCCCCCCCCC....................",
        "..................BBBBBBCCCCCCCCCCCC....................",
        "................BBBBBBCCCCCCCCCCCCCC....................",
        "................BBBBBBCCCCCCCCCCCCCC....................",
        "................BBBBBBCCCCCCCCCCCCCCCC..................",
        "................BBBBBBCCCCCCCCCCCCCCCC..................",
        "..............BBBBBBBBCCCCCCCCCCCCCCCC..................",
        "..............BBBBBBBBCCCCCCCCCCCCCCCC..................",
        "..............BBBBBBCCCCCCCCCCCCCCCCCC..................",
        "..............BBBBBBCCCCCCCCCCCCCCCCCC..................",
        "................BBBBCCCCCCCCCCCCCCCC....................",
        "................BBBBCCCCCCCCCCCCCCCC....................",
        "................EEEEDDDDDDDDDDDD........................",
        "................EEEEDDDDDDDDDDDD........................",
        "..............EEEEEEDDDDDDDDDDDDDD......................",
        "..............EEEEEEDDDDDDDDDDDDDD......................",
        "............EEEEEEEEEEEEDDDDDDDDDD......FFFF............",
        "............EEEEEEEEEEEEDDDDDDDDDD......FFFF............",
        "............EEEEEEEEEEEEDDDDDDDDDD....FFFFFFFFFF........",
        "............EEEEEEEEEEEEDDDDDDDDDD....FFFFFFFFFF........",
        "............EEEEEEEEEEEEEEDDDDDDDDFFFFFFFFFFFFFF........",
        "............EEEEEEEEEEEEEEDDDDDDDDFFFFFFFFFFFFFF........",
        "............EEEEEEEEEEEEEEDDDDDDFFFFFFFFFFFFFFFF........",
        "............EEEEEEEEEEEEEEDDDDDDFFFFFFFFFFFFFFFF........",
        "..............EEEEEEEEEEDDDDDDDDFFFFFFFFFFFFFF..........",
        "..............EEEEEEEEEEDDDDDDDDFFFFFFFFFFFFFF..........",
        "................EEEEHHHHHHHHHHFFFFGGGGFFFF..............",
        "................EEEEHHHHHHHHHHFFFFGGGGFFFF..............",
        "..............IIIIIIHHHHHHHHHHHHGGGGHHHH................",
        "..............IIIIIIHHHHHHHHHHHHGGGGHHHH................",
        "............IIIIIIIIHHHHHHHHHHHHHHHHHHHH................",
        "............IIIIIIIIHHHHHHHHHHHHHHHHHHHH................",
        "..........IIIIIIIIIIIIHHHHHHHHHHHHHHHHHH................",
        "..........IIIIIIIIIIIIHHHHHHHHHHHHHHHHHH................",
        "........IIIIIIIIIIIIIIIIHHHHHHHHHHHHHH..................",
        "........IIIIIIIIIIIIIIIIHHHHHHHHHHHHHH..................",
        "....IIIIIIIIIIIIIIIIIIIIHHHHHHHH........................",
        "....IIIIIIIIIIIIIIIIIIIIHHHHHHHH........................",
        "..IIIIIIIIIIIIIIII......................................",
        "..IIIIIIIIIIIIIIII......................................",
        "IIIIIIIIIIII............................................",
        "IIIIIIIIIIII............................................",
        "IIIIII..................................................",
        "IIIIII..................................................",
        "II......................................................",
        "II......................................................"
    };

    // 使用 ContextMenu，这样不用运行游戏，在编辑器里右键就能生成
    [ContextMenu("生成地图 (Generate Map)")]
    public void GenerateMap()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("请先在 Inspector 面板中赋值 Tile Prefab！");
            return;
        }

        // 清除旧的子物体，防止重复生成
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        int height = gridData.Length;
        int width = gridData[0].Length;

        // 用于缓存并管理各个区域父物体的字典
        Dictionary<char, Transform> regionParents = new Dictionary<char, Transform>();

        for (int y = 0; y < height; y++)
        {
            string row = gridData[y];
            for (int x = 0; x < width; x++)
            {
                char cell = row[x];

                // '.' 代表海洋，跳过不生成预制体
                if (cell == '.') continue;

                // --- 核心改动 1：自动归类到对应的父级空物体下 ---
                if (!regionParents.ContainsKey(cell))
                {
                    // 如果还没有这个大区的父物体，就新建一个
                    string rName = regionNames.ContainsKey(cell) ? regionNames[cell] : cell.ToString();
                    GameObject parentObj = new GameObject($"Region_{cell}_{rName}");
                    parentObj.transform.SetParent(this.transform);
                    parentObj.transform.localPosition = Vector3.zero;
                    regionParents[cell] = parentObj.transform;
                }

                // --- 核心改动 2：沿着 X 和 Z 轴平铺 (地图平躺在地面上) ---
                Vector3 pos = new Vector3(x * tileSize, 0, -y * tileSize);

                // 实例化预制体，并将其作为对应大区父物体的子节点
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, regionParents[cell]);
                tile.name = $"Tile_{x}_{y}";

                // 尝试根据字符设置颜色
                if (regionColorsHex.TryGetValue(cell, out string hex))
                {
                    ColorUtility.TryParseHtmlString(hex, out Color regionColor);

                    // 1. 如果是 3D 物体 (有 Renderer)
                    Renderer rnd = tile.GetComponent<Renderer>();
                    if (rnd != null)
                    {
                        rnd.GetPropertyBlock(propBlock);
                        propBlock.SetColor("_Color", regionColor);
                        rnd.SetPropertyBlock(propBlock);
                    }

                    // 2. 如果是 2D 物体 (有 SpriteRenderer)
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = regionColor;
                    }
                }
            }
        }

        Debug.Log($"地图生成完毕！总行数: {height}, 每行格数: {width}");
    }

    [ContextMenu("清除地图 (Clear Map)")]
    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}