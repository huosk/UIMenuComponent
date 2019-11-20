using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

#pragma warning disable CS0649

namespace UIComponentLib
{
    public static class ExtensionUtil
    {
        public static T GetOrAddComponent<T>(this GameObject g) where T : Component
        {
            T t = g.GetComponent<T>();
            if (t == null)
                t = g.AddComponent<T>();
            return t;
        }

        public static T GetOrAddComponent<T>(this Component c)
            where T : Component
        {
            return c.gameObject.GetOrAddComponent<T>();
        }
    }

    public class UIMenu : Selectable, IPointerClickHandler, ICancelHandler
    {
        public class UIMenuVoidEvent : UnityEvent { }
        public class UIMenuEvent : UnityEvent<int> { }
        public class UIMenuStringEvent : UnityEvent<string> { }

        public class ItemData
        {
            public int cmdId;
            public int groupId;
            public string text;
            public Sprite icon;
            public ItemData parent;
            public List<ItemData> subItems = new List<ItemData>();
        }

        public class UIMenuItem : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public RectTransform rectTransform
            {
                get
                {
                    if (_rectTransform == null)
                        _rectTransform = GetComponent<RectTransform>();
                    return _rectTransform;
                }
            }

            public Toggle toggle
            {
                get
                {
                    if (_toggle == null)
                        _toggle = GetComponent<Toggle>();
                    return _toggle;
                }
            }



            public string lbText
            {
                get
                {
                    return _lbText;
                }
                set
                {
                    _lbText = value;
                    if (text != null) text.text = value;
                    AdjustItemSize();
                }
            }

            public Sprite icon
            {
                get
                {
                    return _icon;
                }
                set
                {
                    _icon = value;
                    if (iconImg != null)
                    {
                        if (_icon != null)
                        {
                            iconImg.sprite = _icon;
                            iconImg.gameObject.SetActive(true);
                        }
                        else
                            iconImg.gameObject.SetActive(false);
                    }
                }
            }

            public Vector2 size
            {
                get
                {
                    return _size;
                }
                set
                {
                    _size = value;
                }
            }

            public ItemData data
            {
                get
                {
                    return _data;
                }
            }

            public GameObject menu
            {
                get
                {
                    return _menu;
                }
                set
                {
                    _menu = value;
                }
            }


            public List<UIMenuItem> subUIItems
            {
                get
                {
                    return _subUIItems;
                }
                set
                {
                    _subUIItems = value;
                }
            }

            public bool isFolder
            {
                get
                {
                    return data.subItems != null && data.subItems.Count > 0;
                }
            }

            UIMenu menuRoot
            {
                get
                {
                    if (_menuRoot == null)
                        _menuRoot = GetComponentInParent<UIMenu>();
                    return _menuRoot;
                }
            }

            Image iconImg
            {
                get
                {
                    if (_iconImg == null)
                    {
                        var go = rectTransform.Find("Icon");
                        if (go != null) _iconImg = go.GetComponent<Image>();
                    }
                    return _iconImg;
                }
            }

            TextMeshProUGUI text
            {
                get
                {
                    if (_text == null)
                        _text = GetComponentInChildren<TextMeshProUGUI>();
                    return _text;
                }
            }

            private RectTransform _rectTransform;
            private Toggle _toggle;
            private TextMeshProUGUI _text;
            private Image _iconImg;
            private Vector2 _size;
            private ItemData _data;
            private GameObject _menu;
            private List<UIMenuItem> _subUIItems = new List<UIMenuItem>();
            private UIMenu _menuRoot;
            private string _lbText;
            private Sprite _icon;


            protected override void Awake()
            {
                base.Awake();
                _size = rectTransform.sizeDelta;
            }

            private void AdjustItemSize()
            {
                float iconWidth = iconImg == null ? 0f : iconImg.GetComponent<RectTransform>().rect.width;
                _size.x = text.preferredWidth + text.rectTransform.offsetMin.x + iconWidth;
            }

            public bool IsParentOf(UIMenuItem childCheck)
            {
                if (childCheck == null)
                    return false;
                var checkData = childCheck.data;
                return data.subItems.Contains(checkData);
            }

            public void BindData(ItemData dat)
            {
                _data = dat;
                lbText = dat.text;
                icon = dat.icon;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                menuRoot.OnFocusMenuItem(this);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
            }

            public void OnSelectSubMenu(UIMenuItem subItem)
            {
                menuRoot.OnSelectMenuItem(subItem);
            }
        }

        public class UIMenuSplitBar : UIBehaviour
        {
            private RectTransform _rectTransform;
            private Vector2 _size;

            public RectTransform rectTransform
            {
                get
                {
                    if (_rectTransform == null)
                        _rectTransform = GetComponent<RectTransform>();
                    return _rectTransform;
                }
            }

            public Vector2 size
            {
                get
                {
                    return _size;
                }
                set
                {
                    _size = value;
                }
            }

            protected override void Awake()
            {
                base.Awake();
                _size = rectTransform.sizeDelta;
            }
        }

        public class SubmenuPair
        {
            public UIMenuItem parentItem;
            public GameObject subMenu;
        }

        [SerializeField]
        private Transform _menuPrototype;


        [SerializeField]
        private GameObject _templete;
        [SerializeField]
        private GameObject _menuItemTemplete;
        [SerializeField]
        private GameObject _splitBarTemplete;

        private List<ItemData> _items = null;
        private GameObject _menuBoard;
        private bool _isShowing;
        private List<UIMenuItem> _currentMenuItems = new List<UIMenuItem>();
        private Canvas _rootCanvas;
        private GameObject _menuBlocker;
        private UIMenuEvent _menuEvent = new UIMenuEvent();
        private UIMenuStringEvent _menuEventStr = new UIMenuStringEvent();
        private RectTransform _rectTransform;
        private Stack<SubmenuPair> _subMenus = new Stack<SubmenuPair>();
        private bool _enableSTRMenuCommand = true;
        private Dictionary<ItemData, UIMenuVoidEvent> _registEvents = new Dictionary<ItemData, UIMenuVoidEvent>();

        public UIMenuEvent onMenuCommand
        {
            get
            {
                return _menuEvent;
            }
        }

        /*
        * onMenuCommandSTR 事件传出的是菜单的路径，比如对于如下菜单：
        * File
        *   - Open
        *   - Save
        * Edit
        *   - Copy
        *   - Paste
        *
        * 当点击Open菜单时，参数的值为 "/File/Open"      
        */
        public UIMenuStringEvent onMenuCommandSTR
        {
            get
            {
                return _menuEventStr;
            }
        }

        public bool enableSTRMenuCommand
        {
            get
            {
                return _enableSTRMenuCommand;
            }
            set
            {
                _enableSTRMenuCommand = value;
            }
        }

        public bool isShowing
        {
            get { return _isShowing; }
            set
            {
                _isShowing = value;
                if (_isShowing) ShowMenu();
                else HideMenu();
            }
        }

        public Canvas rootCanvas
        {
            get
            {
                if (_rootCanvas == null)
                    _rootCanvas = transform.GetComponentInParent<Canvas>();
                return _rootCanvas;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // 初始化菜单数据
            if (_items == null && _menuPrototype != null)
            {
                _items = UIMenuSlot.ConvertToItemTree(_menuPrototype, null);
            }

            //隐藏模板
            if (_templete != null)
                _templete.SetActive(false);
        }

        private UIMenuSplitBar CreateSplitBar(UIMenuSplitBar templ)
        {
            if (templ == null)
                return null;

            GameObject splitBarObj = Instantiate(templ.gameObject);
            UIMenuSplitBar bar = splitBarObj.GetOrAddComponent<UIMenuSplitBar>();
            bar.rectTransform.SetParent(templ.rectTransform.parent, false);
            splitBarObj.SetActive(true);

            return bar;
        }

        //设置模板
        private void SetupTemplete(GameObject templete, GameObject itemTempl, GameObject splitTempl)
        {
            //设置模板
            templete.SetActive(true);
            var item = templete.GetComponent<UIMenuItem>();
            if (itemTempl != null) itemTempl.GetOrAddComponent<UIMenuItem>();
            if (splitTempl != null) splitTempl.GetOrAddComponent<UIMenuSplitBar>();

            var layout = itemTempl.transform.parent.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                int hPadding = layout.padding.left + layout.padding.right;
                int vPadding = layout.padding.top + layout.padding.bottom;
                RectTransform rt = templete.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(hPadding, vPadding);
            }

            Canvas canvas = templete.GetOrAddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31000;
            canvas.GetOrAddComponent<GraphicRaycaster>();
            canvas.GetOrAddComponent<CanvasGroup>();
            templete.SetActive(false);
        }

        private GameObject CreateMenuBoard(GameObject templete, Transform parent)
        {
            GameObject go = Instantiate(templete, parent ?? templete.transform.parent, false);
            go.name = "MenuList";
            go.SetActive(_isShowing);
            return go;
        }

        private UIMenuItem AddMenuItem(ItemData data, UIMenuItem templete, List<UIMenuItem> items)
        {
            GameObject newItemObj = Instantiate<GameObject>(templete.gameObject);
            var newItem = newItemObj.GetOrAddComponent<UIMenuItem>();
            newItem.rectTransform.SetParent(templete.transform.parent, false);
            newItem.gameObject.SetActive(true);
            newItem.BindData(data);

            if (newItem.toggle != null) newItem.toggle.isOn = false;

            if (items != null) items.Add(newItem);

            return newItem;
        }


        private void ShowMenu()
        {
            _isShowing = true;

            _menuBoard = CreateMenu(rectTransform, _items, _currentMenuItems, OnSelectMenuItem);

            if (_menuBoard != null)
            {
                RectTransform menuRT = _menuBoard.GetComponent<RectTransform>();
                RectTransform btnRT = GetComponent<RectTransform>();
                RectTransform rootRT = rootCanvas.GetComponent<RectTransform>();
                AdjustMenuPosition(menuRT, btnRT, rootRT);
            }

            _menuBlocker = CreateBlocker(rootCanvas.transform, _menuBoard);
        }

        private GameObject CreateMenu(RectTransform anchorRT, IList<ItemData> items, List<UIMenuItem> uiItems, UnityAction<UIMenuItem> onSelectItem)
        {
            SetupTemplete(_templete, _menuItemTemplete, _splitBarTemplete);
            var menu = CreateMenuBoard(_templete, anchorRT);

            var itemTemplete = menu.GetComponentInChildren<UIMenuItem>();
            var splitTemplete = menu.GetComponentInChildren<UIMenuSplitBar>();
            float itemHeight = 0;
            float itemMaxWidth = 0;
            uiItems.Clear();

            List<ItemData> sortedItems = items.OrderBy((v1) => v1.groupId).ToList();
            int? lastGroupId = null;
            for (int i = 0; i < sortedItems.Count; i++)
            {
                var itemData = sortedItems[i];
                if (lastGroupId == null)
                    lastGroupId = itemData.groupId;
                else if (lastGroupId != itemData.groupId)
                {
                    lastGroupId = itemData.groupId;
                    if (splitTemplete != null)
                    {
                        var bar = CreateSplitBar(splitTemplete);
                        itemHeight += bar.size.y;
                    }
                }

                var newItem = AddMenuItem(itemData, itemTemplete, uiItems);
                if (newItem != null)
                {
                    newItem.menu = menu;
                    newItem.toggle.onValueChanged.AddListener((isTrue) =>
                    {
                        onSelectItem(newItem);
                    });
                    itemHeight += newItem.size.y;
                    itemMaxWidth = Mathf.Max(newItem.size.x, itemMaxWidth);
                }
            }

            RectTransform menuRT = menu.GetComponent<RectTransform>();
            Vector2 sizeRT = menuRT.sizeDelta;
            sizeRT.y += itemHeight;
            sizeRT.x += itemMaxWidth;
            menuRT.sizeDelta = sizeRT;

            _templete.SetActive(false);
            itemTemplete.gameObject.SetActive(false);
            splitTemplete.gameObject.SetActive(false);

            return menu;
        }

        private void DestroyMenu()
        {
            if (_menuBoard != null)
                Destroy(_menuBoard);
            _menuBoard = null;
        }

        private GameObject CreateBlocker(Transform parent, GameObject menu)
        {
            GameObject blockerObj = new GameObject("Menu Blocker");
            RectTransform rectTransform = blockerObj.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector3.zero;
            rectTransform.anchorMax = Vector3.one;
            rectTransform.sizeDelta = Vector2.zero;
            Canvas canvas = blockerObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            Canvas component = menu.GetComponent<Canvas>();
            canvas.sortingLayerID = component.sortingLayerID;
            canvas.sortingOrder = component.sortingOrder - 1;
            blockerObj.AddComponent<GraphicRaycaster>();
            Image image = blockerObj.AddComponent<Image>();
            image.color = Color.clear;
            Button button = blockerObj.AddComponent<Button>();
            button.onClick.AddListener(() => isShowing = false);
            return blockerObj;
        }

        private void DestroyBlocker()
        {
            if (_menuBlocker != null)
                Destroy(_menuBlocker);
            _menuBlocker = null;
        }

        private Vector2 AdjustMenuPosition(RectTransform menu, RectTransform anchor, RectTransform rootCanvas)
        {
            Vector3[] array = new Vector3[4];
            menu.GetWorldCorners(array);
            Rect rootBound = rootCanvas.rect;

            bool needFlipX = false;
            bool needFlipY = false;

            for (int k = 0; k < 4; k++)
            {
                Vector3 posInRoot = rootCanvas.InverseTransformPoint(array[k]);
                if (!needFlipX && (posInRoot.x < rootBound.min.x || posInRoot.x > rootBound.max.x))
                    needFlipX = true;

                if (!needFlipY && (posInRoot.y < rootBound.min.y || posInRoot.y > rootBound.max.y))
                    needFlipY = true;

                if (needFlipX && needFlipY)
                    break;
            }

            if (needFlipX)
                RectTransformUtility.FlipLayoutOnAxis(menu, 0, false, false);

            if (needFlipY)
                RectTransformUtility.FlipLayoutOnAxis(menu, 1, false, false);

            return new Vector2(needFlipX ? 1 : 0, needFlipY ? 1 : 0);
        }

        private void OnFocusMenuItem(UIMenuItem item)
        {
            EventSystem.current.SetSelectedGameObject(item.gameObject);

            //删除非当前子级
            DestroySubMenu(_subMenus, item);

            GameObject subMenuContainer = CreateSubMenu(_subMenus, item);
            if (subMenuContainer != null)
            {
                RectTransform subMenuBoard = subMenuContainer.transform.GetChild(0) as RectTransform;
                RectTransform containerRT = subMenuContainer.GetComponent<RectTransform>();
                RectTransform rootRT = rootCanvas.GetComponent<RectTransform>();

                // Fix : 当菜单放到其他方向时，子菜单方向不正确问题
                subMenuBoard.pivot = new Vector2(0, 0);
                subMenuBoard.anchorMin = new Vector2(1, 0);
                subMenuBoard.anchorMax = new Vector2(1, 0);
                subMenuBoard.anchoredPosition = Vector3.zero;

                AdjustMenuPosition(subMenuBoard, containerRT, rootRT);
            }
        }

        //检测item的子菜单是否正在显示
        private bool IsSubmenuExist(Stack<SubmenuPair> stack, UIMenuItem item)
        {
            if (stack != null && stack.Count > 0)
            {
                var topPair = stack.Peek();
                if (topPair != null && topPair.parentItem == item)
                    return true;
            }
            return false;
        }

        private GameObject CreateSubMenu(Stack<SubmenuPair> stack, UIMenuItem item)
        {
            if (!item.isFolder)
                return null;

            if (IsSubmenuExist(stack, item))
                return null;

            GameObject submenuContainer = new GameObject("SubMenuContainer");
            RectTransform subcontainerRT = submenuContainer.AddComponent<RectTransform>();
            subcontainerRT.SetParent(item.transform);
            subcontainerRT.anchoredPosition = Vector2.zero;
            subcontainerRT.sizeDelta = item.rectTransform.sizeDelta;
            subcontainerRT.SetParent(item.menu.transform);

            CreateMenu(subcontainerRT, item.data.subItems, item.subUIItems, item.OnSelectSubMenu);

            //save
            stack.Push(new SubmenuPair() { parentItem = item, subMenu = submenuContainer });

            return submenuContainer;
        }

        private void DestroySubMenu(Stack<SubmenuPair> stack, UIMenuItem item)
        {
            while (stack != null && stack.Count > 0)
            {
                var topSubMenu = stack.Peek();
                if (!topSubMenu.parentItem.IsParentOf(item) && topSubMenu.parentItem != item)
                {
                    Destroy(topSubMenu.subMenu);
                    stack.Pop();
                }
                else break;
            }
        }

        private void HideMenu()
        {
            _isShowing = false;

            DestroyMenu();

            DestroyBlocker();

            _subMenus.Clear();
        }

        public void OnCancel(BaseEventData eventData)
        {
            isShowing = false;
        }

        private void OnSelectMenuItem(UIMenuItem item)
        {
            if (!item.toggle.isOn)
                item.toggle.isOn = true;

            if (!item.isFolder)
            {
                onMenuCommand.Invoke(item.data.cmdId);

                // Check and dispatch str command
                if (enableSTRMenuCommand)
                    onMenuCommandSTR.Invoke(GetMenuElementPath(item));

                // Invoke registed events
                InvokeMenuEvent(item.data);

                isShowing = false;
            }
        }

        public void RegisteEvent(string path, UnityAction listener)
        {
            ItemData id = FindChild(path, _items, true);
            if (id == null)
            {
                throw new System.ArgumentException("Can not menu element::" + path);
            }

            UIMenuVoidEvent evt = null;
            if (!_registEvents.TryGetValue(id, out evt))
            {
                evt = new UIMenuVoidEvent();
                _registEvents.Add(id, evt);
            }

            evt.AddListener(listener);
        }

        public void DeregisterEvent(string path, UnityAction listener)
        {
            ItemData id = FindChild(path, _items, true);
            if (id == null)
                return;

            UIMenuVoidEvent evt = null;
            if (_registEvents.TryGetValue(id, out evt) && evt != null)
                evt.RemoveListener(listener);
        }

        private void InvokeMenuEvent(ItemData id)
        {
            UIMenuVoidEvent evt = null;
            if (_registEvents.TryGetValue(id, out evt) && evt != null)
                evt.Invoke();
        }

        private static string GetMenuElementPath(UIMenuItem item)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            ItemData c = item.data;
            do
            {
                sb.Insert(0, c.text);

                c = c.parent;
                sb.Insert(0, "/");
            }
            while (c != null);

            return sb.ToString();
        }

        public ItemData FindChild(string text, IList<ItemData> items, bool recursive)
        {
            if (string.IsNullOrEmpty(text) || items == null)
                return null;

            if (!recursive)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    if (items[i] == null || string.Compare(text, items[i].text, StringComparison.Ordinal) != 0)
                        continue;

                    return items[i];
                }

                return null;
            }
            else
            {
                string[] pathSegs = text.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                ItemData c = null;
                for (int i = 0; i < pathSegs.Length; ++i)
                {
                    if (c == null)
                        c = FindChild(pathSegs[i], items, false);
                    else
                        c = FindChild(pathSegs[i], c.subItems, false);

                    if (c == null)
                        break;
                }
                return c;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            isShowing = !isShowing;
        }
    }
}
