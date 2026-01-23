using UnityEngine;
using System.Collections.Generic;

public class TabSelector : MonoBehaviour
{
    [SerializeField] private List<TabData> tabs;
    private int currentTabIndex;
}
