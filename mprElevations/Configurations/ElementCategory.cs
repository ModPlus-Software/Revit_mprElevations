namespace mprElevations.Configurations
{
    using Autodesk.Revit.DB;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Моделька с категориями
    /// </summary>
    public class ElementCategory : VmBase
    {
        ///
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementCategory"/> class.
        /// </summary>
        /// <param name="name">имя категории</param>
        /// <param name="displayName">Отображаемое имя </param>
        /// <param name="builtInCategory">builtInCategory</param>
        /// <param name="note">Примечание</param>
        public ElementCategory(
            string name,
            string displayName,
            BuiltInCategory builtInCategory,
            string note = null)
        {
            Name = name;
            DisplayName = displayName;
            BuiltInCategory = BuiltInCategory;
            Note = note;
        }

        /// <summary>
        /// Имя категории
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Отображаемое имя категории
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// BuiltInCategory
        /// </summary>
        public BuiltInCategory BuiltInCategory { get; }

        /// <summary>
        /// Примечание
        /// </summary>
        public string Note { get; }
    }
}
