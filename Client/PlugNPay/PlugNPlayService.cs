using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using PlugNPay.Utils;
using PlugNPay.Utils.Logs;
using PlugNPayClient.Controller;

namespace PlugNPayClient
{
    class PlugNPlayService : ServiceBase
    {
        private readonly Log _log = new Log();
        private readonly Dictionary<string, IController> _controllers = new Dictionary<string, IController>();

        private string _workingDirectory;

        public void Start()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Version version = asm.GetName().Version;

            _log.LogMessage("Startup");
            _log.LogMessage($"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");

            DirectoryInfo workingDir = new FileInfo(asm.Location).Directory;
            if (workingDir == null)
            {
                _log.LogError("Cannot find working directory");
                throw new DirectoryNotFoundException("Cannot find working directory");
            }

            _workingDirectory = string.Concat(workingDir.FullName, "\\");
            _log.LogMessage($"Working directory: {_workingDirectory}");

            LoadControllers();
        }

        public void Shutdown()
        {
            foreach (var ctrl in _controllers)
            {
                try
                {
                    ctrl.Value.Shutdown();
                }
                catch(Exception ex)
                {
                    _log.LogError(ex);
                }
            }

            _log.LogMessage("Shutdown");
        }

        private void LoadControllers()
        {
            lock(_controllers)
            {
                Dictionary<Type, Attributes> controllers = CollectTypesFromConfig("controllers");

                foreach (var kv in controllers)
                {
                    try
                    {
                        IController controller = Activator.CreateInstance(kv.Key) as IController;
                        if (controller == null)
                        {
                            _log.LogError($"Cannot create instance of [{kv.Key.FullName}] controller");
                            continue;
                        }

                        if (_controllers.ContainsKey(controller.Id)) continue;

                        controller.Starup(GetControllerContext(kv.Value));
                        _controllers[controller.Id] = controller;

                        _log.LogMessage($"Controller {controller.Id} successfully loaded");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex);
                    }
                }
            }

            _log.LogMessage($"Controllers count: {_controllers.Count}");
        }

        private ControllerContext GetControllerContext(Attributes attributes)
        {
            return new ControllerContext(_log, attributes);
        }

        private Dictionary<Type, Attributes> CollectTypesFromConfig(string sectionName)
        {
            ConfigSectionHandler configSection = ConfigurationManager.GetSection(sectionName) as ConfigSectionHandler;
            if (configSection == null) return null;

            Dictionary<Type, Attributes> configuratedTypes = new Dictionary<Type, Attributes>();

            foreach (ConfigSectionElement instance in configSection.Instances)
            {
                try
                {
                    Type type = Type.GetType(instance.Type);
                    if (type != null)
                    {
                        configuratedTypes[type] = instance.Attributes;
                        continue;
                    }

                    string typeFile = string.Concat(_workingDirectory, instance.Type, ".dll");
                    if (File.Exists(typeFile))
                    {
                        byte[] asmData = File.ReadAllBytes(typeFile);
                        Assembly asm = Assembly.Load(asmData);

                        foreach (Type t in asm.GetTypes().Where(t => t.GetInterface(nameof(IController)) != null))
                            configuratedTypes[t] = instance.Attributes;
                    }

                    if (configuratedTypes.Count != 0)
                        continue;

                    _log.LogError($"Cannot load [{instance.Type}], check {sectionName} section in config file");
                }
                catch (Exception ex)
                {
                    _log.LogError(ex);
                }

            }

            return configuratedTypes;
        }

        #region Windows service control

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Shutdown();
        }

        #endregion
    }
}
