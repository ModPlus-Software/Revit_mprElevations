namespace mprElevations.Utility
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Класс селектора для выбора элементов
    /// </summary>
    public class SelectionFilter : ISelectionFilter
    {
        /// <inheritdoc/>
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance)
                return true;
            if (elem is Group)
                return false;
            return true;
        }

        /// <inheritdoc/>
        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
