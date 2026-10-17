using UnityEngine;
using MaguroPanel;


public class OnlineMenuArea : MenuAreaBehaviour
{
    [SerializeField] private MyRoomMenuManager myRoomMenuManager;
    [SerializeField] private MenuManager onlineMenuManager;
    protected override void OpenMenu()
    {
        if(myRoomMenuManager.isOpen) return;
        myRoomMenuManager.OpenMenu(onlineMenuManager);
        Debug.Log("オンラインメニューを開きました。");
    }
}
