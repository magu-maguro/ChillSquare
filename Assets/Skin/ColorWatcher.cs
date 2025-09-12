using UnityEngine;
using UnityEngine.UI;

public class ColorWatcher : MonoBehaviour
{
    private Image img;
    private Color last;

    void Start()
    {
        img = GetComponent<Image>();
        last = img.color;
    }

    void Update()
    {
        if (img.color != last)
        {
            Debug.Log($"[ColorWatcher] {gameObject.name} color changed: {last} -> {img.color}\nStackTrace:\n{System.Environment.StackTrace}");
            last = img.color;
        }
    }
}
