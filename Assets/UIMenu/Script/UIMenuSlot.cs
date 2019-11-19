using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIComponentLib
{
    public class UIMenuSlot : MonoBehaviour
    {
        public enum SlotType
        {
            MenuElement,
            Spliter,
        }

        public int cmdId;
        public SlotType type = SlotType.MenuElement;
        public string text;
        public Sprite icon;

        public static List<UIMenu.ItemData> ConvertToItemTree(Transform g,UIMenu.ItemData parent)
        {
            List<UIMenu.ItemData> result = new List<UIMenu.ItemData>();
            int groupId = 0;
            for (int i = 0; i < g.childCount; ++i)
            {
                Transform childRT = g.GetChild(i);
                var child = childRT.GetComponent<UIMenuSlot>();
                if (child == null)
                    continue;

                if (child.type == SlotType.Spliter)
                {
                    groupId++;
                    continue;
                }

                var data = new UIMenu.ItemData()
                {
                    cmdId = child.cmdId,
                    groupId = groupId,
                    text = child.text,
                    icon = child.icon,
                    parent = parent,
                };

                data.subItems = ConvertToItemTree(childRT,data);

                result.Add(data);
            }

            return result;
        }
    }
}
