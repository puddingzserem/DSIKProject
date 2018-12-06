using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClientApp.Models;

namespace ClientApp.Views
{
    /// <summary>
    /// Interaction logic for EntityView.xaml
    /// </summary>
    public partial class EntityView : UserControl
    {
        MainWindow mainWindow;
        Entity entity;
        public EntityView(Entity entity, MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.entity = entity;
            InitializeComponent();
            if (entity.Downloaded())
                ItemStatus.Fill = new SolidColorBrush(Color.FromRgb(10,200,50));
            entityName.Text = entity.GetEntityName();
        }

        public delegate void RefreshRequestEventHandler(object sender, Entity entityInstance);
        public event RefreshRequestEventHandler RefreshRequest;
        private void RefreshRequestCall()
        {
            if (RefreshRequest != null)
                RefreshRequest.Invoke(this, entity);
        }
        private void UserControlDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshRequestCall();
        }
    }
}
