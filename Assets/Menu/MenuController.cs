using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerInputActionsを使用してメニューの操作
/// Menuが開かれている間はプレイヤーの操作を受け付けない
/// </summary>
public class MenuController : MonoBehaviour
{
    private PlayerInputActions inputActions;

    private bool isMenuOpen = false;
    [SerializeField] private GameObject MenuRoot;


    private void OnEnable()
    {
        inputActions = InputManager.Instance.GetInputActions();
        if (inputActions == null) return;
        inputActions.Menu.Disable();

        //開
        inputActions.Player.OpenMenu.performed += ctx =>
        {
            if (!isMenuOpen) OpenMenu();
        };
        //閉
        inputActions.Menu.CloseMenu.performed += ctx =>
        {
            if (isMenuOpen) CloseMenu();
        };
    }

    private void OnDisable()
    {
        if (inputActions == null) return;

        inputActions.Player.OpenMenu.performed -= ctx =>
        {
            if (!isMenuOpen) OpenMenu();
        };
        inputActions.Menu.CloseMenu.performed -= ctx =>
        {
            if (isMenuOpen) CloseMenu();
        };
    }

    private void OpenMenu()
    {
        Debug.Log("Open Menu");
        isMenuOpen = true;
        inputActions.Menu.Enable();
        inputActions.Player.Disable();
        // メニューUIの表示などの処理をここに追加
        MenuRoot.SetActive(true);
    }
    private void CloseMenu()
    {
        Debug.Log("Close Menu");
        isMenuOpen = false;
        inputActions.Menu.Disable();
        inputActions.Player.Enable();
        // メニューUIの非表示などの処理をここに追加
        MenuRoot.SetActive(false);
    }
}
