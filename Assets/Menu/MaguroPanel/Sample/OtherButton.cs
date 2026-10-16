using UnityEngine;

namespace maguroPanelSample
{
    public class OtherButton : MonoBehaviour
    {
        public void ChangeColor()
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = new Color(Random.value, Random.value, Random.value);
            }
        }
    }
}