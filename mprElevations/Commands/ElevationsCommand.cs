namespace mprElevations.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using Models;
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
            var doc = commandData.Application.ActiveUIDocument;
            var activeView = doc.ActiveView;
            
            if (activeView.ViewType != ViewType.Section && activeView.ViewType != ViewType.Elevation)
            {
                MessageBox.Show("Данный вид не является разрезом или фасадом");
                return Result.Failed;
            }

#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance);
#endif
            try
            {
                var elementList = GetElements(doc);
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

                new ElevationCreationService(doc).DoWork(selectedElements);
                
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
                .ToList();

            while (!sel.Any())
            {
                sel = uiDoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter())
                    .Select(i => uiDoc.Document.GetElement(i.ElementId))
                    .ToList();

                if (!sel.Any())
                {
                    MessageBox.Show("Не выбрано элементов, для продолжения работы необходимо выбрать элементы", MessageBoxIcon.Alert);
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
            var categoryModelList = new List<CategoryModel>();
            var categoryLIst = new List<string>();

            foreach (var el in elementsList)
            {
                if (el.Category != null && !categoryLIst.Contains(el.Category.Name))
                {
                    categoryLIst.Add(el.Category.Name);
                    categoryModelList.Add(new CategoryModel
                    {
                        Name = el.Category.Name,
                        ElementCategory = el.Category,
                        IsChoose = false
                    });
                }
            }

            return categoryModelList;
        }
    }
}
