using System;
using System.IO;
using DialogueImplementationTool.UI;

namespace DialogueImplementationTool; 

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {
    public static readonly DialogueVM DialogueVM = new();
    
    public App() {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnFirstChanceException;
    }
    
    private void CurrentDomainOnFirstChanceException(object sender, UnhandledExceptionEventArgs e) {
        var exception = (Exception) e.ExceptionObject;
        
        using var log = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashLog.txt"), false);
        log.WriteLine(exception);
    }
}