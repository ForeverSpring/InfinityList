using InifinityList;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    [SerializeField]
    private InfinityList HorizontalList;
    [SerializeField]
    private InfinityList VerticalList;
    void Start()
    {
        HorizontalList.dataProvider = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, };
        VerticalList.dataProvider = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
