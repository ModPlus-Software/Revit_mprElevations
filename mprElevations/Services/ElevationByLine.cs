namespace mprElevations.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using mprElevations.Models;

    /// <summary>
    /// Класс команды
    /// </summary>
    public class ElevationByLine
    {
        /// <summary>
        /// Лист с моделями категорий
        /// </summary>
        public ObservableCollection<CategoryModel> CategoryModels;
        private readonly UIApplication _uIApplication;
        private readonly Document _doc;
        private readonly View _activeView;
        private readonly UIDocument _uidoc;
        private List<Element> _elementList;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevationByLine"/> class.
        /// </summary>
        /// <param name="application">Приложение</param>
        public ElevationByLine(UIApplication application)
        {
            _uIApplication = application;
            _doc = application.ActiveUIDocument.Document;
            _uidoc = application.ActiveUIDocument;
            _activeView = _doc.ActiveView;
            try
            {
                CategoryModels = CreateCategoryList();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Метод исполнения комманды
        /// <param name="categoryModelList">Лист с категориями элементов</param>
        /// </summary>
        public void DoWork(IList<CategoryModel> categoryModelList)
        {
            // Получаем выбранные категори
            var categoryList = categoryModelList
                .Where(i => i.IsChoose)
                .Select(i => i.ElementCategory.Id)
                .ToList();

            // Получаем выбранные элементы по категории
            var selectedElement = _elementList
                .Where(i => categoryList.Contains(i.Category.Id))
                .ToList();

            // Получаем список всех граней выбранных элементов
            var curveRefDict = GetEdges(selectedElement);

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
                var elementList = new List<SpotDimension>();

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

                        elementList.Add(spotDimention);
                    }
                }

                TaskDialog.Show("1", $"{elementList.Count}");
                tr.Commit();
            }
        }

        /// <summary>
        /// Получаем словарь состоящий из кривой и референса
        /// </summary>
        /// <param name="elementsList">Список элементов</param>
        /// <remarks>т.к. референс из геометрии самого семейства не получилось использовать для проставления марк
        /// то выбираются те же линии, которые референт создает в стене и простраиваются отметки по кривым стены
        /// поборовать это пока не удается. В семействе удается получить референс, но он равен null</remarks>
        /// <returns></returns>
        private Dictionary<Curve, Reference> GetEdges(List<Element> elementsList)
        {
            var resultDict = new Dictionary<Curve, Reference>();
            var option = new Options
            {
                ComputeReferences = true
            };
            GeometryElement geom = null;
            var usedEges = new List<Edge>();
            var temporaryEdgesList = new List<Edge>();
            foreach (var el in elementsList)
            {
                if (!(el is FamilyInstance))
                {
                    var dependentElements = el
                        .GetDependentElements(new ElementClassFilter(typeof(FamilyInstance)))
                        .Select(i => _doc.GetElement(i))
                        .ToList();

                    geom = el.get_Geometry(option);

                    foreach (var dependetElement in dependentElements)
                    {
                        foreach (Edge geometryElement in GetGeneratedHostHorizontalLines(dependetElement))
                        {
                            usedEges.Add(geometryElement);
                        }
                    }

                    foreach (var geometry in geom)
                    {
                        if (geometry is Solid solid)
                        {
                            foreach (var element in solid.Edges)
                            {
                                if (!usedEges.Contains(element))
                                {
                                    var curve = (element as Edge).AsCurve();
                                    resultDict.Add(curve, ((Edge)element).Reference);
                                }
                                else
                                {
                                    temporaryEdgesList.Add(element as Edge);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Edge geometryElement in GetGeneratedHostHorizontalLines(el))
                    {
                        if (geometryElement.Reference != null)
                        {
                            resultDict.Add(geometryElement.AsCurve(), geometryElement.Reference);
                        }
                        else if (geometryElement.Reference == null && temporaryEdgesList.Contains(geometryElement))
                        {
                            var element = temporaryEdgesList.Where(i => i.Equals(geometryElement)).First();
                            resultDict.Add(element.AsCurve(), element.Reference);
                        }
                    }
                }
            }

            return resultDict;
        }

        private IEnumerable<Edge> GetGeneratedHostHorizontalLines(Element element)
        {
            var hostElement = GetHostElement(element);

            if (hostElement == null)
            {
                yield break;
            }

            foreach (var line in GetSolids(hostElement).SelectMany(i => i.Edges.Cast<Edge>().ToList()))
            {
                if (hostElement.GetGeneratingElementIds(line).Select(i => i.IntegerValue).Contains(element.Id.IntegerValue))
                {
                    yield return line;
                }
            }
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
                var options = new Options();
                var geom = hostElement.get_Geometry(options);
                foreach (var geometryElement in geom)
                {
                    if (geometryElement is Solid solid)
                    {
                        resultList.Add(solid);
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

        /// <summary>
        /// Выборка элементов
        /// </summary>
        /// <returns></returns>
        private List<Element> SelectionElements()
        {
            // Проверяем есть ли выбранные элементы
            var sel = _uidoc.Selection
                .GetElementIds()
                .Select(i => _doc.GetElement(i))
                .ToList();

            if (!sel.Any())
            {
                sel = _uidoc
                    .Selection
                    .PickObjects(ObjectType.Element, "Выберите элементы")
                    .Select(i => _doc.GetElement(i))
                    .ToList();
            }

            return sel;
        }

        /// <summary>
        /// Создать список с Моделями категорий
        /// </summary>
        private ObservableCollection<CategoryModel> CreateCategoryList()
        {
            var categoryModelList = new ObservableCollection<CategoryModel>();
            var categoryLIst = new List<string>();
            _elementList = SelectionElements();

            foreach (var el in _elementList)
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
