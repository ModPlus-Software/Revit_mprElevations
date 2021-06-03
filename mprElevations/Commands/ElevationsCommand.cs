namespace mprElevations.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using mprElevations.Models;
    using mprElevations.Services;
    using mprElevations.View;
    using ViewModels;

    /// <summary>
    /// Команда создания высотных отметок по текущей конфигурации
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class ElevationsCommand : IExternalCommand
    {
        private const string LangItem = "mprElevations";

        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument;
            var activeView = doc.ActiveView;
            var verticalDirection = new XYZ(0, 0, 1);
            if (!activeView.UpDirection.CrossProduct(verticalDirection).IsAlmostEqualTo(XYZ.Zero))
            {
                MessageBox.Show("Данный вид не является разрезом или фасадом");
                return Result.Failed;
            }

#if DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance);
#endif
            //// TODO Тут надо получить конфигурацию и сообщить если ее нет. Если есть - выполнить построение отметок

            try
            {
                var elementList = GetElements(doc);
                var categoryList = GetCategories(elementList);
                Statistic.SendCommandStarting(ModPlusConnector.Instance);

                var mainContext = new MainContext(categoryList);
                var settingsWindow = new SettingsWindow()
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

                var service = new ElevationCreationService(doc);

                service.DoWork(selectedElements);

            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception e)
            {
                ExceptionBox.Show(e);
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Получить выбранные элементы
        /// </summary>
        /// <param name="uidoc">Документ</param>
        /// <returns></returns>
        private List<Element> GetElements(UIDocument uidoc)
        {
            // Проверяем есть ли выбранные элементы
            var sel = uidoc.Selection
                .GetElementIds()
                .Select(i => uidoc.Document.GetElement(i))
                .ToList();

            while (!sel.Any())
            {
                sel = uidoc
                        .Selection
                        .PickObjects(ObjectType.Element, "Выберите элементы")
                        .Select(i => uidoc.Document.GetElement(i))
                        .ToList();

                if (!sel.Any())
                {
                    MessageBox.Show("Не выбранно элементов, для продолжения работы необходимо выбрать элементы");
                }
            }

            return sel;
        }

        /// <summary>
        /// Получиь категории из элементов
        /// </summary>
        /// <param name="elementsList">Список элементов</param>
        /// <returns></returns>
        private List<CategoryModel> GetCategories(List<Element> elementsList)
        {
            var categoryModelList = new List<CategoryModel>();
            var categoryLIst = new List<string>();

            foreach (var el in elementsList)
            {
                if (!categoryLIst.Contains(el.Category.Name))
                {
                    categoryLIst.Add(el.Category.Name);
                    categoryModelList.Add(new CategoryModel()
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
