using System;
using System.Collections.Generic;
using System.Text;

namespace WarframeDropTableParser
{
    public class WarframeDropTable
    {
        private String _SourceName;
        public String SourceName
        {
            get
            {
                return _SourceName;
            }
            set
            {
               _SourceName =value;
            }
        }

        private String _SourceSubName;
        public String SourceSubName
        {
            get
            {
                return _SourceSubName;
            }
            set
            {
                _SourceSubName = value;
            }
        }

        public Boolean ShowSubName
        {
            get
            {
                return !String.IsNullOrEmpty(_SourceSubName);
            }
        }


        private List<DropTableItem> _DropTableRewards;
        public List<DropTableItem> DropTableRewards
        {
            get
            {
                return _DropTableRewards;
            }
            set
            {
                _DropTableRewards= value;
            }
        }

        public WarframeDropTable()
        {
            SourceName = String.Empty;
            DropTableRewards = new List<DropTableItem>();
        }
    }

    public class DropTableItem
    {
        public string DropTableItemName { get; set; }
        public string DropTableItemChance { get; set; }
    }



}
