using UnityEngine;

/// <summary>
/// TabDataの表示役
/// </summary>
public class TabView : MonoBehaviour
{
    private bool isSelected;
    [SerializeField] private PanelView panelView;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        //選択されているTabの見た目を変える

        if(isSelected)
        {
            //選択されているTabの見た目
            if(panelView.gameObject.activeSelf == false)
            {
                panelView.gameObject.SetActive(true);
            }
        }
        else
        {
            //選択されていないTabの見た目
            if(panelView.gameObject.activeSelf == true)
            {
                panelView.gameObject.SetActive(false);
            }
        }
    }
}
