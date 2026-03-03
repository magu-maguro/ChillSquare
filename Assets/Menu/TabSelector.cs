using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MenuControllerから指示を受けて、現在のTabViewを切り替える
/// </summary>
public class TabSelector : MonoBehaviour
{
    [SerializeField] private List<TabView> tabs;

    private int currentTabIndex = 0;

    void Start()
    {
        //OnTabChanged(currentTabIndex);
        //TabViewクリック時の処理追加
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; // ローカル変数にコピーしてクロージャで使用
            tabs[i].GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnTabChanged(index));
        }
    }

    public void OnTabChanged(int newIndex)
    {
        //Debug.Log("OnTabChanged: " + newIndex);
        if(currentTabIndex == newIndex) return;
        if(newIndex < 0) currentTabIndex = tabs.Count - 1;
        else if(newIndex >= tabs.Count) currentTabIndex = 0;
        else currentTabIndex = newIndex;

        for (int i = 0; i < tabs.Count; i++)
        {
            tabs[i].SetSelected(i == currentTabIndex);
        }
    }

    public void ChangeTab(int direction)
    {
        OnTabChanged(currentTabIndex + direction);
    }
}
