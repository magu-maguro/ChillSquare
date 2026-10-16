using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaguroPanel
{
    [System.Serializable]
    public class ButtonSelectionEvent : UnityEvent<Button> { }

    /// <summary>
    /// キー操作で完結し、階層構造の作りやすい UI Panel
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private List<MenuChildLink> childLinks = new();
        [SerializeField] private Button initialSelectedButton;

        public enum NavigationDirection
        {
            Vertical,
            Horizontal
        }

        [SerializeField]
        private NavigationDirection navigationDirection =
            NavigationDirection.Vertical;

        public NavigationDirection Direction => navigationDirection;

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;
        public ButtonSelectionEvent onSelectionChanged;

        private readonly List<Button> buttons = new();
        private readonly Dictionary<Button, MenuPanel> childMap = new();
        private readonly Dictionary<Button, UnityAction> buttonCallbacks = new();

        private MenuManager manager;
        private bool initialized;

        public IReadOnlyList<Button> Buttons => buttons;
        public Button InitialSelectedButton => initialSelectedButton;

        
        private void Awake()
        {
            Initialize();
        }

        /*
        private void OnEnable()
        {
            Initialize();
        }
        */

        private void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            // Panel内の全Buttonを取得する
            buttons.Clear();
            buttons.AddRange(GetComponentsInChildren<Button>(true));

            // 子Panelへの遷移先を辞書に登録する
            childMap.Clear();

            foreach (var link in childLinks)
            {
                if (link.button == null || link.childPanel == null)
                {
                    Debug.LogWarning($"{name}: MenuChildLinkの設定が不完全です。", this);
                    continue;
                }

                if (!buttons.Contains(link.button))
                {
                    Debug.LogWarning($"{name}: 紐づけられたButtonがPanel内にありません。", this);
                    continue;
                }

                if (childMap.ContainsKey(link.button))
                {
                    Debug.LogWarning($"{name}: 同じButtonが複数登録されています。", this);
                    continue;
                }

                childMap.Add(link.button, link.childPanel);
            }

            // 全Buttonに共通のクリック処理を登録する
            foreach (var button in buttons)
            {
                UnityAction callback = () => HandleButtonClicked(button);

                buttonCallbacks.Add(button, callback);
                button.onClick.AddListener(callback);
            }
        }

        public void Bind(MenuManager menuManager)
        {
            Initialize();
            manager = menuManager;
        }

        private void HandleButtonClicked(Button button)
        {
            Debug.Log(
                $"HandleButtonClicked: Panel={name}, Button={button.name}",
                this
            );
            if (manager == null)
                return;

            // 現在のPanel以外からのクリック処理を無視する
            if (manager.CurrentPanel != this)
                return;

            // 子Panelに対応するButtonなら遷移する
            if (childMap.TryGetValue(button, out var childPanel))
            {
                manager.OpenChild(this, childPanel);
            }

            // 子Panelに対応しないButtonは何もしない。
            // Inspectorで設定したonClickイベントは通常どおり実行される。
        }

        public void Show()
        {
            Initialize();
            gameObject.SetActive(true);
            onOpened?.Invoke();
        }

        public void Hide()
        {
            onClosed?.Invoke();
            gameObject.SetActive(false);
        }

        public bool ContainsButton(Button button)
        {
            return button != null && buttons.Contains(button);
        }

        public void NotifySelectionChanged(Button button)
        {
            onSelectionChanged?.Invoke(button);
        }

        private bool IsSelectable(Button button)
        {
            return button != null
                && buttons.Contains(button)
                && button.isActiveAndEnabled
                && button.interactable;
        }

        private Button FindFirstSelectableButton()
        {
            foreach (var button in buttons)
            {
                if (IsSelectable(button)) return button;
            }

            return null;
        }

        private void OnDestroy()
        {
            foreach (var pair in buttonCallbacks)
            {
                if (pair.Key != null) pair.Key.onClick.RemoveListener(pair.Value);
            }
        }
    }
}