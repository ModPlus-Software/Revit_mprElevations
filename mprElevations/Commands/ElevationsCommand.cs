namespace mprElevations.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Services;
    using Utility;
    using View;
    using ViewModels;

    /// <summary>
    /// Команда создания высотных отметок по текущей конфигурации
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class ElevationsCommand : IExternalCommand
    {
        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
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
                var elementList = GetElements(uiDoc);
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
                    .Where(i => selectedCategoryIdList.Contains(i.Category.Id))
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
        /// Получить выбранные элементы
        /// </summary>
        /// <param name="uiDoc">Документ</param>
        private List<Element> GetElements(UIDocument uiDoc)
        {
            var multiClassFilter = new ElementMulticlassFilter(new List<Type>
            {
                typeof(FamilyInstance),
                typeof(Wall),
                typeof(Floor)
            });

            var sel = uiDoc.Selection
                .GetElementIds()
                .Select(i => uiDoc.Document.GetElement(i))
                .SelectMany(x => (x is Group g) 
                    ? g.GetDependentElements(multiClassFilter).Select(e => uiDoc.Document.GetElement(e))
                    : new List<Element> { x })
                .Where(e => e.Category != null && e.get_Geometry(new Options()) != null)
                .ToList();

            while (!sel.Any())
            {
                sel = uiDoc.Selection.PickElementsByRectangle(new SelectionFilter()).ToList();

                if (!sel.Any())
                {
                    // Не выбрано элементов. Для продолжения работы необходимо выбрать элементы
                    MessageBox.Show(Language.GetItem("h2"), MessageBoxIcon.Alert);
                }
            }

            return sel;
        }

        /// <summary>
        /// Получить категории из элементов
        /// </summary>
        /// <param name="elementsList">Список элементов</param>
        private List<CategoryModel> GetCategories(List<Element> elementsList)
        {
            var categoryModelList = new HashSet<CategoryModel>();

            foreach (var el in elementsList.Select(e => new CategoryModel(e.Category)))
            {
                categoryModelList.Add(el);
            }

            return categoryModelList.ToList();
        }
    }
}
