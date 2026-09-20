using UnityEngine;
using UnityEngine.InputSystem;

public abstract class MenuPanelBehaviour : MonoBehaviour
{
    protected virtual void Update()
    {
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }
    protected abstract void CloseMenu();
}
