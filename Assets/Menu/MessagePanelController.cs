using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessagePanelController : MonoBehaviour
{
    private TextMeshProUGUI messageText;

    void Awake()
    {
        messageText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void ShowMessage(string message)
    {
        messageText.text = message;
    }

    public void HideMessage()
    {
        messageText.text = "";
    }
}
