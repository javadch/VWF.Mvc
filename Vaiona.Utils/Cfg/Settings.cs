using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vaiona.Utils.IO;

namespace Vaiona.Utils.Cfg
{
    /// <summary>
    /// Base class for managing the settings of each module as well as shell (genral).
    /// This class monitors the specified path for file changes and re-syncs the settings if needed.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Contains the settings entries. 
        /// Each entry has a key, a value, and a type.
        /// The type field must match System.TypeCode enumeration, case sensitive.
        /// </summary>
        protected XElement settingsElement;

        /// <summary>
        /// The name of the module or shell or whatever this settings belongs to
        /// </summary>
        protected string id = "";

        /// <summary>
        /// The full path of the setting file. the file should be an XML containg a set of 'entry' items, each having key, value, and type.
        /// The file itself should follow <id>.settings.xml naming format.
        /// The path can be anywhere, but in general, for the modules, its in the root folder of the modules in the workspace folder.
        /// </summary>
        protected string settingsFullPath = "";

        private FileSystemWatcher watcher = new FileSystemWatcher();

        public Settings(string id, string settingsFullPath)
        {
            this.id = id;
            this.settingsFullPath = settingsFullPath;
            if (string.IsNullOrWhiteSpace(settingsFullPath))
                throw new ArgumentNullException("Provided setting path is null or empty");
            if (!File.Exists(settingsFullPath))
                throw new FileNotFoundException($"Provided path {settingsFullPath} does not exist.");

            watcher.Path = Path.GetDirectoryName(settingsFullPath);
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch the manifest file.
            watcher.Filter = Path.GetFileName(settingsFullPath);

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(onCatalogChanged);
            watcher.Created += new FileSystemEventHandler(onCatalogChanged);
            watcher.Deleted += new FileSystemEventHandler(onCatalogChanged);
            watcher.Renamed += new RenamedEventHandler(onCatalogChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
            loadSettings();
        }

        public List<XElement> Entries
        {
            get { return settingsElement.Elements("entry").ToList(); }
            // set{} in this version, the settings is a readonly object
        }

        public object GetEntryValue(string entryKey)
        {
            XElement entry = Entries.Where(p => p.Attribute("key").Value.Equals(entryKey, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (entry == null)
                return null;
            string value = entry.Attribute("value").Value;
            string type = entry.Attribute("type").Value;
            var typedValue = Convert.ChangeType(value, (TypeCode)Enum.Parse(typeof(TypeCode), type));
            return typedValue;
        }

        private void onCatalogChanged(object source, FileSystemEventArgs e)
        {
            loadSettings();
        }

        private void loadSettings()
        {
            FileHelper.WaitForFile(settingsFullPath);
            using (var stream = File.Open(settingsFullPath, FileMode.Open, FileAccess.Read))
            {
                settingsElement = XElement.Load(stream);
            }
        }
    }
}
