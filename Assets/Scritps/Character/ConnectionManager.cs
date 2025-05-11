using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private GameObject hostButton;
    [SerializeField] private GameObject joinButton;
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private GameObject roomListContent;
    [SerializeField] private GameObject roomButtonPrefab;
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner _runner;
    private List<SessionInfo> _sessionList = new List<SessionInfo>();

    public void StartHost()
    {
        StartGame(GameMode.Host, "MyRoom");
    }

    public void ShowRoomList()
    {
        roomListPanel.SetActive(true);
        StartGame(GameMode.Client, null);
    }

    public void JoinRoom(string roomName)
    {
        StartGame(GameMode.Client, roomName);
    }

    private async void StartGame(GameMode mode, string roomName)
    {
        // ถ้ามี runner อยู่แล้ว ให้ shutdown ก่อน
        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        _runner = Instantiate(runnerPrefab);
        _runner.ProvideInput = true;

        // ลงทะเบียน callbacks
        _runner.AddCallbacks(GetComponent<INetworkRunnerCallbacks>());

        await _runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void UpdateRoomList(List<SessionInfo> sessionList)
    {
        _sessionList = sessionList;

        // ลบห้องเก่าทั้งหมด
        foreach (Transform child in roomListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // สร้างปุ่มสำหรับแต่ละห้อง
        foreach (var session in sessionList)
        {
            GameObject roomButton = Instantiate(roomButtonPrefab, roomListContent.transform);
            roomButton.GetComponentInChildren<Text>().text = session.Name;
            roomButton.GetComponent<Button>().onClick.AddListener(() => JoinRoom(session.Name));
        }
    }
}