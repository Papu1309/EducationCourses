using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace EducationCourses.Connect
{
    // Partial класс для добавления дополнительных свойств к сущности Courses
    public partial class Courses : INotifyPropertyChanged
    {
        private bool _canEnroll;

        public bool CanEnroll
        {
            get { return _canEnroll; }
            set
            {
                _canEnroll = value;
                OnPropertyChanged("CanEnroll");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
