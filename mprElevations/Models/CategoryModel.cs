namespace mprElevations.Models
{
    using System;
    using Autodesk.Revit.DB;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Модель представления категории
    /// </summary>
    public class CategoryModel : ObservableObject, IEquatable<CategoryModel>
    {
        private bool _isChoose;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryModel"/> class.
        /// </summary>
        /// <param name="elementCategory">Категория элементов</param>
        public CategoryModel(Category elementCategory)
        {
            Name = elementCategory.Name;
            ElementCategory = elementCategory;
        }

        /// <summary>
        /// Текстовое представление имени категории
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Категория элементов
        /// </summary>
        public Category ElementCategory { get; }

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

        /// <inheritdoc/>
        public bool Equals(CategoryModel other)
        {
            if (ReferenceEquals(null, other)) 
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }
}
