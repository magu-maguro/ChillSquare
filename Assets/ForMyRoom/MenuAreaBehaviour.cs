using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public abstract class MenuAreaBehaviour : MonoBehaviour
{
    private bool isPlayerInTrigger = false;
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }

    protected virtual void Update()
    {
        if(!isPlayerInTrigger)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            OpenMenu();
        }
    }

    protected abstract void OpenMenu();
}
