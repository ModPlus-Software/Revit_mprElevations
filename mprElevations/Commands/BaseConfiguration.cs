namespace mprElevations.Commands
{
    using System;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Базовый класс конфигурации
    /// </summary>
    public class BaseConfiguration : VmBase
    {
        private string _name;

        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название конфигурации
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }
}
