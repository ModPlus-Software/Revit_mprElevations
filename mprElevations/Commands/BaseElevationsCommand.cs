namespace mprElevations.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Services;
    using View;
    using ViewModels;

    /// <summary>
    /// Базовый класс команд
    /// </summary>
    public class BaseElevationsCommand 
    {
        /// <summary>
        /// Исполнительная комманда
        /// </summary>
        /// <param name="commandData">commandData</param>
        /// <param name="getElementFunction">Функция получения элементов</param>
        /// <returns></returns>
        public static Result Execute(ExternalCommandData commandData, Func<List<ElementModel>> getElementFunction)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var activeView = uiDoc.ActiveGraphicalView;

            if (activeView.ViewType != ViewType.Section && activeView.ViewType != ViewType.Elevation)
            {
                // Текущий вид не является разрезом или фасадом
                MessageBox.Show(Language.GetItem("h1"), MessageBoxIcon.Close);
                return Result.Failed;
            }

#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance);
#endif
            try
            {
                var elementList = getElementFunction.Invoke();
                var categoryList = GetCategories(elementList);

                var mainContext = new MainContext(categoryList);
                var settingsWindow = new SettingsWindow
                {
                    DataContext = mainContext
                };
                if (settingsWindow.ShowDialog() != true)
                    return Result.Cancelled;

                var selectedCategoryIdList = mainContext
                    .CategoryModelList
                    .Where(i => i.IsChoose)
                    .Select(i => i.ElementCategory.Id)
                    .ToList();
                var selectedElements = elementList
                    .Where(i => selectedCategoryIdList.Contains(i.Elem.Category.Id))
                    .ToList();

                new ElevationCreationService(uiDoc).DoWork(selectedElements);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }

        /// <summary>
        /// Получить категории из элементов
        /// </summary>
        /// <param name="elementsList">Список элементов</param>
        private static List<CategoryModel> GetCategories(List<ElementModel> elementsList)
        {
            var categoryModelList = new HashSet<CategoryModel>();

            foreach (var el in elementsList.Select(e => new CategoryModel(e.Elem.Category)))
            {
                categoryModelList.Add(el);
            }

            return categoryModelList.ToList();
        }
    }
}
