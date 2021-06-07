namespace mprElevations.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using ModPlus_Revit.Utils;
    using ModPlusAPI;

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
        public void DoWork(List<Element> listElements)
        {
            var curveRefDict = GetEdges(listElements).Where(t => t != default).ToList();

            var trName = Language.GetFunctionLocalName(ModPlusConnector.Instance);
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
        private IEnumerable<(Curve, Reference)> GetEdges(List<Element> elementsList)
        {
            var option = new Options
            {
                ComputeReferences = true
            };

            foreach (var el in elementsList)
            {
                if (el is FamilyInstance familyInstance)
                {
                    if (familyInstance.Host != null)
                    {
                        foreach (var edge in GetGeneratedHostHorizontalLines(familyInstance))
                            yield return ProcessEdge(edge);
                    }
                    else
                    {
                        var geometry = familyInstance.get_Geometry(option).GetTransformed(Transform.Identity);
                        foreach (var geometryElement in geometry)
                        {
                            if (geometryElement is Solid solid && solid.Volume != 0)
                            {
                                foreach (Edge edge in solid.Edges)
                                    yield return ProcessEdge(edge);
                            }
                        }
                    }
                }
                else
                {
                    var dependentElements = el
                            .GetDependentElements(new ElementClassFilter(typeof(FamilyInstance)))
                            .Select(i => _doc.GetElement(i))
                            .ToList();
                    if (dependentElements.Any())
                    {
                        foreach (var edge in GetGeneratedOwnLines(dependentElements))
                            yield return ProcessEdge(edge);
                    }
                    else
                    {
                        var solidList = GetSolids(el);
                        foreach (var solid in solidList)
                        {
                            foreach (Edge edge in solid.Edges)
                                yield return ProcessEdge(edge);
                        }
                    }
                }
            }
        }

        private (Curve, Reference) ProcessEdge(Edge edge)
        {
            if (edge.AsCurve() is Line line && !line.Direction.IsParallelTo(_upDirection))
                return (line, edge.Reference);

            if (edge.AsCurve() is Arc arc)
                return (arc, edge.Reference);

            return default;
        }

        /// <summary>
        /// Возвращает грани элемента-основы, образованные воздействие элемента, полученные из тел геометрии
        /// элемента основы
        /// </summary>
        /// <param name="element">Элемент</param>
        private IEnumerable<Edge> GetGeneratedHostHorizontalLines(Element element)
        {
            var hostElement = GetHostElement(element);

            if (hostElement == null)
                yield break;

            foreach (var edge in GetSolids(hostElement).SelectMany(solid => solid.Edges.Cast<Edge>().ToList()))
            {
                if (hostElement.GetGeneratingElementIds(edge).Select(i => i.IntegerValue).Contains(element.Id.IntegerValue)
                    && edge.Reference != null)
                    yield return edge;
            }
        }

        /// <summary>
        /// Получить все грани хост элемента, которые не образованные зависимыми элементам
        /// </summary>
        /// <param name="elementList">Список зависимых элементов</param>
        private IEnumerable<Edge> GetGeneratedOwnLines(List<Element> elementList)
        {
            var elementListIds = elementList.Select(element => element.Id.IntegerValue).ToList();
            foreach (var element in elementList)
            {
                var hostElement = GetHostElement(element);

                if (hostElement == null)
                    continue;

                foreach (var edge in GetSolids(hostElement).SelectMany(solid => solid.Edges.Cast<Edge>().ToList()))
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
        /// Получение солида
        /// </summary>
        /// <param name="hostElement">Родительский элемент</param>
        private IEnumerable<Solid> GetSolids(Element hostElement)
        {
            if (!(hostElement is FamilyInstance))
            {
                var options = new Options
                {
                    ComputeReferences = true
                };

                var geom = hostElement.get_Geometry(options);
                if (geom != null)
                {
                    geom = geom.GetTransformed(Transform.Identity);
                    foreach (var geometryElement in geom)
                    {
                        if (geometryElement is Solid solid && solid.Volume > 0)
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
            return element is FamilyInstance familyInstance && familyInstance.Host != null ? familyInstance.Host : null;
        }
    }
}
