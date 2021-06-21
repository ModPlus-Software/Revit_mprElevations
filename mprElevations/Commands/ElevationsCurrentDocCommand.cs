namespace mprElevations.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Utility;

    /// <summary>
    /// Команда создания высотных отметок по текущей конфигурации
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class ElevationsCurrentDocCommand : IExternalCommand
    {
        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return BaseElevationsCommand.Execute(commandData, () => GetElements(commandData.Application.ActiveUIDocument));
        }

        /// <summary>
        /// Получить выбранные элементы
        /// </summary>
        /// <param name="uiDoc">Документ</param>
        private List<ElementModel> GetElements(UIDocument uiDoc)
        {
            var sel = new List<ElementModel>();

            while (!sel.Any())
            {
                // Выберите элементы из текущего файла
                sel = uiDoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter(), Language.GetItem("h9"))
                    .Where(i => i != null)
                    .Select(i => new ElementModel(i, uiDoc.Document))
                    .ToList();

                if (!sel.Any())
                {
                    // Не выбрано элементов. Для продолжения работы необходимо выбрать элементы
                    MessageBox.Show(Language.GetItem("h2"), MessageBoxIcon.Alert);
                }
            }

            return sel;
        }
    }
}
