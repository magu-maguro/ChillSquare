using UnityEngine;

/// <summary>
/// PlayerInputActionsを使用してメニューの操作
/// Menuが開かれている間はプレイヤーの操作を受け付けない
/// </summary>
public class MenuController : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    private void OnEnable()
    {
        inputActions.Menu.Enable();

        //開閉
        inputActions.Menu.OpenClose.performed += ctx =>
        {
            Debug.Log("Open/Close Menu");
        };
    }
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
