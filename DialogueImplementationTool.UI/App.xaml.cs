using System.IO;
using System.Windows;
using Autofac;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Services;
using DialogueImplementationTool.UI.ViewModels;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order.DI;
namespace DialogueImplementationTool.UI;

public partial class App {
    public App() {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnFirstChanceException;
    }

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        var builder = new ContainerBuilder();

        builder.RegisterType<PythonEmotionClassifier>()
            .AsSelf()
            .As<IEmotionClassifier>()
            .SingleInstance();

        builder.RegisterType<MainWindowVM>()
            .SingleInstance();

        builder.RegisterType<DialogueVM>();

        builder.RegisterType<DialogueProcessor>()
            .AsSelf();

        builder.RegisterType<OutputPathProvider>()
            .SingleInstance();

        builder.RegisterType<SpeakerFavoritesSelection>()
            .As<ISpeakerFavoritesSelection>();

        builder.RegisterType<OpenDocumentTextParser>();

        builder.RegisterType<DocXDocumentParser>();

        builder.RegisterType<MainWindow>()
            .SingleInstance();

        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var pathProvider = new PluginListingsPathProvider();
        var path = pathProvider.Get(GameRelease.SkyrimSE);
        if (!File.Exists(path)) MessageBox.Show($"Make sure {path} exists.");

        var window = scope.Resolve<MainWindow>();
        window.Show();
    }

    private void CurrentDomainOnFirstChanceException(object sender, UnhandledExceptionEventArgs e) {
        var exception = (Exception) e.ExceptionObject;

        using var log = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashLog.txt"), false);
        log.WriteLine(exception);
    }
}
