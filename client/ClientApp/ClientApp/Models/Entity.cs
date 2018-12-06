using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ClientApp.Models
{
    public class Entity
    {
        private string entityName;
        private string entityDescriptionFilePath;
        private string entityImageFilePath;
        private string entityVideoFilePath;
        bool isDownloaded=false;

        public Entity(string name)
        {
            entityName = name;
        }
        public void IsDownloaded()
        {
            isDownloaded  = true;
        }
        public void SetEntityDescription(string path)
        {
            if (File.Exists(path))
                this.entityDescriptionFilePath = path;
            else
                System.Windows.MessageBox.Show("File doesn't exist");
        }
        public void SetEntityImage(string path)
        {
            if (File.Exists(path))
                entityImageFilePath = path;
            else
                System.Windows.MessageBox.Show("File doesn't exist");
        }
        public void SetEntityVideo(string path)
        {
            if (File.Exists(path))
                entityVideoFilePath = path;
            else
                System.Windows.MessageBox.Show("File doesn't exist");
        }
        public string GetEntityName()
        {
            return entityName;
        }
        public string GetEntityDescription()
        {
            if (!String.IsNullOrEmpty(entityDescriptionFilePath))
                return entityDescriptionFilePath;
            else
                return null;
        }
        public string GetEntityImage()
        {
            if (!String.IsNullOrEmpty(entityImageFilePath))
                return entityImageFilePath;
            else
                return null;
        }
        public string GetEntityVideo()
        {
            if (!String.IsNullOrEmpty(entityVideoFilePath))
                return entityVideoFilePath;
            else
                return null;
        }
        public bool Downloaded()
        {
            return isDownloaded;
        }
    }
}
