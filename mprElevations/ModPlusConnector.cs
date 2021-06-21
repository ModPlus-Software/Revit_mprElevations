namespace mprElevations
{
    using System;
    using System.Collections.Generic;
    using Commands;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Enums;

    /// <inheritdoc/>
    public class ModPlusConnector : IModPlusPlugin
    {
        private static ModPlusConnector _instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ModPlusConnector Instance => _instance ?? (_instance = new ModPlusConnector());

        /// <inheritdoc/>
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        /// <inheritdoc/>
        public string Name => "mprElevations";

#if R2019
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2019";
#elif R2020
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2020";
#elif R2021
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2021";
#elif R2022
        /// <inheritdoc/>
        public string AvailProductExternalVersion => "2022";
#endif

        /// <inheritdoc/>
        public string FullClassName => "mprElevations.Commands.ElevationsCurrentDocCommand";

        /// <inheritdoc/>
        public string AppFullClassName => string.Empty;

        /// <inheritdoc/>
        public Guid AddInId => Guid.Empty;

        /// <inheritdoc/>
        public string LName => "Высотные отметки";

        /// <inheritdoc/>
        public string Description => "Быстрое создание высотных отметок на сечениях и фасадах";

        /// <inheritdoc/>
        public string Author => "Алексей Никитенко";

        /// <inheritdoc/>
        public string Price => "0";

        /// <inheritdoc/>
        public bool CanAddToRibbon => true;

        /// <inheritdoc/>
        public string FullDescription => string.Empty;

        /// <inheritdoc/>
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc/>
        public List<string> SubPluginsNames => new List<string>
        {
            "mprLinkElevations"
        };

        /// <inheritdoc/>
        public List<string> SubPluginsLNames => new List<string>
        {
            "Высотные отметки по связанным файлам"
        };

        /// <inheritdoc/>
        public List<string> SubDescriptions => new List<string>
        { 
            "Быстрое создание высотных отметок на сечениях и фасадах по элементам из связанных файлов"
        };

        /// <inheritdoc/>
        public List<string> SubFullDescriptions => new List<string>
        {
            string.Empty
        };

        /// <inheritdoc/>
        public List<string> SubHelpImages => new List<string>
        {
            string.Empty
        };

        /// <inheritdoc/>
        public List<string> SubClassNames => new List<string>
        {
            $"mprElevations.Commands.{nameof(ElevationsLinkedDocCommand)}"
        };
    }
}
