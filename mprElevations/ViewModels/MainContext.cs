namespace mprElevations.ViewModels
{
    using System.Collections.Generic;
    using Models;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Контекст окна настроек (конфигураций)
    /// </summary>
    public class MainContext : VmBase
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="categoryModels">Лист с категориями</param>
        public MainContext(List<CategoryModel> categoryModels)
        {
            CategoryModelList = categoryModels;
        }

        /// <summary>
        /// Лист с модельками
        /// </summary>
        public List<CategoryModel> CategoryModelList { get; }
    }
}
