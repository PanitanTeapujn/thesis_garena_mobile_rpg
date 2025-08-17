// สร้าง Script ใหม่: MobileDebugUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
public class MobileDebugUI : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    void Start()
    {
        if (debugText == null)
        {
            // สร้าง debug text บนหน้าจอ
            GameObject canvas = new GameObject("DebugCanvas");
            Canvas canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComp.sortingOrder = 999;

            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(canvas.transform);

            debugText = textObj.AddComponent<TextMeshProUGUI>();
            debugText.text = "Debug Info";
            debugText.fontSize = 20;
            debugText.color = Color.red;

            RectTransform rect = debugText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(0, 200);
        }
    }

    void Update()
    {
        if (debugText != null)
        {
            var runner = NetworkRunner.Current;
            var heroes = FindObjectsOfType<Hero>();

            debugText.text = $"Platform: {Application.platform}\n" +
                            $"Internet: {Application.internetReachability}\n" +
                            $"NetworkRunner: {(runner != null ? "OK" : "NULL")}\n" +
                            $"IsServer: {(runner?.IsServer ?? false)}\n" +
                            $"LocalPlayer: {(runner?.LocalPlayer ?? "NULL")}\n" +
                            $"Heroes: {heroes.Length}\n" +
                            $"GameMode: {(runner?.GameMode ?? GameMode.Single)}\n" +
                            $"Time: {Time.time:F1}";
        }
    }
}