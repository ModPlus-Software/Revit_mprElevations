namespace mprElevations.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Класс команды
    /// </summary>
    public class ElevationCreationService
    {
        private readonly Document _doc;
        private readonly View _activeView;
        private readonly UIDocument _uidoc;
        private readonly XYZ _upDirection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevationCreationService"/> class.
        /// </summary>
        /// <param name="uidoc">Приложение</param>
        public ElevationCreationService(UIDocument uidoc)
        {
            _doc = uidoc.Document;
            _uidoc = uidoc;
            _activeView = _doc.ActiveView;
            _upDirection = _activeView.UpDirection;
        }

        /// <summary>
        /// Метод исполнения команды
        /// <param name="listElements">Лист с элементами элементов</param>
        /// </summary>
        public void DoWork(List<Element> listElements)
        {
            // Получаем список всех граней выбранных элементов
            var curveRefDict = GetEdges(listElements);

            using (Transaction tr = new Transaction(_doc, "CreateElevations"))
            {
                tr.Start();

                // Создаем рабочую плоскость
                _activeView.SketchPlane = SketchPlane.Create(_doc, Plane.CreateByNormalAndOrigin(_activeView.ViewDirection, _activeView.Origin));

                // Получаем точку по которой создадутся уровни
                var endPoint = _uidoc.Selection.PickPoint(
                    ObjectSnapTypes.WorkPlaneGrid | ObjectSnapTypes.Centers | ObjectSnapTypes.Endpoints |
                    ObjectSnapTypes.Midpoints | ObjectSnapTypes.Points | ObjectSnapTypes.Perpendicular,
                    "Выберите точку конца");

                var zList = new List<double>();

                // Проходимся по списку всех граней и пытаемся создать уровень
                foreach (var keyValyePair in curveRefDict)
                {
                    if (!zList.Contains(Math.Round(keyValyePair.Key.GetEndPoint(0).Z, 4)))
                    {
                        zList.Add(Math.Round(keyValyePair.Key.GetEndPoint(0).Z, 4));
                        var startPoint = keyValyePair.Key.GetEndPoint(1);
                        var bendPoint = keyValyePair.Key.GetEndPoint(1);
                        var spotDimention = _doc.Create.NewSpotElevation(
                            _doc.ActiveView,
                            keyValyePair.Value,
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
        /// <remarks>Метод работает по трем варинатам, на инстансы без host (колонны, фундаменты), на иснтансы с host (двери, окна)
        /// и на все остальные семейства (типо системных) у всех свой путь получения геометрии</remarks>
        /// <returns></returns>
        private Dictionary<Curve, Reference> GetEdges(List<Element> elementsList)
        {
            var resultDict = new Dictionary<Curve, Reference>();
            var option = new Options
            {
                ComputeReferences = true
            };
            var usedEges = new List<Edge>();
            var temporaryEdgesList = new List<Edge>();
            foreach (var el in elementsList)
            {
                if (el is FamilyInstance)
                {
                    // Если у элемента есть хост объект
                    if ((el as FamilyInstance).Host != null)
                    {
                        foreach (var edge in GetGeneratedHostHorizontalLines(el))
                        {
                            if (!IsParallelTo(((Line)edge.AsCurve()).Direction, _upDirection))
                            {
                                resultDict.Add(edge.AsCurve(), edge.Reference);
                            }
                        }
                    }

                    // Если у элемента нет хост объект
                    else
                    {
                        var geometry = el.get_Geometry(option).GetTransformed(Transform.Identity);
                        foreach (var geometryElement in geometry)
                        {
                            if (geometryElement is Solid solid)
                            {
                                if (solid.Volume != 0)
                                {
                                    foreach (Edge edge in solid.Edges)
                                    {
                                        if (edge.Reference != null && !IsParallelTo(((Line)edge.AsCurve()).Direction, _upDirection))
                                        {
                                            resultDict.Add(edge.AsCurve(), edge.Reference);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Когда элемент не фамили инстанс
                else
                {
                    var dependentElements = el
                            .GetDependentElements(new ElementClassFilter(typeof(FamilyInstance)))
                            .Select(i => _doc.GetElement(i))
                            .ToList();

                    foreach (var edge in GetGeneratedOwnLines(dependentElements))
                    {
                        if (!IsParallelTo(((Line)edge.AsCurve()).Direction, _upDirection))
                        {
                            resultDict.Add(edge.AsCurve(), edge.Reference);
                        }
                    }
                }
            }

            return resultDict;
        }

        /// <summary>
        /// Определить параллельность
        /// </summary>
        /// <param name="firstDirection">Первое направление</param>
        /// <param name="secondDirection">Второе направление</param>
        /// <returns></returns>
        private bool IsParallelTo(XYZ firstDirection, XYZ secondDirection)
        {
            if (firstDirection.CrossProduct(secondDirection).IsAlmostEqualTo(XYZ.Zero))
                return true;
            else
                return false;
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
                if (!(edge is Edge))
                    continue;

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
        /// <returns></returns>
        private List<Edge> GetGeneratedOwnLines(List<Element> elementList)
        {
            var resulList = new List<Edge>();
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
                    if (!(edge is Edge))
                        continue;

                    if (!IsContainsInLIst(elementListIds, hostElement, edge)
                        && edge.Reference != null)
                    {
                        resulList.Add(edge);
                    }
                }
            }

            return resulList;
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
                var options = new Options()
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
        /// <returns></returns>
        private Element GetHostElement(Element element)
        {
            if (element is FamilyInstance familyInstance && familyInstance.Host != null)
                return familyInstance.Host;
            else
                return null;
        }
    }
}
