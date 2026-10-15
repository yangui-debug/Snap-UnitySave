using UnityEngine;
using System.Collections.Generic;

public class Mapchaoxian : MonoBehaviour
{
    [Header("地块预制体 (可以是 Cube 或 Sprite)")]
    public GameObject tilePrefab;

    [Header("地块间距")]
    public float tileSize = 1f;

    // 包含了周边灰色国家的颜色字典
    private readonly Dictionary<char, string> regionColorsHex = new Dictionary<char, string>()
    {
        // 朝鲜 (DPRK)
        {'A', "#E2A9A9"}, // 咸镜北道 
        {'B', "#F2D0A9"}, // 两江道 
        {'C', "#E8E8A6"}, // 慈江道 
        {'D', "#B5E3A4"}, // 咸镜南道 
        {'E', "#A0C4E2"}, // 平安北道 
        {'F', "#C0A9E0"}, // 平安南道 
        {'G', "#FFC8A2"}, // 江原道-北 
        {'H', "#FFB7A1"}, // 黄海北道 
        {'I', "#F5D061"}, // 黄海南道 
        
        // 韩国 (ROK)
        {'J', "#FF4D6D"}, // 京畿道/首尔/仁川 
        {'K', "#8AB6F9"}, // 江原道-南 
        {'L', "#95D5B2"}, // 忠清北道 
        {'M', "#D8F3DC"}, // 忠清南道 
        {'N', "#B1A7A6"}, // 庆尚北道/大邱 
        {'O', "#E5989B"}, // 全罗北道 
        {'P', "#FFB4A2"}, // 庆尚南道/釜山/蔚山 
        {'Q', "#E0B1CB"}, // 全罗南道/光州 
        {'R', "#6D597A"}, // 济州道 (Jeju)

        // 邻国灰色区域 (Neighboring Countries)
        {'S', "#E5E5E5"}, // 中国 (China)
        {'T', "#D5D5D5"}  // 俄罗斯 (Russia)
    };

    // 区域名称映射
    private readonly Dictionary<char, string> regionNames = new Dictionary<char, string>()
    {
        {'A', "DPRK_Hamgyong_North"},
        {'B', "DPRK_Ryanggang"},
        {'C', "DPRK_Chagang"},
        {'D', "DPRK_Hamgyong_South"},
        {'E', "DPRK_Pyongan_North"},
        {'F', "DPRK_Pyongan_South"},
        {'G', "DPRK_Kangwon"},
        {'H', "DPRK_Hwanghae_North"},
        {'I', "DPRK_Hwanghae_South"},
        {'J', "ROK_Gyeonggi_Seoul"},
        {'K', "ROK_Gangwon"},
        {'L', "ROK_Chungcheong_North"},
        {'M', "ROK_Chungcheong_South"},
        {'N', "ROK_Gyeongsang_North"},
        {'O', "ROK_Jeolla_North"},
        {'P', "ROK_Gyeongsang_South"},
        {'Q', "ROK_Jeolla_South"},
        {'R', "ROK_Jeju"},
        {'S', "Neighbor_China"},
        {'T', "Neighbor_Russia"}
    };

    // 极高精度朝鲜半岛及周边 80 x 85 地图数据 (边境无缝接壤版本)
    private readonly string[] gridData = new string[]
    {
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSTTTT.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSTTT.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSTTT.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSTTTAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSAAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSAAAAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSAAAAAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSBBAAAAAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSBBBBAAAAAAAAAA.....",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSBBBBBBBBAAAAAAA......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSBBBBBBBBBAAAAAA.......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCBBBBBBBBBBBAAAAA.......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCBBBBBBBBBBBAAAA.......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCBBBBBBBBBBBDDD........",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCBBBBBBBBBBBDDDD.......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCBBBBBBBBBBBDDDD.......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCCBBBBBBBBBBBDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCCCBBBBBBBBBBDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCCCCBBBBBBBBBBDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCCCCCBBBBBBBBBDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSCCCCCCCCCCCBBBBBBBBDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEECCCCCCCCCCCBBBBBBBDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEECCCCCCCCCCCBBBBBBDDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEEECCCCCCCCCCCBBBBBBDDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEEEECCCCCCCCCCBBBBBBDDDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEEEEEECCCCCCCCCBBBBBDDDDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEEEEEEECCCCCCCCBBBBBDDDDDDDDDDDDD......",
        ".....SSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSEEEEEEEEECCCCCCCBBBBBDDDDDDDDDDDDD.......",
        ".....SSSSSSSSSSSSSSSSSSSS.............EEEEEEEEEECCCCCCCBBBBDDDDDDDDDDDDDD.......",
        ".....SSSSSSSSSSSSSSS.................EEEEEEEEEEECCCCCCCDDDDDDDDDDDDDDDDD........",
        ".....SSSSSSSSSS.....................EEEEEEEEEEEECCCCCCCDDDDDDDDDDDDDDDDD........",
        ".....SSSSS.........................EEEEEEEEEEEECCCCCCCDDDDDDDDDDDDDDDDD.........",
        "...................................EEEEEEEEEEEEFFFFFFGDDDDDDDDDDDDDDDD..........",
        "..................................EEEEEEEEEEEFFFFFFFGDDDDDDDDDDDDDDDD...........",
        ".................................EEEEEEEEEEEFFFFFFFFGGDDDDDDDDDDDDDD............",
        ".................................EEEEEEEEEFFFFFFFFFFGGDDDDDDDDDDDDDD............",
        "................................EEEEEEEEFFFFFFFFFFFFGGDDDDDDDDDDDDD.............",
        "................................EEEEEEFFFFFFFFFFFFFFGGDDDDDDDDDDDD..............",
        "...............................EEEEEFFFFFFFFFFFFFFFFGGGGDDDDDDDDDD..............",
        "...............................EEEEFFFFFFFFFFFFFFFFFGGGGDDDDDDDDD...............",
        "................................EEFFFFFFFFFFFFFFFFFFGGGGDDDDDDDD................",
        ".................................IFFFFFFFFFFFFFFFFFFGGGGDDDDDDD.................",
        "..................................IIFFFFFFFFFFFFFFFFGGGGDDDDDD..................",
        ".................................IIIHHHHHHHFFFFFFFFGGGGGDDDD....................",
        ".................................IIIHHHHHHHHHHHHHHHGGGGGDDD.....................",
        "................................IIIIHHHHHHHHHHHHHHHGGGGGGG......................",
        "................................IIIIHHHHHHHHHHHHHHHGGGGGGG......................",
        "...............................IIIIIHHHHHHHHHHHHHHHGGGGGG.......................",
        "...............................IIIIIHHHHHHHHHHHHHHHGGGGGG.......................",
        "................................IIIIHHHHHHHHHHHHHH..............................",
        ".................................IIIHHHHHHHHHHHHH...............................",
        "....................................JJJJJJJJJKKKKKKKKKKKKK......................",
        "...................................JJJJJJJJJJKKKKKKKKKKKKKK.....................",
        "...................................JJJJJJJJJJKKKKKKKKKKKKKK.....................",
        "...................................JJJJJJJJJJKKKKKKKKKKKKKK.....................",
        "...................................JJJJJJJJJJKKKKKKKKKKKKKK.....................",
        "..................................MMMMMMMMLLLLLLLNNNNNNNNNN.....................",
        "..................................MMMMMMMMLLLLLLLNNNNNNNNNNN....................",
        ".................................MMMMMMMMMLLLLLLNNNNNNNNNNNN....................",
        ".................................MMMMMMMMMLLLLLLNNNNNNNNNNNN....................",
        ".................................OOOOOOOOOLLLLLLNNNNNNNNNNNNN...................",
        ".................................OOOOOOOOOLLLLLNNNNNNNNNNNNNN...................",
        ".................................OOOOOOOOOLLLLLNNNNNNNNNNNNNN...................",
        ".................................OOOOOOOOOLLLLNNNNNNNNNNNNNNN...................",
        ".................................OOOOOOOOOPPPPPPPPPPPPPPPPPPP...................",
        ".................................QQQQQQQQQPPPPPPPPPPPPPPPPPPP...................",
        "..................................QQQQQQQQPPPPPPPPPPPPPPPPPPP...................",
        "..................................QQQQQQQQPPPPPPPPPPPPPPPPPPP...................",
        "..................................QQQQQQQQPPPPPPPPPPPPPPPPPPP...................",
        "...................................QQQQQQQQPPPPPPPPPPPPPPPPP....................",
        "...................................QQQQQQQ..PPPPPPPPPPPPPPP.....................",
        "....................................QQQQQ....PPPPPPPPPPPPP......................",
        "....................................QQQ.......PPPPPPPPPP........................",
        "................................................................................",
        "................................................................................",
        "................................................................................",
        "................................................................................",
        ".................................RRRRR..........................................",
        "................................RRRRRRR.........................................",
        ".................................RRRRR..........................................",
        "..................................RR............................................",
        "................................................................................",
        "................................................................................",
        "................................................................................"
    };

    [ContextMenu("生成朝鲜半岛地图")]
    public void GenerateMap()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("请先在 Inspector 面板中赋值 Tile Prefab！");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        int height = gridData.Length;
        int width = gridData[0].Length;
        Dictionary<char, Transform> regionParents = new Dictionary<char, Transform>();

        for (int y = 0; y < height; y++)
        {
            string row = gridData[y];
            for (int x = 0; x < width; x++)
            {
                char cell = row[x];
                if (cell == '.') continue;

                if (!regionParents.ContainsKey(cell))
                {
                    string rName = regionNames.ContainsKey(cell) ? regionNames[cell] : cell.ToString();
                    GameObject parentObj = new GameObject($"Region_{cell}_{rName}");
                    parentObj.transform.SetParent(this.transform);
                    parentObj.transform.localPosition = Vector3.zero;
                    regionParents[cell] = parentObj.transform;
                }

                Vector3 pos = new Vector3(x * tileSize, 0, -y * tileSize);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, regionParents[cell]);
                tile.name = $"Tile_{x}_{y}";

                if (regionColorsHex.TryGetValue(cell, out string hex))
                {
                    ColorUtility.TryParseHtmlString(hex, out Color regionColor);
                    Renderer rnd = tile.GetComponent<Renderer>();
                    if (rnd != null)
                    {
                        rnd.GetPropertyBlock(propBlock);
                        propBlock.SetColor("_Color", regionColor);
                        rnd.SetPropertyBlock(propBlock);
                    }
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = regionColor;
                    }
                }
            }
        }
        Debug.Log($"极高精度朝鲜半岛地图生成完毕 (已优化边境接壤)！总分辨率: {width} x {height}");
    }

    [ContextMenu("清除地图")]
    public void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}