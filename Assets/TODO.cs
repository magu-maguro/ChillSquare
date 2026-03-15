#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class TODO : MonoBehaviour
{
    [Multiline(5)]
    [SerializeField] private List<string> todoList = new List<string>();
}
#endif