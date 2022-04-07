namespace mprElevations.Models;

using Autodesk.Revit.DB;

/// <summary>
/// Модель представляющая из себя элемент и его документ
/// </summary>
public class ElementModel
{
    /// <summary>
    /// Конструктор модели элемента с референсом
    /// </summary>
    /// <param name="reference">Получаемый референс</param>
    /// <param name="document">Текущий рабочий документ</param>
    public ElementModel(Reference reference, Document document)
    {
        if (reference.LinkedElementId != ElementId.InvalidElementId)
        {
            LinkInstance = (RevitLinkInstance)document.GetElement(reference.ElementId);
            Doc = LinkInstance.GetLinkDocument();
            Elem = Doc.GetElement(reference.LinkedElementId);
        }
        else
        {
            Doc = document;
            Elem = document.GetElement(reference.ElementId);
        }
    }

    /// <summary>
    /// Конструктор с элементом и документом
    /// </summary>
    /// <param name="element">Элемент</param>
    /// <param name="document">Текущий рабочий документ</param>
    /// <param name="revitLinkInstance">Экземпляр связи</param>
    public ElementModel(Element element, Document document, RevitLinkInstance revitLinkInstance = null)
    {
        Doc = document;
        Elem = element;
        LinkInstance = revitLinkInstance;
    }

    /// <summary>
    /// Сам элемент
    /// </summary>
    public Element Elem { get; }

    /// <summary>
    /// Документ в котором содержится элемент
    /// </summary>
    public Document Doc { get; }

    /// <summary>
    /// Экземпляр связи
    /// </summary>
    public RevitLinkInstance LinkInstance { get; }
}