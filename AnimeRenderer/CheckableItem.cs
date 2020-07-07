using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AnimeRenderer
{
    public class CheckableItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<CheckableItem> children = new ObservableCollection<CheckableItem>();
        private bool isChecked = true;
        private string value;
        private CheckableItem parent;
        public ImageSource ico;
        public string Value { get => value; set => this.value = value; }
        public ObservableCollection<CheckableItem> Children { get => children; set => children = value; }
        private string path;
        private bool isFolder;
        private SolidColorBrush textColor;
        public bool IsChecked
        {
            get { return isChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        public CheckableItem Parent { get => parent; set => parent = value; }
        public bool IsFolder { get => isFolder; set => isFolder = value; }
        public SolidColorBrush TextColor { get => textColor; set => textColor = value; }
        public string Path { get => path; set => path = value; }

        void SetIsChecked(bool value, bool updateChildren, bool updateParent)
        {
            if (value == isChecked)
                return;

            isChecked = value;

            if (updateChildren)
                this.Children.ToList().ForEach(c => c.SetIsChecked(isChecked, true, false));

            if (updateParent && parent != null)
                parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool state = true;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool current = this.Children[i].IsChecked;
                if (state != current)
                {
                    state = false;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }

        void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
