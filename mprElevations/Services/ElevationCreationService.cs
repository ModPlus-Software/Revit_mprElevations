namespace mprElevations.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using ModPlus_Revit.Utils;

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
            // Получаем список всех граней выбранных элементов
            var curveRefDict = GetEdges(listElements);

            using (var tr = new Transaction(_doc, "CreateElevations"))
            {
                tr.Start();

                // Создаем рабочую плоскость
                _activeView.SketchPlane = SketchPlane.Create(_doc, Plane.CreateByNormalAndOrigin(_activeView.ViewDirection, _activeView.Origin));

                // Получаем точку по которой создадутся уровни
                var endPoint = _uiDoc.Selection.PickPoint(
                    ObjectSnapTypes.WorkPlaneGrid | ObjectSnapTypes.Centers | ObjectSnapTypes.Endpoints |
                    ObjectSnapTypes.Midpoints | ObjectSnapTypes.Points | ObjectSnapTypes.Perpendicular,
                    "Выберите точку конца");

                var zList = new List<double>();

                // Проходимся по списку всех граней и пытаемся создать уровень
                foreach (var pair in curveRefDict)
                {
                    if (!zList.Contains(Math.Round(pair.Key.GetEndPoint(0).Z, 4)))
                    {
                        zList.Add(Math.Round(pair.Key.GetEndPoint(0).Z, 4));
                        var startPoint = pair.Key.GetEndPoint(1);
                        var bendPoint = pair.Key.GetEndPoint(1);
                        _doc.Create.NewSpotElevation(
                            _doc.ActiveView,
                            pair.Value,
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
        /// <remarks>Метод работает по трем вариантам, на инстансы без host (колонны, фундаменты), на иснтансы с host (двери, окна)
        /// и на все остальные семейства (типо системных) у всех свой путь получения геометрии</remarks>
        /// <returns></returns>
        private Dictionary<Curve, Reference> GetEdges(List<Element> elementsList)
        {
            var resultDict = new Dictionary<Curve, Reference>();
            var option = new Options
            {
                ComputeReferences = true
            };
            
            foreach (var el in elementsList)
            {
                if (el is FamilyInstance familyInstance)
                {
                    // Если у элемента есть хост объект
                    if (familyInstance.Host != null)
                    {
                        foreach (var edge in GetGeneratedHostHorizontalLines(familyInstance))
                        {
                            if (edge.AsCurve() is Line line)
                            {
                                if (!line.Direction.IsParallelTo(_upDirection))
                                {
                                    resultDict.Add(line, edge.Reference);
                                }
                            }
                            else if (edge.AsCurve() is Arc arc)
                            {
                                resultDict.Add(arc, edge.Reference);
                            }
                        }
                    }

                    // Если у элемента нет хост объект
                    else
                    {
                        var geometry = familyInstance.get_Geometry(option).GetTransformed(Transform.Identity);
                        foreach (var geometryElement in geometry)
                        {
                            if (geometryElement is Solid solid)
                            {
                                if (solid.Volume != 0)
                                {
                                    foreach (Edge edge in solid.Edges)
                                    {
                                        if (edge.AsCurve() is Line line)
                                        {
                                            if (edge.Reference != null && 
                                                !line.Direction.IsParallelTo(_upDirection))
                                            {
                                                resultDict.Add(line, edge.Reference);
                                            }
                                        }
                                        else if (edge.AsCurve() is Arc arc)
                                        {
                                            resultDict.Add(arc, edge.Reference);
                                        }
                                    }
                                }
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
                        {
                            if (edge.AsCurve() is Line line)
                            {
                                if (!line.Direction.IsParallelTo(_upDirection))
                                {
                                    resultDict.Add(line, edge.Reference);
                                }
                            }
                            else if (edge.AsCurve() is Arc arc)
                            {
                                resultDict.Add(arc, edge.Reference);
                            }
                        }
                    }
                    else
                    {
                        var solidList = GetSolids(el);
                        foreach (var solid in solidList)
                        {
                            foreach (Edge edge in solid.Edges)
                            {
                                if (edge.AsCurve() is Line line)
                                {
                                    if (!line.Direction.IsParallelTo(_upDirection))
                                    {
                                        resultDict.Add(line, edge.Reference);
                                    }
                                }
                                else if (edge.AsCurve() is Arc arc)
                                {
                                    resultDict.Add(arc, edge.Reference);
                                }
                            }
                        }
                    }
                }
            }

            return resultDict;
        }
        
        /// <summary>
        /// Возвращает грани элемента-основы, образованные воздействие элемента, полученные из тел геометрии
        /// элемента основы
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns></returns>
        private IEnumerable<Edge> GetGeneratedHostHorizontalLines(Element element)
        {
            var hostElement = GetHostElement(element);

            if (hostElement == null)
            {
                yield break;
            }

            foreach (var edge in GetSolids(hostElement).SelectMany(solid => solid.Edges.Cast<Edge>().ToList()))
            {
                if (hostElement.GetGeneratingElementIds(edge).Select(i => i.IntegerValue).Contains(element.Id.IntegerValue)
                    && edge.Reference != null)
                {
                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Получить все грани хост элемента, которые не образованные зависимыми элементам
        /// </summary>
        /// <param name="elementList">Список зависимых элементов</param>
        private List<Edge> GetGeneratedOwnLines(List<Element> elementList)
        {
            var result = new List<Edge>();
            var elementListIds = elementList.Select(element => element.Id.IntegerValue).ToList();
            foreach (var element in elementList)
            {
                var hostElement = GetHostElement(element);

                if (hostElement == null)
                {
                    continue;
                }

                foreach (var edge in GetSolids(hostElement).SelectMany(solid => solid.Edges.Cast<Edge>().ToList()))
                {
                    if (!IsContainsInLIst(elementListIds, hostElement, edge)
                        && edge.Reference != null)
                    {
                        result.Add(edge);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Метод для поиска пересечения элемента из двух списков
        /// </summary>
        /// <param name="listElementId">Список элементов с айдишниками</param>
        /// <param name="hostElement">Хост элемент</param>
        /// <param name="edge">Грань</param>
        /// <returns></returns>
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
        /// <returns></returns>
        private List<Solid> GetSolids(Element hostElement)
        {
            var resultList = new List<Solid>();
            if (!(hostElement is FamilyInstance))
            {
                var options = new Options
                {
                    ComputeReferences = true
                };
                var geom = hostElement.get_Geometry(options);
                foreach (var geometryElement in geom)
                {
                    if (geometryElement is Solid solid)
                    {
                        if (solid.Volume != 0)
                        {
                            resultList.Add(solid);
                        }
                    }
                }
            }

            return resultList;
        }

        /// <summary>
        /// Возвращает элемент основу для текущего элемента, если текущий элемент является экземпляром семества
        /// </summary>
        /// <param name="element">Элемент</param>
        private Element GetHostElement(Element element)
        {
            return element is FamilyInstance familyInstance && familyInstance.Host != null ? familyInstance.Host : null;
        }
    }
}
