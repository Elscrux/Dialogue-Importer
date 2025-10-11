using System.IO;
using System.IO.Abstractions;
using System.Windows;
using Autofac;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Script;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Services;
using DialogueImplementationTool.UI.ViewModels;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order.DI;
namespace DialogueImplementationTool.UI;

public partial class App {
    public App() {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnFirstChanceException;
    }

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        var builder = new ContainerBuilder();

        builder.RegisterInstance(new FileSystem())
            .As<IFileSystem>();

        builder.RegisterType<TransformersEmotionClassifier>()
            .AsSelf()
            .As<IEmotionClassifier>()
            .SingleInstance();

        builder.RegisterType<CachedEmotionClassifier>()
            .AsSelf()
            .SingleInstance();

        builder.RegisterType<MainWindowVM>()
            .SingleInstance();

        builder.RegisterType<IterableDialogueConfigVM>();

        builder.RegisterType<DialogueProcessor>()
            .AsSelf();

        builder.RegisterType<QuestDialogueVM>()
            .AsSelf();

        builder.RegisterType<DocumentVM>()
            .AsSelf();

        builder.RegisterType<OutputPathProvider>()
            .SingleInstance();

        builder.RegisterType<AutoApplyProvider>()
            .SingleInstance();

        builder.RegisterType<PapyrusCompilerWrapper>()
            .SingleInstance();

        builder.RegisterType<ScriptWriter>()
            .SingleInstance();

        builder.RegisterType<FormKeyCache>()
            .SingleInstance();

        builder.RegisterType<UIFormKeySelection>()
            .As<IFormKeySelection>()
            .SingleInstance();

        builder.RegisterType<AutomaticSpeakerSelection>()
            .AsSelf();

        builder.RegisterType<UISpeakerSelection>()
            .AsSelf()
            .As<ISpeakerSelection>();

        builder.RegisterType<InjectedPrefixProvider>()
            .As<IPrefixProvider>()
            .SingleInstance();

        builder.RegisterType<SkyrimDialogueContext>()
            .AsSelf()
            .As<IDialogueContext>();

        builder.RegisterType<SpeakerFavoritesSelection>()
            .As<ISpeakerFavoritesSelection>()
            .InstancePerLifetimeScope();

        builder.RegisterType<OpenDocumentTextParser>();
        builder.RegisterType<DocXDocumentParser>();
        builder.RegisterType<CsvDocumentParser>();

        builder.RegisterType<MainWindow>()
            .SingleInstance();

        builder.RegisterType<EnvironmentContext>()
            .As<IEnvironmentContext>()
            .SingleInstance();

        builder.Register(x => x.Resolve<IEnvironmentContext>().Environment.LinkCache)
            .As<ILinkCache>()
            .SingleInstance();

        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var pathProvider = new PluginListingsPathProvider(new DataDirectoryInjection(string.Empty));
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
