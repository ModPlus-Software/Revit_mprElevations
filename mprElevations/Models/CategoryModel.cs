namespace mprElevations.Models
{
    using Autodesk.Revit.DB;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Модель представления категории
    /// </summary>
    public class CategoryModel : VmBase
    {

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
                OnPropertyChanged();
            }
        }

        private bool _isChoose;
    }
}
