using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Diagnostics.CodeAnalysis;

namespace Jannesen.Configuration.Settings
{
    public static class AppSettings
    {
        private sealed class StaticSettings
        {
            public  readonly            string                          ProgramExe;
            public  readonly            string                          ProgramDirectory;
            public  readonly            IReadOnlyCollection<string>     ConfigFilenames;
            public  readonly            NameValueCollection             AppSettings;

            public                                              StaticSettings()
            {
                ProgramExe       = Path.ChangeExtension(Environment.ProcessPath ?? throw new AppSettingException("Environment.ProcessPath is null."), ".exe");
                var path = Path.GetDirectoryName(ProgramExe);                
                if (string.IsNullOrEmpty(path))
                    throw new AppSettingException("Can't GetDirectoryName of '" + ProgramExe + "'.");
                ProgramDirectory = Path.TrimEndingDirectorySeparator(path);
                AppSettings      = new NameValueCollection();

                var configFilenames = new List<string>();
                _loadAppSettings(configFilenames, Path.ChangeExtension(ProgramExe, ".dll.config"), "/configuration/appSettings");

                ConfigFilenames = configFilenames.AsReadOnly();
            }

            private                     void                    _loadAppSettings(List<string> configFilenames, string filename, string xpath)
            {
                var xmlFile = new XmlDocument() { XmlResolver=null };

                try {
                    using (var reader = new XmlTextReader(filename) {DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null} ) {
                        xmlFile.Load(reader);
                    }
                }
                catch(Exception err) {
                    throw new AppSettingException("Failed to load '" + filename + "'.", err);
                }

                configFilenames.Add(filename);

                try {
                    var elmAppSettings = xmlFile.SelectSingleNode(xpath) as XmlElement ?? throw new AppSettingException("Missing element " + xpath);

                    var file = elmAppSettings.Attributes["file"];
                    if (file != null) {
                        var path = Path.GetDirectoryName(filename);                
                        if (string.IsNullOrEmpty(path))
                            throw new AppSettingException("Can't GetDirectoryName of '" + filename + "'.");

                        _loadAppSettings(configFilenames, Path.Combine(path, file.Value), "/appSettings");
                    }

                    foreach (XmlNode node in elmAppSettings.ChildNodes) {
                        if (node is XmlElement elm) {
                            switch(elm.Name) {
                            case "add": {
                                    var key   = elm.GetAttribute("key");
                                    var value = elm.GetAttribute("value");
                                    if (key != null) {
                                        AppSettings.Add(key, value);
                                    }
                                }
                                break;

                            case "remove": {
                                    var key = elm.GetAttribute("key");
                                    if (key != null) {
                                        AppSettings.Remove(key);
                                    }
                                }
                                break;

                            case "clear":
                                AppSettings.Clear();
                                break;
                            }
                        }
                    }
                }
                catch(Exception err) {
                    throw new AppSettingException("Error while processing '" + filename + "'.", err);
                }
            }
        }
       
        private static  readonly    object                  _lockObject = new object();
        private static              StaticSettings?         _Settings;

        private static              StaticSettings          Settings {
            get {
                lock(_lockObject) {
                    if (_Settings == null) {
                        _Settings = new StaticSettings();
                    }

                    return _Settings;
                }
            }
        }

        public  static              string                          ProgramExe          => Settings.ProgramExe;
        public  static              string                          ProgramDirectory    => Settings.ProgramDirectory;
        public  static              IReadOnlyCollection<string>     ConfigFilenames     => Settings.ConfigFilenames;

        public  static              string                          GetSetting(string name)
        {
            return GetSetting(name, null) ?? throw new AppSettingException("Missing appSetting '" + name + "'.");
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public  static              string?                         GetSetting(string name, string? defaultValue)
        {
            var value = Settings.AppSettings[name];

            if (value is null) {
                return defaultValue;
            }

            try {
                var pbegin = 0;

                while (pbegin < value.Length && (pbegin = value.IndexOf("${", pbegin, StringComparison.Ordinal)) >= 0) {
                    var pend = value.IndexOf('}', pbegin + 1);
                    if (pend < 0) {
                        throw new AppSettingException("Missing '}' in ${<name>}.");
                    }
                    var expname  = value.Substring(pbegin + 2, pend - pbegin - 2);
                    var expvalue = GetExpandSetting(expname)
                                       ?? throw new AppSettingException("Can't find '" + expname + "' in appSettings or environment.");
                    value  = string.Concat(value.AsSpan(0, pbegin), expvalue, value.AsSpan(pend + 1));
                    pbegin = pend + 1;
                }
            }
            catch (Exception err) {
                throw new AppSettingException("Failed to expand appSetting '" + name + "'.", err);
            }

            return value;
        }

        public  static              string?                         GetExpandSetting(string name)
        {
            string? value;

            if ((value = GetSetting(name, null)) != null) {
                return value;
            }

            if ((value = Environment.GetEnvironmentVariable(name)) != null) {
                return value;
            }

            switch (name) {
            case "ProgramDirectory": return ProgramDirectory;
            }

            return null;
        }

    }
}
