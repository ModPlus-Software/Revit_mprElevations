namespace mprElevations.ViewModels;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Models;
using ModPlusAPI;
using ModPlusAPI.Mvvm;

/// <summary>
/// Контекст окна настроек (конфигураций)
/// </summary>
public class MainContext : ObservableObject
{
    private bool _isEnabledContinue;

    /// <summary>
    /// Конструктор класса
    /// </summary>
    /// <param name="categoryModels">Лист с категориями</param>
    public MainContext(List<CategoryModel> categoryModels)
    {
        CategoryModelList = categoryModels;
        foreach (var categoryModel in CategoryModelList)
        {
            categoryModel.PropertyChanged += (sender, args) =>
            {
                IsEnabledContinue = CategoryModelList.Any(c => c.IsChoose);
            };
        }

        var savedCategories = UserConfigFile.GetValue(ModPlusConnector.Instance.Name, "Categories");
        foreach (var s in savedCategories.Split(';'))
        {
            if (int.TryParse(s, out var i))
            {
                var categoryModel = CategoryModelList.FirstOrDefault(c => c.ElementCategory.Id.IntegerValue == i);
                if (categoryModel != null) 
                    categoryModel.IsChoose = true;
            }
        }
    }

    /// <summary>
    /// Лист с модельками
    /// </summary>
    public List<CategoryModel> CategoryModelList { get; }
        
    /// <summary>
    /// Доступность кнопки "Продолжить"
    /// </summary>
    public bool IsEnabledContinue
    {
        get => _isEnabledContinue;
        set
        {
            if (_isEnabledContinue == value)
                return;
            _isEnabledContinue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Выбрать все
    /// </summary>
    public ICommand SelectAllCommand =>
        new RelayCommandWithoutParameter(() => CategoryModelList.ForEach(c => c.IsChoose = true));
        
    /// <summary>
    /// Снять выбор
    /// </summary>
    public ICommand UnselectAllCommand =>
        new RelayCommandWithoutParameter(() => CategoryModelList.ForEach(c => c.IsChoose = false));

    /// <summary>
    /// On window closing
    /// </summary>
    public ICommand OnClosingCommand => new RelayCommandWithoutParameter(() =>
    {
        UserConfigFile.SetValue(
            ModPlusConnector.Instance.Name,
            "Categories",
            string.Join(";", CategoryModelList.Where(c => c.IsChoose).Select(c => c.ElementCategory.Id.IntegerValue)),
            true);
    });
}