using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InifinityList
{
    public class InfinityListItem : MonoBehaviour
    {
        public object Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                if (value.Equals(m_Data))
                    return;
                m_Data = value;
                OnDataUpdated(m_Data);
            }
        }
        private object m_Data;

        protected virtual void OnDataUpdated(object data) { }
    }
}