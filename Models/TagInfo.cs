using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrendChartApp.Models
{
    /// <summary>
    /// Model class for tag information
    /// </summary>
    public class TagInfo : INotifyPropertyChanged
    {
        public int Index { get; set; }            // 在資料表中的索引
        public int GroupNo { get; set; }          // 群組編號
        public string GroupName { get; set; }     // 群組名稱
        public string TagNo { get; set; }         // 標籤編號
        public string TagName { get; set; }       // 標籤名稱
        public int TableNo { get; set; }          // 對應的資料表編號
        public int ItemPos { get; set; }          // 對應的欄位位置

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}