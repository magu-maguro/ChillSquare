using UnityEngine;

public class MyRoomMenuManager : MonoBehaviour
{
    public void OpenMenu(GameObject panel)
    {
        if(panel == null)
        {
            Debug.LogWarning("パネルが設定されていません。");
            return;
        }
        panel.SetActive(true);

        InputManager.Instance.ActivateMenuInput();
    }

    public void CloseMenu(GameObject panel)
    {
        panel.SetActive(false);

        InputManager.Instance.DeactivateMenuInput();
    }
}
