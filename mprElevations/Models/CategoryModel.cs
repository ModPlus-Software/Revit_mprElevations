namespace mprElevations.Models
{
    using System;
    using System.ComponentModel;
    using Autodesk.Revit.DB;

    /// <summary>
    /// Модель представления категории
    /// </summary>
    public class CategoryModel : ICloneable
    {
        /// <summary>
        /// Вызов события уведомления
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isChoose { get; set; }

        /// <summary>
        /// Текстовое представление имени категории
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Категория элементов
        /// </summary>
        public Category ElementCategory { get; set; }

        /// <summary>
        /// Выбор категории
        /// </summary>
        public bool IsChoose
        {
            get => _isChoose;
            set
            {
                _isChoose = value;
                NotifyPropertyChanged();
            }
        }

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return new CategoryModel()
            {
                IsChoose = _isChoose,
                ElementCategory = ElementCategory,
                Name = Name
            };
        }
    }
}
