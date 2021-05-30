namespace mprElevations.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using Autodesk.Revit.UI;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Mvvm;
    using mprElevations.Models;
    using Services;

    /// <summary>
    /// Контекст окна настроек (конфигураций)
    /// </summary>
    public class MainContext : VmBase
    {
        private ElevationByLine _elevationByLine;
        private UIApplication _uiapp;
        
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="app">Приложение</param>
        public MainContext(UIApplication app)
        {
            _uiapp = app;
            _elevationByLine = new ElevationByLine(_uiapp);
            CategoryModelList = _elevationByLine.CategoryModels;
        }

        /// <summary>
        /// Лист с модельками
        /// </summary>
        public ObservableCollection<CategoryModel> CategoryModelList { get; set; }

        /// <summary>
        /// Принятия настроек
        /// </summary>
        public ICommand ApplyCommand => new RelayCommand<IClosable>(Apply);

        /// <summary>
        /// Комманда закрытия первого окна
        /// </summary>
        public ICommand CancelCommand => new RelayCommand<IClosable>(win => win.Close());

        public ICommand SelectAll => new RelayCommand<IClosable>(SellectAllCheckBox);

        /// <summary>
        /// Метод для реализации галочки "Выбрать все"
        /// </summary>
        /// <param name="win"></param>
        private void SellectAllCheckBox(IClosable win)
        {
            if (CategoryModelList.Count > 0)
            {
                var propertyOfFirstElement = CategoryModelList[0].IsChoose;
                var copyCategryList = new ObservableCollection<CategoryModel>();
                foreach (CategoryModel item in CategoryModelList)
                {
                    copyCategryList.Add((CategoryModel)item.Clone());
                }

                CategoryModelList.Clear();
                foreach (var element in copyCategryList)
                {
                    element.IsChoose = propertyOfFirstElement == true ? false : true;
                    CategoryModelList.Add(element);
                }
            }
        }

        private void Apply(IClosable win)
        {
            win.Close();
            try
            {
                _elevationByLine.DoWork(CategoryModelList);
            }
            catch (Exception e)
            {
                TaskDialog.Show("1", e.ToString());
            }
        }
    }
}
