using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// PlayerInputActionsを使用してメニューの操作
/// Menuが開かれている間はプレイヤーの操作を受け付けない
/// TabSelector, ItemSelectorに指示
/// </summary>
public class MenuController : MonoBehaviour
{
    private PlayerInputActions inputActions;

    private bool isMenuOpen = false;
    [SerializeField] private GameObject MenuRoot;
    [SerializeField] private TabSelector tabSelector;

    // Navigate 用
    private Vector2 currentNavigate;
    private Coroutine navigateRepeatCoroutine;
    private void OnEnable()
    {
        inputActions = InputManager.Instance.GetInputActions();
        if (inputActions == null) return;
        inputActions.Menu.Disable();

        inputActions.Player.OpenMenu.performed += ctx =>
        {
            if (!isMenuOpen) OpenMenu();
        };
        inputActions.Menu.CloseMenu.performed += ctx =>
        {
            if (isMenuOpen) CloseMenu();
        };
        // メニュー操作（上下左右）
        inputActions.Menu.Navigate.performed += OnNavigatePerformed;
        inputActions.Menu.Navigate.canceled  += OnNavigateCanceled;
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
        inputActions.Menu.Navigate.performed -= OnNavigatePerformed;
        inputActions.Menu.Navigate.canceled  -= OnNavigateCanceled;
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

    // =========================
    // Navigate 入力
    // =========================

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        currentNavigate = context.ReadValue<Vector2>();

        // 1回分の移動
        HandleNavigateOnce(currentNavigate);

        // 長押し用コルーチン開始
        StartNavigateRepeat();
    }

    private void OnNavigateCanceled(InputAction.CallbackContext context)
    {
        StopNavigateRepeat();
    }

    // =========================
    // 呼び出される処理（中身は未実装）
    // =========================

    private void HandleNavigateOnce(Vector2 direction)
    {
        // 上下左右1回分の処理を書く
        //左右：Tabの切り替え
        if (direction.x > 0.5f) // 右
        {
            //Debug.Log("Navigate Right");
            tabSelector.ChangeTab(1);
        }
        else if (direction.x < -0.5f) // 左
        {
            //Debug.Log("Navigate Left");
            tabSelector.ChangeTab(-1);
        }
    }

    private void StartNavigateRepeat()
    {
        StopNavigateRepeat();
        navigateRepeatCoroutine = StartCoroutine(NavigateRepeatCoroutine());
    }

    private void StopNavigateRepeat()
    {
        if (navigateRepeatCoroutine != null)
        {
            StopCoroutine(navigateRepeatCoroutine);
            navigateRepeatCoroutine = null;
        }
    }

    private IEnumerator NavigateRepeatCoroutine()
    {
        // 長押し時のディレイ・リピート処理を書く
        yield break;
    }
}
