using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace InifinityList
{
    public enum LayoutStyle { UpToBottom, LeftToRight }
    [RequireComponent(typeof(ScrollRect))]
    public class InfinityList : MonoBehaviour
    {
        [SerializeField]
        private ScrollRect ScrollRect;
        private RectTransform Content => ScrollRect.content;
        private RectTransform Viewport => ScrollRect.viewport;
        [SerializeField]
        private GridLayoutGroup GridLayoutGroup;
        public RectOffset Padding;
        public Vector2 ItemSize;
        public Vector2 ItemSpace;
        public bool Horizontal = true;
        public bool Vertical = true;
        public bool HorizontalBarEnable = true;
        public bool VerticalBarEnable = true;
        [Tooltip("UpToBottom：根据Column计算 LeftToRight：根据Row计算")]
        public LayoutStyle Style;
        [Tooltip("列数")]
        public int ColumnCount = 1;
        [Tooltip("行数")]
        public int RowCount = 1;
        public int PageMaxCnt => Style == LayoutStyle.UpToBottom ? RowCount : ColumnCount;
        [SerializeField]
        private InfinityListItem ItemPrefab;
        private List<InfinityListItem> Items = new List<InfinityListItem>();
        private List<InfinityListItem> pool = new List<InfinityListItem>();
        private InfinityListItem GetItem()
        {
            if (pool.Count == 0) return null;
            var ret = pool.Last();
            pool.Remove(ret);
            ret.gameObject.SetActive(true);
            return ret;
        }
        private void RecycleItem(InfinityListItem item)
        {
            if (Items.Contains(item))
            {
                pool.Add(item);
                Items.Remove(item);
                item.gameObject.SetActive(false);
            }
        }
        private void RefreshItemData(InfinityListItem item)
        {
            int index = 0;
            RectTransform rect = (RectTransform)item.transform;
            if (Style == LayoutStyle.UpToBottom)
            {
                index = Mathf.RoundToInt((-rect.anchoredPosition.y - Padding.top) / (ItemSize.y + ItemSpace.y));
            }
            else if (Style == LayoutStyle.LeftToRight)
            {
                index = Mathf.RoundToInt((rect.anchoredPosition.x - Padding.left) / (ItemSize.x + ItemSpace.x));
            }
            //Debug.Log(index);
            if (index >= 0 && index < m_Data.Count)
                item.Data = m_Data[index];
        }
        public IList dataProvider
        {
            get
            {
                return m_Data;
            }
            set
            {
                m_Data = value;
                DataCnt = 0;
                foreach (var data in m_Data)
                    DataCnt++;
                CalculateContentSize();
                FirstCalculateItemPos();
                RefreshData();
            }
        }
        private IList m_Data;
        private int DataCnt;
        private void Awake()
        {
            RefreshItemPreview();
            FirstCalculateItemPos();
            ScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
        private Vector2 LastScrolVal;
        private Vector2 moveDelta;
        private void OnScrollValueChanged(Vector2 vector2)
        {
            moveDelta = vector2 - LastScrolVal;
            LastScrolVal = vector2;
            UpdateItems();
        }
        private void UpdateItems()
        {
            //向下或向右显示,回收前面的Item，添加新Item到队尾
            if (moveDelta.y < 0 || moveDelta.x > 0)
            {
                RecycleItem();
                if (Items.Count < PageMaxCnt)
                {
                    int toAddTimes = PageMaxCnt - Items.Count;
                    for (int i = 0; i < toAddTimes; i++)
                        AddItemToTail();
                    RefreshData();
                }
            }
            else if (moveDelta.y > 0 || moveDelta.x < 0)
            {
                RecycleItem();
                if (Items.Count < PageMaxCnt)
                {
                    int toAddTimes = PageMaxCnt - Items.Count;
                    for (int i = 0; i < toAddTimes; i++)
                        AddItemToHead();
                    RefreshData();
                }
            }

        }
        private void RecycleItem()
        {
            List<InfinityListItem> toRecycle = new List<InfinityListItem>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].gameObject.activeInHierarchy) continue;
                var itemPos = ((RectTransform)Items[i].transform).anchoredPosition;
                var contentPos = Content.anchoredPosition;
                if (itemPos.x + contentPos.x < -ItemSize.x
                    || itemPos.x + contentPos.x > Viewport.rect.width
                    || itemPos.y + contentPos.y > ItemSize.y
                    || itemPos.y + contentPos.y < -Viewport.rect.height)
                {
                    toRecycle.Add(Items[i]);
                }
            }
            foreach (var item in toRecycle)
                RecycleItem(item);
        }
        private void AddItemToTail()
        {
            var top = GetItem();
            if (top == null) return;
            if (Style == LayoutStyle.UpToBottom)
            {
                ((RectTransform)top.transform).anchoredPosition = ((RectTransform)Items.Last().transform).anchoredPosition
                + new Vector2(0, -(ItemSize.y + ItemSpace.y));
            }
            else if (Style == LayoutStyle.LeftToRight)
            {
                ((RectTransform)top.transform).anchoredPosition = ((RectTransform)Items.Last().transform).anchoredPosition
                + new Vector2((ItemSize.x + ItemSpace.x), 0);
            }
            Items.Add(top);
        }

        private void AddItemToHead()
        {
            var top = GetItem();
            if (top == null) return;
            if (Style == LayoutStyle.UpToBottom)
            {
                ((RectTransform)top.transform).anchoredPosition = ((RectTransform)Items.First().transform).anchoredPosition
                - new Vector2(0, -(ItemSize.y + ItemSpace.y));
            }
            else if (Style == LayoutStyle.LeftToRight)
            {
                ((RectTransform)top.transform).anchoredPosition = ((RectTransform)Items.First().transform).anchoredPosition
                - new Vector2((ItemSize.x + ItemSpace.x), 0);
            }
            Items.Insert(0, (top));
        }
        public void RefreshData()
        {
            foreach (var item in Items)
                RefreshItemData(item);
        }

        #region ScrolBar
#if UNITY_EDITOR
        private void OnValidate()
        {

            RefreshScrollDirect();
            RefreshScrollBar();
        }
#endif
        private void RefreshScrollDirect()
        {
            ScrollRect.horizontal = Horizontal;
            ScrollRect.vertical = Vertical;
        }
        private void RefreshScrollBar()
        {
            var bars = ScrollRect.transform.GetComponentsInChildren<Scrollbar>(true);
            var horizontalBar = bars.First(bar => bar.direction == Scrollbar.Direction.LeftToRight || bar.direction == Scrollbar.Direction.RightToLeft);
            var verticalBar = bars.First(bar => bar.direction == Scrollbar.Direction.TopToBottom || bar.direction == Scrollbar.Direction.BottomToTop);
            //更新水平拖动条
            if (HorizontalBarEnable)
            {
                horizontalBar.gameObject.SetActive(true);
                ScrollRect.horizontalScrollbar = horizontalBar;
            }
            else
            {
                horizontalBar.gameObject.SetActive(false);
                ScrollRect.horizontalScrollbar = null;
                ScrollRect.viewport.sizeDelta = new Vector2(0, ScrollRect.viewport.sizeDelta.y);
            }
            //更新垂直拖动条
            if (VerticalBarEnable)
            {
                verticalBar.gameObject.SetActive(true);
                ScrollRect.verticalScrollbar = verticalBar;
            }
            else
            {
                verticalBar.gameObject.SetActive(false);
                ScrollRect.verticalScrollbar = null;
                ScrollRect.viewport.sizeDelta = new Vector2(ScrollRect.viewport.sizeDelta.x, 0);
            }
        }
        #endregion

        private void RefreshItemPreview()
        {
            if (ItemPrefab == null) return;
            while (Content.childCount > 0)
            {
                DestroyImmediate(Content.GetChild(0).gameObject);
            }
            for (int i = 0; i <= PageMaxCnt; i++)
            {
                Instantiate(ItemPrefab, Content);
            }
            Items.Clear();
            for (int i = 0; i < Content.childCount; i++)
            {
                Items.Add(Content.GetChild(i).GetComponent<InfinityListItem>());
            }
        }
        /// <summary>
        /// 根据数据数量计算Content大小
        /// </summary>
        private void CalculateContentSize()
        {
            int horizontalCnt = 1;
            int verticalCnt = 1;
            if (Style == LayoutStyle.UpToBottom)
                verticalCnt = DataCnt;
            else if (Style == LayoutStyle.LeftToRight)
                horizontalCnt = DataCnt;
            var ContentWidth = (Padding.left + Padding.right)
                + horizontalCnt * (ItemSize.x + ItemSpace.x);
            var ContentHeight = (Padding.top + Padding.bottom)
                + verticalCnt * (ItemSize.y + ItemSpace.y);
            Content.sizeDelta = new Vector2(ContentWidth, ContentHeight);
        }
        private void FirstCalculateItemPos()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var rect = (RectTransform)Items[i].transform;
                rect.sizeDelta = ItemSize;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                int currentRow = 0;
                int currentCol = 0;
                if (Style == LayoutStyle.UpToBottom)
                {
                    currentRow = 0;
                    currentCol = i % RowCount;
                }
                else if (Style == LayoutStyle.LeftToRight)
                {
                    currentRow = i % ColumnCount;
                    currentCol = 0;
                }
                ((RectTransform)Items[i].transform).anchoredPosition = new Vector2(Padding.left, -Padding.top)
                     + new Vector2(currentRow * (ItemSize.x + ItemSpace.x), -currentCol * (ItemSize.y + ItemSpace.y));
            }
            RecycleItem();
        }
    }
}