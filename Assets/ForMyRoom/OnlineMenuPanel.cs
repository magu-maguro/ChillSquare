using UnityEngine;

public class OnlineMenuPanel : MenuPanelBehaviour
{
    [SerializeField] private MyRoomMenuManager menuManager;
    [SerializeField] private MatchmakingManager matchmakingManager;

    protected override void CloseMenu()
    {
        menuManager.CloseMenu(gameObject);
        Debug.Log("オンラインメニューを閉じました。");
    }

    public void OnPublicButtonClicked()
    {
        //マッチメイキング中は処理しない
        matchmakingManager.StartMatchmaking();
    }
}
