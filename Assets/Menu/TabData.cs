using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Menu/Tab")]
public class TabData : ScriptableObject
{
    public string tabName;
    public List<ItemData> items;
}
