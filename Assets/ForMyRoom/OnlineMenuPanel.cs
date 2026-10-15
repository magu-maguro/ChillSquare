using UnityEngine;

public class OnlineMenuPanel : MenuPanelBehaviour
{
    [SerializeField] private MyRoomMenuManager menuManager;
    //[SerializeField] private MatchmakingManager matchmakingManager;

    [SerializeField] private GameObject privateMenuPanel;

    protected override void CloseMenu()
    {
        menuManager.CloseMenu(gameObject);
        Debug.Log("オンラインメニューを閉じました。");
    }

    public void OnPublicButtonClicked()
    {
        //マッチメイキング中は処理しない
        if(NetworkManager.Instance.GetComponent<MatchmakingManager>().CurrentState != MatchmakingManager.MatchmakingState.Idle)
        {
            return;
        }
        NetworkManager.Instance.GetComponent<MatchmakingManager>().StartMatchmaking();
    }

    public void OnPrivateButtonClicked()
    {
        //マッチメイキング中は処理しない
        if(NetworkManager.Instance.GetComponent<MatchmakingManager>().CurrentState != MatchmakingManager.MatchmakingState.Idle)
        {
            return;
        }
        menuManager.OpenMenu(privateMenuPanel);
    }
}
