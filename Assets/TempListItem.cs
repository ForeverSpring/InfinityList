using InifinityList;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TempListItem : InfinityListItem
{
    [SerializeField]
    private Text TextContent;
    protected override void OnDataUpdated(object data)
    {
        base.OnDataUpdated(data);
        int Data = (int)data;
        TextContent.text = Data.ToString();
        gameObject.name = TextContent.text;
    }
}
