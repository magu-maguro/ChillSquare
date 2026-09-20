using UnityEngine;


public class OnlineMenuArea : MenuAreaBehaviour
{
    [SerializeField] private MyRoomMenuManager menuManager;
    [SerializeField] private GameObject onlineMenuPanel;
    protected override void OpenMenu()
    {
        menuManager.OpenMenu(onlineMenuPanel);
        Debug.Log("オンラインメニューを開きました。");
    }
}
