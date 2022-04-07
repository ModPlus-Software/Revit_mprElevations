namespace mprElevations.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ModPlus_Revit.Utils;
using ModPlusAPI;
using Models;

/// <summary>
/// Класс команды
/// </summary>
public class ElevationCreationService
{
    private readonly Document _doc;
    private readonly View _activeView;
    private readonly UIDocument _uiDoc;
    private readonly XYZ _upDirection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElevationCreationService"/> class.
    /// </summary>
    /// <param name="uiDoc">Приложение</param>
    public ElevationCreationService(UIDocument uiDoc)
    {
        _doc = uiDoc.Document;
        _uiDoc = uiDoc;
        _activeView = _doc.ActiveView;
        _upDirection = _activeView.UpDirection;
    }

    /// <summary>
    /// Метод исполнения команды 
    /// </summary>
    /// <param name="listElements">Лист с элементами элементов</param>
    public void DoWork(List<ElementModel> listElements)
    {
        var curveRefDict = GetEdges(listElements).Where(t => t != default).ToList();

        var trName = Language.GetPluginLocalName(ModPlusConnector.Instance);
        if (string.IsNullOrEmpty(trName))
            trName = "CreateElevations";

        using (var tr = new Transaction(_doc, trName))
        {
            tr.Start();

            _activeView.SketchPlane = SketchPlane.Create(
                _doc, Plane.CreateByNormalAndOrigin(_activeView.ViewDirection, _activeView.Origin));

            // Выберите точку конца
            var endPoint = _uiDoc.Selection.PickPoint(
                ObjectSnapTypes.WorkPlaneGrid | ObjectSnapTypes.Centers | ObjectSnapTypes.Endpoints |
                ObjectSnapTypes.Midpoints | ObjectSnapTypes.Points | ObjectSnapTypes.Perpendicular,
                Language.GetItem("h3"));

            var zList = new List<double>();

            foreach (var (curve, reference) in curveRefDict)
            {
                if (!zList.Contains(Math.Round(curve.GetEndPoint(0).Z, 4)))
                {
                    zList.Add(Math.Round(curve.GetEndPoint(0).Z, 4));
                    var startPoint = curve.GetEndPoint(1);
                    var bendPoint = curve.GetEndPoint(1);
                    _doc.Create.NewSpotElevation(
                        _doc.ActiveView,
                        reference,
                        startPoint,
                        bendPoint,
                        endPoint,
                        endPoint,
                        true);
                }
            }

            tr.Commit();
        }
    }

    /// <summary>
    /// Получаем словарь состоящий из кривой и референса
    /// </summary>
    /// <param name="elementsList">Список элементов</param>
    /// <remarks>Метод работает по трем вариантам, на экземпляры без host (колонны, фундаменты), на экземпляры
    /// с host (двери, окна) и на все остальные семейства (типа системных) у всех свой путь получения геометрии
    /// </remarks>
    private IEnumerable<(Curve, Reference)> GetEdges(List<ElementModel> elementsList)
    {
        var option = new Options
        {
            ComputeReferences = true
        };

        // Список для перебора элементов по классам
        var dependetClasses = new List<Type>
        {
            typeof(FamilyInstance),
            typeof(Opening),
            typeof(Panel)
        };

        foreach (var el in elementsList)
        {
            if (dependetClasses.Any(classType => el.Elem.GetType() == classType))
            {
                var hostElement = GetHostElement(el.Elem);
                if (hostElement != null
                    && !(hostElement is Level)
                    && !(el.Elem is Panel))
                {
                    foreach (var edge in GetGeneratedHostHorizontalLines(el))
                        yield return ProcessEdge(edge, el);
                }
                else
                {
                    var geometry = el.Elem.get_Geometry(option).GetTransformed(
                        el.LinkInstance == null 
                            ? Transform.Identity
                            : el.LinkInstance.GetTotalTransform());
                        
                    foreach (var geometryElement in geometry)
                    {
                        if (geometryElement is Solid solid && solid.Volume != 0)
                        {
                            foreach (Edge edge in solid.Edges)
                                yield return ProcessEdge(edge, el);
                        }
                    }
                }
            }
            else
            {
                var dependentElements = new List<Element>();
                if (el.Elem is Wall || el.Elem is Floor)
                {
                    dependentElements = ((HostObject)el.Elem)
                        .FindInserts(true, false, true, true)
                        .Select(i => el.Doc.GetElement(i))
                        .ToList();
                }

                if (dependentElements.Any())
                {
                    foreach (var edge in GetGeneratedOwnLines(dependentElements, el))
                        yield return ProcessEdge(edge, el);
                }
                else
                {
                    var solidList = GetSolids(el);
                    foreach (var solid in solidList)
                    {
                        foreach (Edge edge in solid.Edges)
                            yield return ProcessEdge(edge, el);
                    }
                }
            }
        }
    }

    private (Curve, Reference) ProcessEdge(Edge edge, ElementModel elementModel)
    {
        Reference reference;
        if (elementModel.LinkInstance == null)
        {
            reference = edge.Reference;
        }
        else
        {
            reference = edge.Reference;
            var stableRepresentation = reference.CreateLinkReference(elementModel.LinkInstance).ConvertToStableRepresentation(_doc);

            // Приведение получаемой строки из одного вида в другой
            // 1a5ab77d-1ae2-4e82-872d-63be5c36dec1-00039e53:RVTLINK/1a5ab77d-1ae2-4e82-872d-63be5c36dec1-00039e52:1224485 ->
            // 1a5ab77d-1ae2-4e82-872d-63be5c36dec1-00039e53:0:RVTLINK/1a5ab77d-1ae2-4e82-872d-63be5c36dec1-00039e52:1224485
            // инфа с форума https://adn-cis.org/forum/index.php?topic=2757.0
            var fitstUnderString = stableRepresentation.Split(':')[0];
            var resultString = fitstUnderString + ":0";
            for (int i = 1; i < stableRepresentation.Split(':').Count(); i++)
            {
                resultString += ":" + stableRepresentation.Split(':')[i];
            }

            reference = Reference.ParseFromStableRepresentation(_doc, resultString);
        }

        if (edge.AsCurve() is Line line && !line.Direction.IsParallelTo(_upDirection))
            return (line, reference);

        if (edge.AsCurve() is Arc arc)
            return (arc, reference);

        return default;
    }

    /// <summary>
    /// Возвращает грани элемента-основы, образованные воздействие элемента, полученные из тел геометрии
    /// элемента основы
    /// </summary>
    /// <param name="elementModel">Модель элемента</param>
    private IEnumerable<Edge> GetGeneratedHostHorizontalLines(ElementModel elementModel)
    {
        var hostElement = GetHostElement(elementModel.Elem);

        if (hostElement == null)
            yield break;

        foreach (var edge in GetSolids(new ElementModel(
                         hostElement,
                         elementModel.Doc,
                         elementModel.LinkInstance))
                     .SelectMany(solid => solid.Edges
                         .Cast<Edge>()
                         .ToList()))
        {
            if (hostElement.GetGeneratingElementIds(edge).Select(i => i.IntegerValue).Contains(elementModel.Elem.Id.IntegerValue)
                && edge.Reference != null)
                yield return edge;
        }
    }

    /// <summary>
    /// Получить все грани хост элемента, которые не образованные зависимыми элементам
    /// </summary>
    /// <param name="elementList">Список зависимых элементов</param>
    /// <param name="elementModel"><see cref="ElementModel"/></param>
    private IEnumerable<Edge> GetGeneratedOwnLines(List<Element> elementList, ElementModel elementModel)
    {
        var elementListIds = elementList.Select(element => element.Id.IntegerValue).ToList();
        foreach (var element in elementList)
        {
            var hostElement = GetHostElement(element);

            if (hostElement == null)
                continue;

            foreach (var edge in GetSolids(new ElementModel(
                             hostElement,
                             elementModel.Doc,
                             elementModel.LinkInstance))
                         .SelectMany(solid => solid.Edges.Cast<Edge>()
                             .ToList()))
            {
                if (!IsContainsInLIst(elementListIds, hostElement, edge)
                    && edge.Reference != null)
                    yield return edge;
            }
        }
    }

    /// <summary>
    /// Метод для поиска пересечения элемента из двух списков
    /// </summary>
    /// <param name="listElementId">Список элементов с айдишниками</param>
    /// <param name="hostElement">Хост элемент</param>
    /// <param name="edge">Грань</param>
    private bool IsContainsInLIst(List<int> listElementId, Element hostElement, Edge edge)
    {
        foreach (var el in listElementId)
        {
            if (hostElement.GetGeneratingElementIds(edge).Select(i => i.IntegerValue).Contains(el))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Получение солида из модели с элементом
    /// </summary>
    /// <param name="elementModel">Экземпляр модельки с элементом</param>
    private IEnumerable<Solid> GetSolids(ElementModel elementModel)
    {
        if (!(elementModel.Elem is FamilyInstance))
        {
            var options = new Options
            {
                ComputeReferences = true
            };

            var geom = elementModel.Elem.get_Geometry(options);
            if (geom != null)
            {
                geom = geom.GetTransformed(
                    elementModel.LinkInstance == null
                        ? Transform.Identity
                        : elementModel.LinkInstance.GetTotalTransform());
                    
                foreach (var geometryElement in geom)
                {
                    if (geometryElement is Solid { Volume: > 0 } solid)
                        yield return solid;
                }
            }
        }
    }

    /// <summary>
    /// Возвращает элемент основу для текущего элемента, если текущий элемент является экземпляром семейства
    /// </summary>
    /// <param name="element">Элемент</param>
    private Element GetHostElement(Element element)
    {
        switch (element)
        {
            case Opening opening:
                return opening.Host;
            case FamilyInstance familyInstance:
                return familyInstance.Host;
            default:
                return null;
        }
    }
}