using UnityEngine;
using UnityEngine.UI;

namespace MaguroPanel
{
    /// <summary>
    /// Button と 子 Panel のリンク
    /// </summary>
    [System.Serializable]
    public class MenuChildLink
    {
        public Button button;
        public MenuPanel childPanel;
    }
}