using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DialogueImplementationTool.UI;
using Microsoft.Win32;
using Syncfusion.SfSkinManager;
using Syncfusion.Themes.MaterialDark.WPF;
using Syncfusion.Themes.MaterialLight.WPF;

namespace DialogueImplementationTool; 

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App {
    public static readonly DialogueVM DialogueVM = new();
    
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";
    
    private enum WindowsTheme {
        Light,
        Dark
    }

    private static WindowsTheme GetTheme() {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var registryValueObject = key?.GetValue(RegistryValueName);
        if (registryValueObject == null) return WindowsTheme.Light;

        var registryValue = (int) registryValueObject;
        return registryValue == 1 ? WindowsTheme.Light : WindowsTheme.Dark;
    }
    
    public static void UpdateTheme(DependencyObject dependencyObject) {
        SfSkinManager.ApplyStylesOnApplication = true;
        switch (GetTheme()) {
            case WindowsTheme.Dark:
                var darkThemeSettings = new MaterialDarkThemeSettings {
                    PrimaryBackground = new SolidColorBrush(SystemParameters.WindowGlassColor)
                };
                SfSkinManager.RegisterThemeSettings("MaterialDark", darkThemeSettings);
                SfSkinManager.SetTheme(dependencyObject, new Theme("MaterialDark"));

                break;
            case WindowsTheme.Light:
                var lightThemeSettings = new MaterialLightThemeSettings {
                    PrimaryBackground = new SolidColorBrush(SystemParameters.WindowGlassColor)
                };
                SfSkinManager.RegisterThemeSettings("MaterialLight", lightThemeSettings);
                SfSkinManager.SetTheme(dependencyObject, new Theme("MaterialLight"));
                break;
        }
    }
}