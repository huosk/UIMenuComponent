using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenuSample : MonoBehaviour
{
    public UIComponentLib.UIMenu menu;

    // Start is called before the first frame update
    void Start()
    {
        menu.RegisteEvent("/Edit File/Cut", () =>
        {
            Debug.Log("点击了 /Edit File/Cut 菜单项");
        });

        menu.RegisteEvent("/Save File", () =>
        {
            Debug.Log("点击了 /Save File 菜单项");
        });

        menu.onMenuCommand.AddListener((int cmdId) =>
        {
            Debug.Log("点击了菜单项，命令ID::" + cmdId);
        });

        menu.enableSTRMenuCommand = true;
        menu.onMenuCommandSTR.AddListener((string cmd) =>
        {
            Debug.Log("点击了菜单项，菜单路径::" + cmd);
        });
    }
}
