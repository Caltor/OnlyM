﻿namespace OnlyM.Core.Services.Options
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Serilog;
    using Utils;

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class OptionsService : IOptionsService
    {
        private readonly ILogLevelSwitchService _logLevelSwitchService;

        private readonly int _optionsVersion = 1;
        private Options _options;
        private string _optionsFilePath;
        private string _originalOptionsSignature;
        
        public event EventHandler MediaFolderChangedEvent;

        public event EventHandler ImageFadeTypeChangedEvent;

        public event EventHandler ImageFadeSpeedChangedEvent;

        public event EventHandler AlwaysOnTopChangedEvent;
        
        public OptionsService(ILogLevelSwitchService logLevelSwitchService)
        {
            _logLevelSwitchService = logLevelSwitchService;
        }

        public Options Options
        {
            get
            {
                Init();
                return _options;
            }
        }

        public bool IsMediaMonitorSpecified
        {
            get
            {
                Init();
                return !string.IsNullOrEmpty(Options.MediaMonitorId);
            }
        }

        /// <summary>
        /// Saves the settings (if they have changed since they were last read)
        /// </summary>
        public void Save()
        {
            try
            {
                var newSignature = GetOptionsSignature(_options);
                if (_originalOptionsSignature != newSignature)
                {
                    // changed...
                    WriteOptions();
                    Log.Logger.Information("Settings changed and saved");
                }
            }
            // ReSharper disable once CatchAllClause
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Could not save settings");
            }
        }

        private void Init()
        {
            if (_options == null)
            {
                try
                {
                    string commandLineIdentifier = CommandLineParser.Instance.GetId();
                    _optionsFilePath = FileUtils.GetUserOptionsFilePath(commandLineIdentifier, _optionsVersion);
                    var path = Path.GetDirectoryName(_optionsFilePath);
                    if (path != null)
                    {
                        FileUtils.CreateDirectory(path);
                        ReadOptions();
                    }

                    if (_options == null)
                    {
                        _options = new Options();
                    }

                    // store the original settings so that we can determine if they have changed
                    // when we come to save them
                    _originalOptionsSignature = GetOptionsSignature(_options);
                }
                // ReSharper disable once CatchAllClause
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Could not read options file");
                    _options = new Options();
                }

                if (_options != null)
                {
                    _options.MediaFolderChangedEvent += HandleMediaFolderChangedEvent;
                    _options.ImageFadeTypeChangedEvent += HandleImageFadeTypeChangedEvent;
                    _options.ImageFadeSpeedChangedEvent += HandleImageFadeSpeedChangedEvent;
                    _options.LogEventLevelChangedEvent += HandleLogEventLevelChangedEvent;
                    _options.AlwaysOnTopChangedEvent += HandleAlwaysOnTopChangedEvent;
                    
                    _logLevelSwitchService.SetMinimumLevel(Options.LogEventLevel);
                }
            }
        }

        private void HandleAlwaysOnTopChangedEvent(object sender, EventArgs e)
        {
            OnAlwaysOnTopChangedEvent();
        }

        private void HandleImageFadeSpeedChangedEvent(object sender, EventArgs e)
        {
            OnImageFadeSpeedChangedEvent();
        }

        private void HandleLogEventLevelChangedEvent(object sender, EventArgs e)
        {
            _logLevelSwitchService.SetMinimumLevel(Options.LogEventLevel);
        }

        private void HandleImageFadeTypeChangedEvent(object sender, EventArgs e)
        {
            OnImageFadeTypeChangedEvent();
        }

        private void HandleMediaFolderChangedEvent(object sender, EventArgs e)
        {
            OnMediaFolderChangedEvent();
        }

        private string GetOptionsSignature(Options options)
        {
            // config data is small so simple solution is best...
            return JsonConvert.SerializeObject(options);
        }

        private void ReadOptions()
        {
            if (!File.Exists(_optionsFilePath))
            {
                WriteDefaultOptions();
            }
            else
            {
                using (StreamReader file = File.OpenText(_optionsFilePath))
                {
                    var serializer = new JsonSerializer();
                    _options = (Options)serializer.Deserialize(file, typeof(Options));
                    _options.Sanitize();
                }
            }
        }

        private void WriteDefaultOptions()
        {
            _options = new Options();
            WriteOptions();
        }

        private void WriteOptions()
        {
            if (_options != null)
            {
                using (var file = File.CreateText(_optionsFilePath))
                {
                    var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                    serializer.Serialize(file, _options);
                    _originalOptionsSignature = GetOptionsSignature(_options);
                }
            }
        }

        private void OnMediaFolderChangedEvent()
        {
            MediaFolderChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnImageFadeTypeChangedEvent()
        {
            ImageFadeTypeChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnImageFadeSpeedChangedEvent()
        {
            ImageFadeSpeedChangedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void OnAlwaysOnTopChangedEvent()
        {
            AlwaysOnTopChangedEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
