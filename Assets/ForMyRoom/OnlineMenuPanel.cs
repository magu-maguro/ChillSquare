using UnityEngine;

public class OnlineMenuPanel : MenuPanelBehaviour
{
    [SerializeField] private MyRoomMenuManager menuManager;

    protected override void CloseMenu()
    {
        menuManager.CloseMenu(gameObject);
        Debug.Log("オンラインメニューを閉じました。");
    }
}
