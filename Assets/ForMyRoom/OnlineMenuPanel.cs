using UnityEngine;
using MaguroPanel;

public class OnlineMenuPanel : MenuPanelBehaviour
{
    [SerializeField] private MyRoomMenuManager menuManager;
    private MatchmakingManager matchmakingManager;

    [SerializeField] private MessagePanelController messagePanel;

    private void Awake()
    {
        matchmakingManager = NetworkManager.Instance.GetComponent<MatchmakingManager>();
    }

    private void OnEnable()
    {
        matchmakingManager.MessageChanged += OnMessageChanged;
    }

    private void OnDisable()
    {
        matchmakingManager.MessageChanged -= OnMessageChanged;
    }

    private void OnMessageChanged(string message)
    {
        //メッセージを表示する処理をここに追加
        messagePanel.gameObject.SetActive(true);
        messagePanel.ShowMessage(message);
    }

    protected override void CloseMenu()
    {
        menuManager.CloseMenu();
        Debug.Log("オンラインメニューを閉じました。");
    }

    public void OnPublicButtonClicked()
    {
        GetComponent<MenuManager>().AllowControl(false);
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
        //menuManager.OpenMenu(privateMenuPanel);
    }
}
