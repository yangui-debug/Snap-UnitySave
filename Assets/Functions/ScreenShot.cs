using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Vector2 rect=new Vector2(1024,1024);
    [SerializeField] private int viewLayer=6;

    private int index = 1;
    private string savePath = "D:/";

    private void Awake()
    {
        Debug.Log($"[截取头像]: 设定保存目录 -> {savePath}");
        Debug.Log($"[截取头像]: 点击游戏窗口，按下A键 截图");
        var getCamera = transform.GetComponent<Camera>();
        if (getCamera != null)
        {
            viewCamera = getCamera;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))// 按下A键截图
        {
            Shot();
        }
    }

    private void Shot()
    {
        if (viewCamera == null)
        {
            Debug.Log($"[截取头像]: 没有绑定相机");
            return;
        }
        RenderTexture renderTexture = new RenderTexture((int)rect.x, (int)rect.y, 24);
        Camera camera = viewCamera; // 获取SceneView的相机
        var oldClearFlag = camera.clearFlags;
        var oldMask = camera.cullingMask;
        // 将相机渲染到 RenderTexture
        camera.clearFlags = CameraClearFlags.Depth;
        camera.targetTexture = renderTexture;
        camera.cullingMask = 2<<(viewLayer-1);
        camera.Render();

        // 从 RenderTexture 中读取数据创建 Texture2D
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        var path = $"{savePath}{index}.png";
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"[截取头像]: 保存到了 -> {path}");
        camera.clearFlags = oldClearFlag;
        camera.targetTexture = null;
        camera.cullingMask=oldMask;
        camera.Render();
        index++;
    }
}
