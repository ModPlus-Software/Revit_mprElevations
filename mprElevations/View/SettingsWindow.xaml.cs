namespace mprElevations.View
{
    using Models;
    using ModPlusAPI.Abstractions;

    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : IClosable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Button_Apply_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Window.Close();
        }

        private void Button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Window.Close();
        }

        private void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            bool flag = true;
            CategoryModel firstElementInList = null;
            foreach (CategoryModel el in list.ItemsSource)
            {
                firstElementInList = el;
                break;
            }

            if (firstElementInList.IsChoose)
                flag = false;
            else
                flag = true;
            foreach (CategoryModel el in list.ItemsSource)
            {
                el.IsChoose = flag;
            }
        }
    }
}
