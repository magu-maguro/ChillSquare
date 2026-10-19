using UnityEngine;
using MaguroPanel;

public class MyRoomMenuManager : MonoBehaviour
{
    public bool isOpen { get; private set; } = false;
    public void OpenMenu(MenuManager menuManager)
    {
        if(menuManager == null)
        {
            Debug.LogWarning("MenuManagerが設定されていません。");
            return;
        }
        menuManager.OpenRoot();

        isOpen = true;
        //InputManager.Instance.ActivateMenuInput();
        InputManager.Instance.ChangeActiveState_PlayerInput(false);
    }

    public void CloseMenu()
    {
        //panel?.SetActive(false);
        isOpen = false;

        //InputManager.Instance.DeactivateMenuInput();
        InputManager.Instance.ChangeActiveState_PlayerInput(true);
    }
}
