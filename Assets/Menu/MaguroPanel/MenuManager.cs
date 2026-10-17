using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaguroPanel
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private MenuPanel rootPanel;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Menu";
        [SerializeField] private string navigateActionName = "Navigate";
        [SerializeField] private string submitActionName = "Submit";
        [SerializeField] private string backActionName = "Back";

        [Header("Events")]
        public UnityEvent onMenuOpened;
        public UnityEvent onMenuClosed;
        public UnityEvent<MenuPanel> onChildPanelOpened;
        public UnityEvent<MenuPanel> onParentPanelOpened;

        private InputActionMap menuActionMap;
        private InputAction navigateAction;
        private InputAction submitAction;
        private InputAction backAction;

        private Button selectedButton;
        private GameObject previousEventSystemSelection;

        private readonly Stack<MenuPanel> panelStack = new();

        public MenuPanel CurrentPanel =>
            panelStack.Count > 0 ? panelStack.Peek() : null;

        private GameObject previousSelectedObject;

        public bool IsOpen => CurrentPanel != null;


        private bool isAllowedToControl = true;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    $"{nameof(MenuManager)}: InputActionAsset is not assigned.",
                    this);
                return;
            }

            menuActionMap = inputActions.FindActionMap(actionMapName, false);

            if (menuActionMap == null)
            {
                Debug.LogError(
                    $"Action Map '{actionMapName}' was not found.",
                    this);
                return;
            }

            navigateAction = menuActionMap.FindAction(
                navigateActionName, false);
            submitAction = menuActionMap.FindAction(
                submitActionName, false);
            backAction = menuActionMap.FindAction(
                backActionName, false);

            if (navigateAction == null ||
                submitAction == null ||
                backAction == null)
            {
                Debug.LogError(
                    "Menu input actions are missing. " +
                    "Check the Action names in the Inspector.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (navigateAction == null ||
                submitAction == null ||
                backAction == null)
                return;

            navigateAction.performed += OnNavigate;
            submitAction.performed += OnSubmit;
            backAction.performed += OnBack;
        }

        private void OnDisable()
        {
            if (navigateAction != null)
                navigateAction.performed -= OnNavigate;

            if (submitAction != null)
                submitAction.performed -= OnSubmit;

            if (backAction != null)
                backAction.performed -= OnBack;

            menuActionMap?.Disable();
        }

        public void OpenRoot()
        {
            if (rootPanel == null)
            {
                Debug.LogWarning("RootPanel is not assigned.", this);
                return;
            }

            bool wasOpen = IsOpen;

            if (CurrentPanel != null)
                CurrentPanel.Hide();

            panelStack.Clear();

            rootPanel.Bind(this);
            panelStack.Push(rootPanel);
            rootPanel.Show();

            SetInitialSelection(rootPanel);
            menuActionMap?.Enable();

            if (!wasOpen)
                onMenuOpened?.Invoke();
        }

        public void OpenChild(MenuPanel source, MenuPanel child)
        {
            Debug.Log(
                $"OpenChild: {source.name} -> {child.name}\n" +
                $"Frame: {Time.frameCount}",
                this
            );
            if (source == null || child == null)
                return;

            // 選択中のパネルからの遷移だけを受け付ける
            if (source != CurrentPanel)
                return;

            // 同じパネルを再度開かない
            if (child == source || panelStack.Contains(child))
                return;

            source.Hide();

            child.Bind(this);
            panelStack.Push(child);
            child.Show();

            SetInitialSelection(child);
            onChildPanelOpened?.Invoke(child);
        }

        public void GoBack()
        {
            if (!IsOpen)
                return;

            // 子パネルなら親パネルへ戻る
            if (panelStack.Count > 1)
            {
                CurrentPanel.Hide();
                panelStack.Pop();

                MenuPanel parent = CurrentPanel;
                parent.Show();

                SetInitialSelection(parent);
                onParentPanelOpened?.Invoke(parent);
                return;
            }

            // RootPanelならメニューを閉じる
            CurrentPanel.Hide();
            panelStack.Pop();

            selectedButton = null;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            menuActionMap?.Disable();
            onMenuClosed?.Invoke();
        }

        private void OnNavigate(InputAction.CallbackContext context)
        {
            if(!isAllowedToControl) return;
            if (!IsOpen)
                return;

            Vector2 direction = context.ReadValue<Vector2>();

            switch (CurrentPanel.Direction)
            {
                case MenuPanel.NavigationDirection.Vertical:
                    if (Mathf.Abs(direction.y) >= 0.5f)
                        MoveSelection(direction.y > 0 ? -1 : 1);
                    break;

                case MenuPanel.NavigationDirection.Horizontal:
                    if (Mathf.Abs(direction.x) >= 0.5f)
                        MoveSelection(direction.x > 0 ? 1 : -1);
                    break;
            }
        }

        private void MoveSelection(int direction)
        {
            MenuPanel panel = CurrentPanel;

            if (panel == null || panel.Buttons.Count == 0)
                return;

            int count = panel.Buttons.Count;
            int currentIndex = -1;

            for (int i = 0; i < panel.Buttons.Count; i++)
            {
                if (panel.Buttons[i] == selectedButton)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                currentIndex = direction > 0 ? -1 : 0;
            }

            // 選択が失われていた場合は先頭から再開する
            if (currentIndex < 0)
            {
                currentIndex = direction > 0 ? -1 : 0;
            }

            for (int i = 0; i < count; i++)
            {
                currentIndex = (currentIndex + direction + count) % count;

                Button candidate = panel.Buttons[currentIndex];

                if (candidate == null ||
                    !candidate.isActiveAndEnabled ||
                    !candidate.interactable)
                    continue;

                SelectButton(candidate);
                return;
            }
        }

        private bool isSubmitting;

        private void OnSubmit(InputAction.CallbackContext context)
        {
            if(!isAllowedToControl) return;
            Debug.Log($"Submit: CurrentPanel={CurrentPanel?.name}, Selected={selectedButton?.name}");
            if (!IsOpen || isSubmitting)
                return;

            EnsureSelectionIsValid();

            if (selectedButton == null ||
                !selectedButton.isActiveAndEnabled ||
                !selectedButton.interactable)
                return;

            isSubmitting = true;

            try
            {
                selectedButton.onClick.Invoke();
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            if(!isAllowedToControl) return;
            if (IsOpen)
                GoBack();
        }

        private void SetInitialSelection(MenuPanel panel)
        {
            selectedButton = null;

            if (panel == null)
                return;

            Button initial = panel.InitialSelectedButton;

            if (IsUsableButton(panel, initial))
            {
                SelectButton(initial);
                return;
            }

            foreach (Button button in panel.Buttons)
            {
                if (IsUsableButton(panel, button))
                {
                    SelectButton(button);
                    return;
                }
            }

            // 選択可能なボタンがない場合
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private bool IsUsableButton(MenuPanel panel, Button button)
        {
            return button != null &&
                    button.isActiveAndEnabled &&
                    button.interactable &&
                    panel.ContainsButton(button);
        }

        private void SelectButton(Button button)
        {
            if (button == null)
                return;

            selectedButton = button;

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != button.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }

            CurrentPanel?.NotifySelectionChanged(button);
        }

        private void EnsureSelectionIsValid()
        {
            MenuPanel panel = CurrentPanel;

            if (panel == null)
                return;

            if (!IsUsableButton(panel, selectedButton))
                SetInitialSelection(panel);

            // EventSystem側の選択が外部UIに移動した場合もメニュー側へ戻す
            if (selectedButton != null &&
                EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != selectedButton.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
            }
        }

        public void AllowControl(bool allow)
        {
            isAllowedToControl = allow;

            if (!allow)
            {
                // メニュー操作を無効化する場合、選択を解除する
                selectedButton = null;

                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                // メニュー操作を有効化する場合、現在のパネルの初期選択に戻す
                SetInitialSelection(CurrentPanel);
            }
        }

        private void Update()
        {
            if (!IsOpen || EventSystem.current == null)
                return;

            GameObject current =
                EventSystem.current.currentSelectedGameObject;

            if (current != null)
            {
                Button button = current.GetComponent<Button>();

                if (IsUsableButton(CurrentPanel, button))
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        CurrentPanel.NotifySelectionChanged(button);
                    }

                    previousEventSystemSelection = current;
                    return;
                }
            }

            // 選択が外れた、またはメニュー外へ移動した場合は
            // MenuManager が管理する選択対象に戻す
            if (IsUsableButton(CurrentPanel, selectedButton))
            {
                if (current != selectedButton.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(
                        selectedButton.gameObject);
                }
            }
            else
            {
                SetInitialSelection(CurrentPanel);
            }

            previousEventSystemSelection =
                EventSystem.current.currentSelectedGameObject;
        }
    }
}