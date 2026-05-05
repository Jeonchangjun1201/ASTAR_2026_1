using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared.DataTable
{
    public abstract class DataTable<TRow> : IEnumerable<TRow> where TRow : DataTableRow
    {
        [SerializeField]
        private List<TRow> tableRowList = null;
        private readonly Dictionary<int, TRow> tableRows = null;

        public DataTable()
        {
            tableRowList = new List<TRow>();
            tableRows = new Dictionary<int, TRow>();
        }

        protected virtual void OnInitialize() { }
        public void Initialize()
        {
            int count = tableRowList.Count;
            for (int i = 0; i < count; ++i)
            {
                TRow tableRow = tableRowList[i];
                tableRow.id = i;
                tableRows[tableRow.id] = tableRow;
            }

            OnInitialize();
        }

        public TRow GetRow(int id)
        {
            tableRows.TryGetValue(id, out TRow tableRow);
            return tableRow;
        }

        IEnumerator<TRow> IEnumerable<TRow>.GetEnumerator() => tableRows.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => tableRows.Values.GetEnumerator();
    }
}

