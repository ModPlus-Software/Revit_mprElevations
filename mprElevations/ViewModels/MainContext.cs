namespace mprElevations.ViewModels
{
    using System.Collections.Generic;
    using ModPlusAPI.Mvvm;
    using mprElevations.Models;

    /// <summary>
    /// Контекст окна настроек (конфигураций)
    /// </summary>
    public class MainContext : VmBase
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="categoryModelslist">Лист с категориями</param>
        public MainContext(List<CategoryModel> categoryModelslist)
        {
            CategoryModelList = categoryModelslist;
        }

        /// <summary>
        /// Лист с модельками
        /// </summary>
        public List<CategoryModel> CategoryModelList { get; }

    }
}
