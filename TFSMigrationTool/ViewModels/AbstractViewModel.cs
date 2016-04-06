using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TFSMigrationTool.ViewModels
{
    public abstract class AbstractViewModel:INotifyPropertyChanged
    {
        protected abstract string ViewName { get; }
        public Window View { get; private set; }
        public AbstractViewModel()
        {
            View = ServiceLocator.Resolve<Window>(ViewName);
            View.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Close()
        {
            View.Close();
        }

        public void Show()
        {
            View.Show();
        }
        public void ShowDialog()
        {
            View.ShowDialog();
        }
    }
}
