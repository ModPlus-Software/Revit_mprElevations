namespace mprElevations.Commands
{
    using System;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using mprElevations.Services;
    using mprElevations.View;
    using ViewModels;

    /// <summary>
    /// Команда создания высотных отметок по текущей конфигурации
    /// </summary>
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class ElevationsCommand : IExternalCommand
    {
        private const string LangItem = "mprElevations";

        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
#if DEBUG
                ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance);
#endif
                //// TODO Тут надо получить конфигурацию и сообщить если ее нет. Если есть - выполнить построение отметок
                
                try
                {
                    Statistic.SendCommandStarting(ModPlusConnector.Instance);

                    var doc = commandData.Application.ActiveUIDocument.Document;

                    var mainContext = new MainContext(commandData.Application);
                    var settingsWindow = new SettingsWindow()
                    {
                        DataContext = mainContext
                    };
                    settingsWindow.ShowDialog();
                    
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
                catch (Exception e)
                {
                    ExceptionBox.Show(e);
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }
    }
}
