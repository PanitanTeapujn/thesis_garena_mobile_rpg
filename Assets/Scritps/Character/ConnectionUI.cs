using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private ConnectionManager connectionManager;

    public void OnHostButtonClicked()
    {
        connectionManager.StartHost();
        gameObject.SetActive(false); // ซ่อน UI เมื่อเริ่มเกม
    }

    public void OnJoinButtonClicked()
    {
        connectionManager.ShowRoomList();
    }

    public void OnCloseRoomListClicked()
    {
        // ซ่อนหน้าต่างแสดงรายการห้อง
        transform.Find("RoomListPanel").gameObject.SetActive(false);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
