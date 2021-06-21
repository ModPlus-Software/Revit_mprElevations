namespace mprElevations.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using mprElevations.Models;
    using mprElevations.Utility;

    /// <summary>
    /// Команда создания высотных отметок по на связанные файлы
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class ElevationsLinkedDocCommand : IExternalCommand
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
                var selectionFilter = new SelectionFilter();
                sel = uiDoc.Selection.PickObjects(objectType: Autodesk.Revit.UI.Selection.ObjectType.LinkedElement, "Выберите элементы из связанных файлов")
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
